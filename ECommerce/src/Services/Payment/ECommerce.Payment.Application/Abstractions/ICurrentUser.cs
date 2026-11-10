namespace ECommerce.Payment.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}