using MassTransit;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class PaymentService(IPaymentRepository paymentRepository,ILogger<PaymentService> logger,IUnitOfWork unitOfWork,IPayStackService payStackService) : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        private readonly ILogger<PaymentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IPayStackService _payStackService = payStackService ?? throw new ArgumentNullException(nameof(payStackService));
        public async Task<BaseResponse<PayStackInitDto>> IniatilizeRegistrationPayment(string email)
        {
            var reference = GenerateReference();
            
            var payment = new Payment
            {
                Amount = 5000,
                Reference = reference,
                PaymentStatus = "Pending",
                DateCreated = DateTime.UtcNow,
            };
            if (payment == null)
            {
                _logger.LogError("Payment Is Null");
                return new BaseResponse<PayStackInitDto>
                {
                    Message = "Payment is Null",
                    Status = false
                };     
            }
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
            return await _payStackService.Initialize(reference, 5000,email);

        }

        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            var result = await _payStackService.Verify(reference);
            if (!result.Status)
            {
                return false;
            }
            var payment = await _paymentRepository.GetByReference(reference);

            payment.PaymentStatus = "Successful";
            payment.PaidAt = result.Data.PaidAt;

             _paymentRepository.Update<Payment>(payment);
              await _unitOfWork.SaveChangesAsync();

            return true;
        }
        private string GenerateReference()
        {
            // Use a safe, URL-friendly reference (no slashes). Format "REG-<guid without dashes>"
            return $"REG-{NewId.Next().ToGuid().ToString("N")}";
        }
    }
}
