using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Common;

internal static class ModifierValidator
{
    public static List<Modifier> ValidateAndBuild(
        Guid modifierGroupId,
        IReadOnlyList<ModifierInput> inputs)
    {
        if (inputs.Count == 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["modifiers"] = ["A modifier group must have at least one modifier."]
            });

        var duplicateNames = inputs
            .GroupBy(m => m.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateNames.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["modifiers"] = [$"Duplicate modifier name(s): {string.Join(", ", duplicateNames)}."]
            });

        return inputs.Select(i => new Modifier(
            Guid.NewGuid(),
            modifierGroupId,
            i.Name.Trim(),
            new Money(i.PriceAdjustmentAmount, i.PriceAdjustmentCurrency.ToUpperInvariant()),
            i.DisplayOrder,
            i.IsDefault)
        ).ToList();
    }
}
