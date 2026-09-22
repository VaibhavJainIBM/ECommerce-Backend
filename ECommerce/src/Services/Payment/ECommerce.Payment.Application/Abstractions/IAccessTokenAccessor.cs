namespace ECommerce.Payment.Application.Abstractions;

public interface IAccessTokenAccessor
{
    string? AccessToken { get; }
}