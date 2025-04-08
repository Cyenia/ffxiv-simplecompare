using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Utility;
using static FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace SimpleCompare;

internal class InvItem(Item item, bool isHq)
{
    public readonly bool IsHq = isHq;
    public Item Item = item;
}

internal partial class PluginUI : IDisposable
{
    private bool _visible;

    internal InvItem? InvItem { get; set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int keyCode);

    public void Draw()
    {
        if ((GetKeyState(0x10) & 0x8000) == 0)
        {
            return;
        }

        var hoveredItem = InvItem;
        if (hoveredItem == null) return;

        var inventoryType = GetInventoryType(hoveredItem.Item);
        if (inventoryType is InventoryType.ArmorySoulCrystal or InventoryType.Inventory1) return;

        var equippedItems = GetEquippedItemsByType(inventoryType);
        if (equippedItems.Count <= 0) return;

        if (ImGui.Begin("SimpleCompare", ref _visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus))
        {
            for (var i = 0; i < equippedItems.Count; i++)
            {
                var item = equippedItems[i];

                ImGui.Text(
                    $"Equipped: {item.Item.Name.ExtractText().StripSoftHyphen()} (iLvl {item.Item.LevelItem.RowId}):");
                DrawItemCompareEquipped(item, hoveredItem);
                if (i + 1 < equippedItems.Count)
                {
                    ImGui.Separator();
                }
            }
        }

        var size = ImGui.GetWindowSize();
        var mousePos = ImGui.GetMousePos();
        mousePos.X -= size.X + 25;
        ImGui.SetWindowPos(mousePos, ImGuiCond.Always);

        if (ImGui.Begin("SimpleCompare2", ref _visible,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoNavFocus))
        {
            for (var i = 0; i < equippedItems.Count; i++)
            {
                var item = equippedItems[i];
                ImGui.Text(
                    $"{hoveredItem.Item.Name.ExtractText().StripSoftHyphen()} (iLvl {hoveredItem.Item.LevelItem.RowId}):");
                DrawItemCompareHovered(item, hoveredItem);

                if (i + 1 < equippedItems.Count)
                {
                    ImGui.Separator();
                }
            }
        }

        mousePos.X += size.X + 50;
        ImGui.SetWindowPos(mousePos, ImGuiCond.Always);

        ImGui.End();
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

    private static void DrawItemCompareEquipped(InvItem itemA, InvItem itemB)
    {
        DrawStat("Materia", itemA.Item.MateriaSlotCount - itemB.Item.MateriaSlotCount);

        // map bonus value to type for comparison
        var bonusMapA = GetItemStats(itemA);
        var bonusMapB = GetItemStats(itemB);

        bonusMapA = BonusMapA(bonusMapA, itemA);
        bonusMapB = BonusMapB(bonusMapB, itemB);

        var bonusTypes = new HashSet<uint>();
        bonusTypes = BonusTypes(bonusTypes, bonusMapA, bonusMapB);

        foreach (var bonusType in bonusTypes)
        {
            var valueA = bonusMapA.TryGetValue(bonusType, out var valA) ? valA : 0;
            var valueB = bonusMapB.TryGetValue(bonusType, out var valB) ? valB : 0;

            DrawStat(BaseParamToName(bonusType), valueA - valueB);
        }
    }

    private static void DrawItemCompareHovered(InvItem itemA, InvItem itemB)
    {
        DrawStat("Materia", itemB.Item.MateriaSlotCount - itemA.Item.MateriaSlotCount);

        // map bonus value to type for comparison
        var bonusMapA = GetItemStats(itemA);
        var bonusMapB = GetItemStats(itemB);

        bonusMapA = BonusMapA(bonusMapA, itemA);
        bonusMapB = BonusMapB(bonusMapB, itemB);

        var bonusTypes = new HashSet<uint>();
        bonusTypes = BonusTypes(bonusTypes, bonusMapA, bonusMapB);

        foreach (var bonusType in bonusTypes)
        {
            var valueA = bonusMapA.TryGetValue(bonusType, out var valA) ? valA : 0;
            var valueB = bonusMapB.TryGetValue(bonusType, out var valB) ? valB : 0;

            DrawStat(BaseParamToName(bonusType), valueB - valueA);
        }
    }

    private static Dictionary<uint, short> BonusMapA(Dictionary<uint, short> bonusMapA, InvItem itemA)
    {
        if (!bonusMapA.TryAdd((uint)ItemBonusType.DEFENSE, (short)itemA.Item.DefensePhys))
            bonusMapA[(uint)ItemBonusType.DEFENSE] += (short)itemA.Item.DefensePhys;

        if (!bonusMapA.TryAdd((uint)ItemBonusType.MAGIC_DEFENSE, (short)itemA.Item.DefenseMag))
            bonusMapA[(uint)ItemBonusType.MAGIC_DEFENSE] += (short)itemA.Item.DefenseMag;

        if (!bonusMapA.TryAdd((uint)ItemBonusType.PHYSICAL_DAMAGE, (short)itemA.Item.DamagePhys))
            bonusMapA[(uint)ItemBonusType.PHYSICAL_DAMAGE] += (short)itemA.Item.DamagePhys;

        if (!bonusMapA.TryAdd((uint)ItemBonusType.MAGIC_DAMAGE, (short)itemA.Item.DamageMag))
            bonusMapA[(uint)ItemBonusType.MAGIC_DAMAGE] += (short)itemA.Item.DamageMag;

        if (!bonusMapA.TryAdd((uint)ItemBonusType.BLOCK_STRENGTH, (short)itemA.Item.Block))
            bonusMapA[(uint)ItemBonusType.BLOCK_STRENGTH] += (short)itemA.Item.Block;

        if (!bonusMapA.TryAdd((uint)ItemBonusType.BLOCK_RATE, (short)itemA.Item.BlockRate))
            bonusMapA[(uint)ItemBonusType.BLOCK_RATE] += (short)itemA.Item.BlockRate;

        return bonusMapA;
    }

    private static Dictionary<uint, short> BonusMapB(Dictionary<uint, short> bonusMapB, InvItem itemB)
    {
        if (!bonusMapB.TryAdd((uint)ItemBonusType.DEFENSE, (short)itemB.Item.DefensePhys))
            bonusMapB[(uint)ItemBonusType.DEFENSE] += (short)itemB.Item.DefensePhys;

        if (!bonusMapB.TryAdd((uint)ItemBonusType.MAGIC_DEFENSE, (short)itemB.Item.DefenseMag))
            bonusMapB[(uint)ItemBonusType.MAGIC_DEFENSE] += (short)itemB.Item.DefenseMag;

        if (!bonusMapB.TryAdd((uint)ItemBonusType.PHYSICAL_DAMAGE, (short)itemB.Item.DamagePhys))
            bonusMapB[(uint)ItemBonusType.PHYSICAL_DAMAGE] += (short)itemB.Item.DamagePhys;

        if (!bonusMapB.TryAdd((uint)ItemBonusType.MAGIC_DAMAGE, (short)itemB.Item.DamageMag))
            bonusMapB[(uint)ItemBonusType.MAGIC_DAMAGE] += (short)itemB.Item.DamageMag;

        if (!bonusMapB.TryAdd((uint)ItemBonusType.BLOCK_STRENGTH, (short)itemB.Item.Block))
            bonusMapB[(uint)ItemBonusType.BLOCK_STRENGTH] += (short)itemB.Item.Block;

        if (!bonusMapB.TryAdd((uint)ItemBonusType.BLOCK_RATE, (short)itemB.Item.BlockRate))
            bonusMapB[(uint)ItemBonusType.BLOCK_RATE] += (short)itemB.Item.BlockRate;

        return bonusMapB;
    }

    private static HashSet<uint> BonusTypes(HashSet<uint> bonusTypes, Dictionary<uint, short> bonusMapA,
        Dictionary<uint, short> bonusMapB)
    {
        bonusTypes.UnionWith(bonusMapA.Keys);
        bonusTypes.UnionWith(bonusMapB.Keys);

        return bonusTypes;
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
        if (value != 0)
        {
            ImGui.TextColored(value > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed,
                $"{name}: {(value > 0 ? $"+{value}" : $"{value}")}");
        }
    }

    private static string BaseParamToName(uint baseParam)
    {
        return Service.Data.GetExcelSheet<BaseParam>().GetRow(baseParam).Name.ExtractText().StripSoftHyphen();
    }

    private static InventoryType GetInventoryType(Item item)
    {
        if (item.EquipSlotCategory.Value.MainHand == 1)
        {
            return InventoryType.ArmoryMainHand;
        }

        if (item.EquipSlotCategory.Value.OffHand == 1)
        {
            return InventoryType.ArmoryOffHand;
        }

        if (item.EquipSlotCategory.Value.Head == 1)
        {
            return InventoryType.ArmoryHead;
        }

        if (item.EquipSlotCategory.Value.Body == 1)
        {
            return InventoryType.ArmoryBody;
        }

        if (item.EquipSlotCategory.Value.Gloves == 1)
        {
            return InventoryType.ArmoryHands;
        }

        if (item.EquipSlotCategory.Value.Waist == 1)
        {
            return InventoryType.ArmoryWaist;
        }

        if (item.EquipSlotCategory.Value.Legs == 1)
        {
            return InventoryType.ArmoryLegs;
        }

        if (item.EquipSlotCategory.Value.Feet == 1)
        {
            return InventoryType.ArmoryFeets;
        }

        if (item.EquipSlotCategory.Value.Ears == 1)
        {
            return InventoryType.ArmoryEar;
        }

        if (item.EquipSlotCategory.Value.Neck == 1)
        {
            return InventoryType.ArmoryNeck;
        }

        if (item.EquipSlotCategory.Value.Wrists == 1)
        {
            return InventoryType.ArmoryWrist;
        }

        if (item.EquipSlotCategory.Value.FingerL == 1)
        {
            return InventoryType.ArmoryRings;
        }

        if (item.EquipSlotCategory.Value.FingerR == 1)
        {
            return InventoryType.ArmoryRings;
        }

        return item.EquipSlotCategory.Value.SoulCrystal == 1
            ? InventoryType.ArmorySoulCrystal
            : InventoryType.Inventory1;
    }
}