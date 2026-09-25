namespace ECommerce.Order.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
