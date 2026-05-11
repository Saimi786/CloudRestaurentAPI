using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Common;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Commands;

public sealed record DeactivateUserCommand(Guid Id) : IRequest;

public sealed class DeactivateUserHandler(IIdentityService identity, ITenantContext tenantContext, ICurrentUser currentUser)
    : IRequestHandler<DeactivateUserCommand>
{
    public async Task Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (currentUser.UserId == request.Id)
            throw new BusinessRuleException("You cannot deactivate your own account.");

        try
        {
            await identity.DeactivateUserAsync(request.Id, tenantId, ct);
        }
        catch (IdentityOperationException ex)
        {
            throw IdentityErrorMapper.ToAppException(ex);
        }
    }
}
