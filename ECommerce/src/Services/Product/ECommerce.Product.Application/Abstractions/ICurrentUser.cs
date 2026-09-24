namespace ECommerce.Product.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}