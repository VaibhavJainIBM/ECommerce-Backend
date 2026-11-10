namespace ECommerce.Product.Application.Abstractions;

public interface IAccessTokenAccessor
{
    string? AccessToken { get; }
}