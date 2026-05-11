using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Common;
using FluentValidation;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Commands;

public sealed record ResetPasswordCommand(Guid Id, string NewPassword) : IRequest;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public sealed class ResetPasswordHandler(IIdentityService identity, ITenantContext tenantContext)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        try
        {
            await identity.ResetPasswordAsync(request.Id, tenantId, request.NewPassword, ct);
        }
        catch (IdentityOperationException ex)
        {
            throw IdentityErrorMapper.ToAppException(ex);
        }
    }
}
