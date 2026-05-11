using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    decimal? MaxDiscountPercent);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class LoginCommandHandler(IIdentityService identity, IJwtTokenService jwt)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await identity.ValidateCredentialsAsync(request.Email, request.Password, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        var token = jwt.Issue(user);
        return new LoginResult(
            token.Value, token.ExpiresAt,
            user.UserId, user.Email, user.FullName,
            user.TenantId, user.Roles, user.BranchIds, user.MaxDiscountPercent);
    }
}
