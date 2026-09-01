using ECommerce.Application.Authentication.Models;

namespace ECommerce.Application.Abstractions.Authentication;

public interface IAccessTokenGenerator
{
    AccessToken Generate(UserAccount account);
}