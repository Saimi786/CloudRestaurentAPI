using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Roles;

public sealed record GetRolesDetailedQuery : IRequest<IReadOnlyList<RoleDetailsDto>>;

public sealed record GetPermissionCatalogQuery : IRequest<IReadOnlyList<PermissionDescriptor>>;

public sealed record CreateRoleCommand(string Name, IReadOnlyList<string> Permissions)
    : IRequest<RoleDetailsDto>;

public sealed record UpdateRoleCommand(Guid Id, string Name, IReadOnlyList<string> Permissions)
    : IRequest<RoleDetailsDto>;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Permissions).NotNull();
    }
}

public sealed class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Permissions).NotNull();
    }
}

public sealed class GetRolesDetailedHandler(IRoleAdminService svc, ITenantContext tenant)
    : IRequestHandler<GetRolesDetailedQuery, IReadOnlyList<RoleDetailsDto>>
{
    public Task<IReadOnlyList<RoleDetailsDto>> Handle(GetRolesDetailedQuery _, CancellationToken ct)
    {
        var tid = tenant.TenantId ?? throw new UnauthorizedException("No tenant in current context.");
        return svc.ListAsync(tid, ct);
    }
}

public sealed class GetPermissionCatalogHandler(IRoleAdminService svc)
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionDescriptor>>
{
    public Task<IReadOnlyList<PermissionDescriptor>> Handle(GetPermissionCatalogQuery _, CancellationToken ct) =>
        Task.FromResult(svc.GetPermissionCatalog());
}

public sealed class CreateRoleHandler(IRoleAdminService svc, ITenantContext tenant)
    : IRequestHandler<CreateRoleCommand, RoleDetailsDto>
{
    public Task<RoleDetailsDto> Handle(CreateRoleCommand cmd, CancellationToken ct)
    {
        var tid = tenant.TenantId ?? throw new UnauthorizedException("No tenant in current context.");
        return svc.CreateAsync(tid, cmd.Name, cmd.Permissions, ct);
    }
}

public sealed class UpdateRoleHandler(IRoleAdminService svc, ITenantContext tenant)
    : IRequestHandler<UpdateRoleCommand, RoleDetailsDto>
{
    public Task<RoleDetailsDto> Handle(UpdateRoleCommand cmd, CancellationToken ct)
    {
        var tid = tenant.TenantId ?? throw new UnauthorizedException("No tenant in current context.");
        return svc.UpdateAsync(tid, cmd.Id, cmd.Name, cmd.Permissions, ct);
    }
}

public sealed class DeleteRoleHandler(IRoleAdminService svc, ITenantContext tenant)
    : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand cmd, CancellationToken ct)
    {
        var tid = tenant.TenantId ?? throw new UnauthorizedException("No tenant in current context.");
        await svc.DeleteAsync(tid, cmd.Id, ct);
    }
}
