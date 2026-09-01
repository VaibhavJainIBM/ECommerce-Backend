namespace ECommerce.Application.Common;

public sealed record Error(
    string Code,
    string Description);