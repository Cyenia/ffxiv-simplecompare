using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using SimpleCompare.Services;
using InventoryItem = SimpleCompare.Models.InventoryItem;
using ItemFlags = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags;

namespace SimpleCompare.Helpers;

/// <summary>
/// Provides helper methods for managing and retrieving inventory-related data.
/// </summary>
internal static class InventoryHelper
{
    /// <summary>
    /// Gets a list of equipped items filtered by the specified inventory type.
    /// </summary>
    /// <param name="inventoryType">The inventory type to filter the equipped items.</param>
    /// <returns>A list of <see cref="InventoryItem"/> objects that match the specified inventory type.</returns>
    public static List<InventoryItem> GetEquippedItemsByType(InventoryType inventoryType)
    {
        var items = new List<InventoryItem>();
        unsafe
        {
            var equippedItems = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
            var count = equippedItems->Size;
            for (var i = 0; i < count; i++)
            {
                var inventoryItem = equippedItems->Items[i];
                var item = Service.Data.GetExcelSheet<Item>().GetRow(inventoryItem.ItemId);

                if (inventoryType != GetInventoryType(item)) continue;

                const ItemFlags hqFlag = ItemFlags.HighQuality;
                items.Add(new InventoryItem(item, (inventoryItem.Flags & hqFlag) == hqFlag));
            }
        }

        return items;
    }

    /// <summary>
    /// Determines the inventory type of given item based on its equipment slot category.
    /// </summary>
    /// <param name="item">The item whose inventory type is being determined.</param>
    /// <returns>
    /// The corresponding <see cref="InventoryType"/> for the item's equipment slot category.
    /// If no specific match is found, the default is <see cref="InventoryType.Invalid"/>.
    /// </returns>
    public static InventoryType GetInventoryType(Item item)
    {
        var slotMapping = new (Func<EquipSlotCategory, int> selector, InventoryType type)[]
        {
            (c => c.MainHand, InventoryType.ArmoryMainHand),
            (c => c.OffHand, InventoryType.ArmoryOffHand),
            (c => c.Head, InventoryType.ArmoryHead),
            (c => c.Body, InventoryType.ArmoryBody),
            (c => c.Gloves, InventoryType.ArmoryHands),
            (c => c.Waist, InventoryType.ArmoryWaist),
            (c => c.Legs, InventoryType.ArmoryLegs),
            (c => c.Feet, InventoryType.ArmoryFeets),
            (c => c.Ears, InventoryType.ArmoryEar),
            (c => c.Neck, InventoryType.ArmoryNeck),
            (c => c.Wrists, InventoryType.ArmoryWrist),
            (c => c.FingerL, InventoryType.ArmoryRings),
            (c => c.FingerR, InventoryType.ArmoryRings),
            (c => c.SoulCrystal, InventoryType.ArmorySoulCrystal)
        };

        var category = item.EquipSlotCategory.Value;

        foreach (var (selector, type) in slotMapping)
            if (selector(category) == 1)
                return type;

        return InventoryType.Invalid;
    }
}