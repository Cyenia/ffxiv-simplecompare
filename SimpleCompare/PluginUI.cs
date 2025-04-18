﻿using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Utility;
using static FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace SimpleCompare;

internal class InvItem(Item item, bool isHq)
{
    public readonly bool IsHq = isHq;
    public Item Item = item;
}

internal class PluginUI : IDisposable
{
    private bool _visible;

    private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration |
                                                 ImGuiWindowFlags.NoBringToFrontOnFocus |
                                                 ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus;

    internal InvItem? InvItem { get; set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Draw()
    {
        if (!Service.KeyState[VirtualKey.SHIFT]) return;
        var hoveredItem = InvItem;
        if (hoveredItem == null) return;
        var inventoryType = GetInventoryType(hoveredItem.Item);
        if (inventoryType is InventoryType.ArmorySoulCrystal or InventoryType.Inventory1) return;
        var equippedItems = GetEquippedItemsByType(inventoryType);
        if (equippedItems.Count <= 0) return;

        DrawItemList("SimpleCompare", hoveredItem, equippedItems, false);

        var size = ImGui.GetWindowSize();
        var mousePos = ImGui.GetMousePos();
        mousePos.X -= size.X + 25;
        ImGui.SetWindowPos(mousePos, ImGuiCond.Always);

        DrawItemList("SimpleCompare2", hoveredItem, equippedItems, true);

        mousePos.X += size.X + 50;
        ImGui.SetWindowPos(mousePos, ImGuiCond.Always);

        ImGui.End();
    }
    
    private void DrawItemList(string windowName, InvItem hoveredItem, List<InvItem> equippedItems, bool hovered)
    {
        if (!ImGui.Begin(windowName, ref _visible, WindowFlags)) return;

        for (var i = 0; i < equippedItems.Count; i++)
        {
            var item = equippedItems[i];
            var iLvlDiff = hovered
                ? (int)(hoveredItem.Item.LevelItem.RowId - item.Item.LevelItem.RowId)
                : (int)(item.Item.LevelItem.RowId - hoveredItem.Item.LevelItem.RowId);

            ImGui.Text(hovered
                ? $"{hoveredItem.Item.Name.ExtractText().StripSoftHyphen()}"
                : $"Equipped: {item.Item.Name.ExtractText().StripSoftHyphen()}");
            ImGui.SameLine();
            TextColored($"(iLvl {(hovered ? hoveredItem : item).Item.LevelItem.RowId})", iLvlDiff);

            if (hovered)
                DrawItemCompare(hoveredItem, item, false);
            else
                DrawItemCompare(item, hoveredItem);

            if (i + 1 < equippedItems.Count)
                ImGui.Separator();
        }
    }

    private static List<InvItem> GetEquippedItemsByType(InventoryType inventoryType)
    {
        var items = new List<InvItem>();
        unsafe
        {
            var equippedItems = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
            var count = equippedItems->Size;
            for (var i = 0; i < count; i++)
            {
                var inventoryItem = equippedItems->Items[i];
                var item = Service.Data.GetExcelSheet<Item>().GetRow(inventoryItem.ItemId);

                if (inventoryType == GetInventoryType(item))
                {
                    items.Add(new InvItem(item,
                        (inventoryItem.Flags & ItemFlags.HighQuality) == ItemFlags.HighQuality));
                }
            }
        }

        return items;
    }

    private static void DrawItemCompare(InvItem itemA, InvItem itemB, bool equipped = true)
    {
        DrawStat("Materia", itemB.Item.MateriaSlotCount - itemA.Item.MateriaSlotCount);

        // map bonus value to type for comparison
        var bonusMapA = BonusMap(itemA);
        var bonusMapB = BonusMap(itemB);

        var bonusMapKeys = equipped ? bonusMapA.Keys.Union(bonusMapB.Keys) : bonusMapB.Keys.Union(bonusMapA.Keys);
        var bonusTypes = new HashSet<uint>(bonusMapKeys);

        foreach (var bonusType in bonusTypes)
        {
            var valueA = bonusMapA.TryGetValue(bonusType, out var valA) ? valA : 0;
            var valueB = bonusMapB.TryGetValue(bonusType, out var valB) ? valB : 0;

            DrawStat(BaseParamToName(bonusType), valueA - valueB);
        }
    }

    private static Dictionary<uint, short> BonusMap(InvItem item)
    {
        var bonusMap = GetItemStats(item);

        if (!bonusMap.TryAdd((uint)ItemBonusType.DEFENSE, (short)item.Item.DefensePhys))
            bonusMap[(uint)ItemBonusType.DEFENSE] += (short)item.Item.DefensePhys;

        if (!bonusMap.TryAdd((uint)ItemBonusType.MAGIC_DEFENSE, (short)item.Item.DefenseMag))
            bonusMap[(uint)ItemBonusType.MAGIC_DEFENSE] += (short)item.Item.DefenseMag;

        if (!bonusMap.TryAdd((uint)ItemBonusType.PHYSICAL_DAMAGE, (short)item.Item.DamagePhys))
            bonusMap[(uint)ItemBonusType.PHYSICAL_DAMAGE] += (short)item.Item.DamagePhys;

        if (!bonusMap.TryAdd((uint)ItemBonusType.MAGIC_DAMAGE, (short)item.Item.DamageMag))
            bonusMap[(uint)ItemBonusType.MAGIC_DAMAGE] += (short)item.Item.DamageMag;

        if (!bonusMap.TryAdd((uint)ItemBonusType.BLOCK_STRENGTH, (short)item.Item.Block))
            bonusMap[(uint)ItemBonusType.BLOCK_STRENGTH] += (short)item.Item.Block;

        if (!bonusMap.TryAdd((uint)ItemBonusType.BLOCK_RATE, (short)item.Item.BlockRate))
            bonusMap[(uint)ItemBonusType.BLOCK_RATE] += (short)item.Item.BlockRate;

        return bonusMap;
    }

    private static Dictionary<uint, short> GetItemStats(InvItem invItem)
    {
        var bonusMap = new Dictionary<uint, short>();

        for (var i = 0; i < invItem.Item.BaseParam.Count; i++)
        {
            bonusMap[invItem.Item.BaseParam[i].RowId] = invItem.Item.BaseParamValue[i];
        }

        if (!invItem.IsHq)
        {
            // We can return here, because no conversion is needed for nq items
            return bonusMap;
        }

        var result = new Dictionary<uint, short>();
        for (var i = 0; i < invItem.Item.BaseParamSpecial.Count; i++)
        {
            if (bonusMap.TryGetValue(invItem.Item.BaseParamSpecial[i].RowId, out var baseVal))
            {
                baseVal += invItem.Item.BaseParamValueSpecial[i];
                result[invItem.Item.BaseParamSpecial[i].RowId] = baseVal;
            }
            else
            {
                result[invItem.Item.BaseParamSpecial[i].RowId] = invItem.Item.BaseParamValueSpecial[i];
            }
        }

        return result;
    }

    private static void DrawStat(string name, int value)
    {
        TextColored($"{name}: {(value > 0 ? $"+{value}" : $"{value}")}", value);
    }
    
    private static void TextColored(string text, int value)
    {
        if (value == 0) return;

        var color = value > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
        ImGui.TextColored(color, text);
    }

    private static string BaseParamToName(uint baseParam)
    {
        return Service.Data.GetExcelSheet<BaseParam>().GetRow(baseParam).Name.ExtractText().StripSoftHyphen();
    }

    private static InventoryType GetInventoryType(Item item)
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
        {
            if (selector(category) == 1)
                return type;
        }

        return InventoryType.Inventory1;
    }
}