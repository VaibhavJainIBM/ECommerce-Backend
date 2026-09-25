using ECommerce.User.Application.Authentication.Models;

namespace ECommerce.User.Application.Abstractions.Authentication;

public interface IAccessTokenGenerator
{
    AccessToken Generate(UserAccount account);
}
