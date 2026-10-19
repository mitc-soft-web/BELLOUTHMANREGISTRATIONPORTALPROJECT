using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class PaymentService(
        IPaymentRepository paymentRepository,
        ILogger<PaymentService> logger,
        IUnitOfWork unitOfWork,
        IPayStackService payStackService,
        IOptions<PaystackConfiguration> paystackOptions) : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        private readonly ILogger<PaymentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IPayStackService _payStackService = payStackService ?? throw new ArgumentNullException(nameof(payStackService));
        private readonly PaystackConfiguration _paystack = paystackOptions.Value;

        public async Task<BaseResponse<PayStackInitDto>> IniatilizeRegistrationPayment(string email)
        {
            if (!_paystack.IsConfigured)
            {
                _logger.LogError("Paystack is not configured for payment initialization.");
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = "Online payment is temporarily unavailable. Please try again later or contact support."
                };
            }

            var reference = GenerateReference();
            var fee = _paystack.RegistrationFeeNaira;

            var payment = new Payment
            {
                Amount = fee,
                Reference = reference,
                PaymentStatus = "Pending",
                DateCreated = DateTime.UtcNow,
            };

            var newPayment = await _paymentRepository.Add<Payment>(payment);
            await _unitOfWork.SaveChangesAsync();
            if (newPayment == null)
            {
                return new BaseResponse<PayStackInitDto>
                {
                    Message = "Payment created unsucessfully",
                    Status = false
                };
            }

            var response = await _payStackService.Initialize(reference, fee, email);
            if (!response.Status)
            {
                newPayment.PaymentStatus = "Failed";
                _paymentRepository.Update<Payment>(newPayment);
                await _unitOfWork.SaveChangesAsync();
            }

            return response;
        }

        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            var payment = await _paymentRepository.GetByReference(reference);
            if (payment == null)
            {
                _logger.LogWarning("Payment record {Reference} was not found in database.", reference);
                return false;
            }

            if (string.Equals(payment.PaymentStatus, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var result = await _payStackService.Verify(reference);
            if (!result.Status)
            {
                return false;
            }

            payment.PaymentStatus = "Successful";
            payment.PaidAt = result.Data?.PaidAt ?? DateTime.UtcNow.ToString("O");
            if (result.Data?.AmountKobo is long kobo)
            {
                payment.Amount = kobo / 100m;
            }

            payment.ConcurrencyStamp = Guid.NewGuid();
            _paymentRepository.Update<Payment>(payment);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogInformation("Concurrent payment verification update detected for reference {Reference}; proceeding idempotently.", reference);
            }

            return true;
        }

        private string GenerateReference()
        {
            return $"REG-{NewId.Next().ToGuid().ToString("N")}";
        }
    }
}
