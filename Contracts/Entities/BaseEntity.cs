using MassTransit;

namespace Mitc_Registration_Portal.Contracts.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = NewId.Next().ToGuid();
        public DateTime DateCreated { get; set; }
        public bool IsDelete { get; set; } = false;
    }
}
