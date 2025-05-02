using Lumina.Excel.Sheets;

namespace SimpleCompare.Models;

/// <summary>
/// Represents an inventory item, including its base item data and quality status.
/// </summary>
internal class InventoryItem
{
    /// <summary>
    /// Indicates if the item is High Quality (HQ).
    /// </summary>
    public readonly bool IsHq;

    /// <summary>
    /// The base item data associated with this inventory item.
    /// </summary>
    public Item Item;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItem"/> class.
    /// </summary>
    /// <param name="item">The base item data.</param>
    /// <param name="isHq">Indicates if the item is High Quality (HQ).</param>
    public InventoryItem(Item item, bool isHq)
    {
        Item = item;
        IsHq = isHq;
    }
}