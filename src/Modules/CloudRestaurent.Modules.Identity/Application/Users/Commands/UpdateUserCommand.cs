using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Common;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using FluentValidation;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Commands;

public sealed record UpdateUserCommand(
    Guid Id,
    string FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid>? BranchIds = null,
    decimal? MaxDiscountPercent = null) : IRequest<UserDto>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles).NotNull();
        RuleFor(x => x.MaxDiscountPercent).InclusiveBetween(0m, 100m)
            .When(x => x.MaxDiscountPercent.HasValue);
    }
}

public sealed class UpdateUserHandler(IIdentityService identity, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        try
        {
            var user = await identity.UpdateUserAsync(
                request.Id, tenantId,
                new UpdateUserInput(
                    request.FullName, request.IsActive, request.Roles,
                    request.BranchIds ?? Array.Empty<Guid>(),
                    request.MaxDiscountPercent),
                ct);
            return new UserDto(user.Id, user.Email, user.FullName, user.IsActive,
                user.CreatedAt, user.LastLoginAt, user.Roles, user.BranchIds, user.MaxDiscountPercent);
        }
        catch (IdentityOperationException ex)
        {
            throw IdentityErrorMapper.ToAppException(ex);
        }
    }
}
