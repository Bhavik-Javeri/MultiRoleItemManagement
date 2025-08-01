using ItemManagement.Model; // Needed to reference ItemType enum

namespace ItemManagement.Model.DTO
{
    public class ItemFilterDto
    {
        // Change type from 'Category' to 'ItemType' and remove the 'using static' for clarity.
        // The property name 'Category' is fine here, as it's a filter input.
        public Category? categoryType { get; set; }
    }
}
