using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Commands;

public sealed record CreateCustomerGroupCommand(
    string Name,
    decimal DiscountPercent,
    string? Description) : IRequest<CustomerGroupDto>;

public sealed class CreateCustomerGroupValidator : AbstractValidator<CreateCustomerGroupCommand>
{
    public CreateCustomerGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class CreateCustomerGroupHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateCustomerGroupCommand, CustomerGroupDto>
{
    public async Task<CustomerGroupDto> Handle(CreateCustomerGroupCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<CustomerGroup>().AnyAsync(g => g.Name == request.Name, ct))
            throw new ConflictException($"A customer group named '{request.Name}' already exists.");

        var group = new CustomerGroup(Guid.NewGuid(), tenantId, request.Name, request.DiscountPercent, request.Description);
        db.Set<CustomerGroup>().Add(group);
        await db.SaveChangesAsync(ct);

        return new CustomerGroupDto(group.Id, group.Name, group.DiscountPercent, group.Description, group.IsActive);
    }
}
