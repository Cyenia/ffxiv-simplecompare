using System.Collections.Generic;
using SimpleCompare.Models;

namespace SimpleCompare.Helpers;

/// <summary>
/// Provides utilities for comparing inventory items and calculating their stats.
/// </summary>
internal static class ItemComparer
{
    /// <summary>
    /// Creates a bonus map for the given inventory item, including its base stats and additional bonuses.
    /// </summary>
    /// <param name="item">The inventory item to generate the bonus map for.</param>
    /// <returns>A dictionary that maps stat IDs to their respective values.</returns>
    public static Dictionary<uint, short> BonusMap(InventoryItem item)
    {
        var bonusMap = GetItemStats(item);

        AddStat(bonusMap, (uint)ItemBonusType.DEFENSE, (short)item.Item.DefensePhys);
        AddStat(bonusMap, (uint)ItemBonusType.MAGIC_DEFENSE, (short)item.Item.DefenseMag);
        AddStat(bonusMap, (uint)ItemBonusType.PHYSICAL_DAMAGE, (short)item.Item.DamagePhys);
        AddStat(bonusMap, (uint)ItemBonusType.MAGIC_DAMAGE, (short)item.Item.DamageMag);
        AddStat(bonusMap, (uint)ItemBonusType.BLOCK_STRENGTH, (short)item.Item.Block);
        AddStat(bonusMap, (uint)ItemBonusType.BLOCK_RATE, (short)item.Item.BlockRate);

        return bonusMap;
    }

    /// <summary>
    /// Adds a stat to the given map or increments its value if it already exists.
    /// </summary>
    /// <param name="map">The dictionary to add the stat to.</param>
    /// <param name="stat">The stat ID to add or update.</param>
    /// <param name="value">The value of the stat to add.</param>
    private static void AddStat(Dictionary<uint, short> map, uint stat, short value)
    {
        if (!map.TryAdd(stat, value)) map[stat] += value;
    }

    /// <summary>
    /// Retrieves the base and special stats of an inventory item, taking into account its HQ status.
    /// </summary>
    /// <param name="item">The inventory item from which to extract stats.</param>
    /// <returns>A dictionary that maps stat IDs to their respective values.</returns>
    private static Dictionary<uint, short> GetItemStats(InventoryItem item)
    {
        var bonusMap = new Dictionary<uint, short>();

        for (var i = 0; i < item.Item.BaseParam.Count; i++)
            bonusMap[item.Item.BaseParam[i].RowId] = item.Item.BaseParamValue[i];

        if (!item.IsHq)
            return bonusMap;

        var result = new Dictionary<uint, short>();
        for (var i = 0; i < item.Item.BaseParamSpecial.Count; i++)
            if (bonusMap.TryGetValue(item.Item.BaseParamSpecial[i].RowId, out var baseVal))
            {
                baseVal += item.Item.BaseParamValueSpecial[i];
                result[item.Item.BaseParamSpecial[i].RowId] = baseVal;
            }
            else
            {
                result[item.Item.BaseParamSpecial[i].RowId] = item.Item.BaseParamValueSpecial[i];
            }

        return result;
    }
}