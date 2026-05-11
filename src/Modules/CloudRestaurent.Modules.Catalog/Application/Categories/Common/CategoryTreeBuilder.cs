using CloudRestaurent.Modules.Catalog.Application.Categories.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;

namespace CloudRestaurent.Modules.Catalog.Application.Categories.Common;

internal static class CategoryTreeBuilder
{
    /// <summary>
    /// Sort categories so parents always appear before children, indent by depth,
    /// compute full breadcrumb path. Cycle-safe (depth capped at the row count).
    /// </summary>
    public static List<CategoryDto> BuildOrdered(IReadOnlyList<Category> all)
    {
        var byId = all.ToDictionary(c => c.Id);
        var children = all
            .Where(c => c.ParentCategoryId.HasValue)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList());

        var roots = all.Where(c => !c.ParentCategoryId.HasValue || !byId.ContainsKey(c.ParentCategoryId.Value))
                       .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();

        var result = new List<CategoryDto>(all.Count);
        foreach (var root in roots) Walk(root, depth: 0, parentPath: "", parent: null);
        return result;

        void Walk(Category c, int depth, string parentPath, Category? parent)
        {
            var path = string.IsNullOrEmpty(parentPath) ? c.Name : $"{parentPath} > {c.Name}";
            result.Add(new CategoryDto(
                c.Id, c.Name, c.DisplayOrder,
                c.ParentCategoryId, parent?.Name,
                c.KitchenStationId,
                depth, path, c.IsActive));

            if (children.TryGetValue(c.Id, out var kids) && depth < all.Count)
                foreach (var kid in kids) Walk(kid, depth + 1, path, c);
        }
    }
}
