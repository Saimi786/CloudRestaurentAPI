using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Common;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using FluentValidation;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Commands;

public sealed record CreateUserCommand(
    string Email,
    string FullName,
    string Password,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid>? BranchIds = null,
    decimal? MaxDiscountPercent = null) : IRequest<UserDto>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.Roles).NotNull();
        RuleFor(x => x.MaxDiscountPercent).InclusiveBetween(0m, 100m)
            .When(x => x.MaxDiscountPercent.HasValue);
    }
}

public sealed class CreateUserHandler(IIdentityService identity, ITenantContext tenantContext)
    : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        try
        {
            var user = await identity.CreateUserAsync(
                new CreateUserInput(
                    request.Email, request.FullName, tenantId, request.Password,
                    request.Roles,
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
