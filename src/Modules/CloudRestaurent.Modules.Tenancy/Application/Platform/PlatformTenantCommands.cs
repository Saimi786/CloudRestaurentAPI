using CloudRestaurent.Application.Common.Abstractions;
using FluentValidation;
using MediatR;

namespace CloudRestaurent.Modules.Tenancy.Application.Platform;

public sealed record GetPlatformTenantsQuery(bool IncludeInactive = true)
    : IRequest<IReadOnlyList<PlatformTenantListItem>>;

public sealed record GetPlatformTenantQuery(Guid Id) : IRequest<PlatformTenantDetails>;

public sealed record CreatePlatformTenantCommand(
    string Name,
    string Slug,
    int BusinessType,
    int Plan,
    string AdminEmail,
    string AdminFullName,
    string AdminPassword) : IRequest<PlatformTenantDetails>;

public sealed record UpdatePlatformTenantCommand(Guid Id, string Name, int Plan)
    : IRequest<PlatformTenantDetails>;

public sealed record SetTenantActiveCommand(Guid Id, bool IsActive) : IRequest;

public sealed class CreatePlatformTenantValidator : AbstractValidator<CreatePlatformTenantCommand>
{
    public CreatePlatformTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(60)
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
            .WithMessage("Slug must be lowercase letters/digits/hyphens only, with no leading/trailing hyphen.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8);
    }
}

public sealed class UpdatePlatformTenantValidator : AbstractValidator<UpdatePlatformTenantCommand>
{
    public UpdatePlatformTenantValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class GetPlatformTenantsHandler(IPlatformTenantService svc)
    : IRequestHandler<GetPlatformTenantsQuery, IReadOnlyList<PlatformTenantListItem>>
{
    public Task<IReadOnlyList<PlatformTenantListItem>> Handle(GetPlatformTenantsQuery q, CancellationToken ct) =>
        svc.ListAsync(q.IncludeInactive, ct);
}

public sealed class GetPlatformTenantHandler(IPlatformTenantService svc)
    : IRequestHandler<GetPlatformTenantQuery, PlatformTenantDetails>
{
    public Task<PlatformTenantDetails> Handle(GetPlatformTenantQuery q, CancellationToken ct) =>
        svc.GetAsync(q.Id, ct);
}

public sealed class CreatePlatformTenantHandler(IPlatformTenantService svc)
    : IRequestHandler<CreatePlatformTenantCommand, PlatformTenantDetails>
{
    public Task<PlatformTenantDetails> Handle(CreatePlatformTenantCommand c, CancellationToken ct) =>
        svc.CreateAsync(new CreatePlatformTenantInput(
            c.Name, c.Slug, c.BusinessType, c.Plan,
            c.AdminEmail, c.AdminFullName, c.AdminPassword), ct);
}

public sealed class UpdatePlatformTenantHandler(IPlatformTenantService svc)
    : IRequestHandler<UpdatePlatformTenantCommand, PlatformTenantDetails>
{
    public Task<PlatformTenantDetails> Handle(UpdatePlatformTenantCommand c, CancellationToken ct) =>
        svc.UpdateAsync(c.Id, new UpdatePlatformTenantInput(c.Name, c.Plan), ct);
}

public sealed class SetTenantActiveHandler(IPlatformTenantService svc)
    : IRequestHandler<SetTenantActiveCommand>
{
    public async Task Handle(SetTenantActiveCommand c, CancellationToken ct) =>
        await svc.SetActiveAsync(c.Id, c.IsActive, ct);
}
