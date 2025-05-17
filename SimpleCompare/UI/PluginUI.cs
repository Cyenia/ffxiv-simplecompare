using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Excel;
using SimpleCompare.Helpers;
using SimpleCompare.Services;
using InventoryItem = SimpleCompare.Models.InventoryItem;

namespace SimpleCompare.UI;

/// <summary>
/// Handles the user interface (UI) for the SimpleCompare plugin.
/// Displays item comparison windows and manages their positioning.
/// </summary>
internal class PluginUI : IDisposable
{
    private bool _visible;
    private int _statDiff;

    private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration |
                                                 ImGuiWindowFlags.NoBringToFrontOnFocus |
                                                 ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus;

    internal InventoryItem? HoveredItem { get; set; }
    private ExcelSheet<BaseParam> BaseParamSheet { get; } = Service.Data.GetExcelSheet<BaseParam>();

    /// <summary>
    /// Disposes of the resources used by the PluginUI instance.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Draws the UI elements for the plugin, including the item comparison windows.
    /// </summary>
    public void Draw()
    {
        if (!Service.KeyState[VirtualKey.SHIFT]) return;
        var hoveredItem = HoveredItem;
        if (hoveredItem == null) return;
        var inventoryType = InventoryHelper.GetInventoryType(hoveredItem.Item);
        if (inventoryType is InventoryType.Invalid) return;
        var equippedItems = InventoryHelper.GetEquippedItemsByType(inventoryType);
        if (equippedItems.Count <= 0) return;

        DrawItemList("SimpleCompare", hoveredItem, equippedItems, false);
        var equipSize = ImGui.GetWindowSize();

        DrawItemList("SimpleCompare2", hoveredItem, equippedItems, true);
        var compareSize = ImGui.GetWindowSize();

        var screenSize = ImGui.GetMainViewport().Size;
        var mousePos = ImGui.GetMousePos();

        var equipPos = new Vector2(mousePos.X - equipSize.X - 25, mousePos.Y);
        var comparePos = new Vector2(mousePos.X + 25, mousePos.Y);
        var equipLeftOutOfBounds = equipPos.X - 5 < 0;
        var compareRightOutOfBounds = comparePos.X + compareSize.X + 5 > screenSize.X;

        if (equipLeftOutOfBounds || compareRightOutOfBounds)
        {
            if (equipLeftOutOfBounds)
            {
                equipPos = comparePos;
                comparePos.Y += equipSize.Y + 5;
            }
            else
            {
                comparePos = new Vector2(equipPos.X, equipPos.Y + equipSize.Y + 5);
            }

            var totalHeight = equipSize.Y + compareSize.Y + 10;
            var overflowY = equipPos.Y + totalHeight - screenSize.Y;
            if (overflowY > 0)
            {
                equipPos.Y -= overflowY;
                comparePos.Y -= overflowY;
            }
        }
        else
        {
            var minHeight = Math.Min(equipSize.Y, compareSize.Y) + 5;
            var overflowY = mousePos.Y + minHeight - screenSize.Y;
            if (overflowY > 0)
            {
                equipPos.Y -= overflowY;
                comparePos.Y -= overflowY;
            }
        }

        ImGui.SetWindowPos("SimpleCompare", equipPos, ImGuiCond.Always);
        ImGui.SetWindowPos("SimpleCompare2", comparePos, ImGuiCond.Always);

        ImGui.End();
    }

    /// <summary>
    /// Draws a list of items in a comparison window.
    /// </summary>
    /// <param name="windowName">The name of the ImGui window.</param>
    /// <param name="hoveredItem">The currently hovered inventory item.</param>
    /// <param name="equippedItems">The list of equipped items to compare.</param>
    /// <param name="hovered">Indicates whether the hovered item is being compared.</param>
    private void DrawItemList(string windowName, InventoryItem hoveredItem, List<InventoryItem> equippedItems,
        bool hovered)
    {
        if (!ImGui.Begin(windowName, ref _visible, WindowFlags)) return;

        for (var i = 0; i < equippedItems.Count; i++)
        {
            var item = equippedItems[i];
            var iLvlDiff = hovered
                ? (int)(hoveredItem.Item.LevelItem.RowId - item.Item.LevelItem.RowId)
                : (int)(item.Item.LevelItem.RowId - hoveredItem.Item.LevelItem.RowId);

            if (equippedItems.Count > 1)
            {
                ImGui.Text($"{i + 1}:");
                ImGui.SameLine();
            }

            ImGui.Text(hovered
                ? $"{hoveredItem.Item.Name.ExtractText().StripSoftHyphen()}"
                : $"Equipped: {item.Item.Name.ExtractText().StripSoftHyphen()}");
            ImGui.SameLine();
            TextColored($"(iLvl {(hovered ? hoveredItem : item).Item.LevelItem.RowId})", iLvlDiff, true);

            _statDiff = 0;
            if (hovered)
                DrawItemCompare(hoveredItem, item, false);
            else
                DrawItemCompare(item, hoveredItem);

            if (_statDiff == 0)
            {
                ImGui.Separator();
                ImGui.TextUnformatted("There's no difference");
            }

            if (i + 1 >= equippedItems.Count) continue;

            ImGui.NewLine();
        }
    }

    /// <summary>
    /// Compares two items and displays their stat differences.
    /// </summary>
    /// <param name="itemA">The first item to compare.</param>
    /// <param name="itemB">The second item to compare.</param>
    /// <param name="equipped">Indicates whether the comparison is for equipped items.</param>
    private void DrawItemCompare(InventoryItem itemA, InventoryItem itemB, bool equipped = true)
    {
        DrawStat("Materia", itemB.Item.MateriaSlotCount - itemA.Item.MateriaSlotCount);

        // Map bonus value to type for comparison
        var bonusMapA = ItemComparer.BonusMap(itemA);
        var bonusMapB = ItemComparer.BonusMap(itemB);

        var bonusTypes = equipped
            ? bonusMapA.Keys.Union(bonusMapB.Keys)
            : bonusMapB.Keys.Union(bonusMapA.Keys);

        foreach (var bonusType in bonusTypes)
        {
            var valueA = bonusMapA.TryGetValue(bonusType, out var valA) ? valA : 0;
            var valueB = bonusMapB.TryGetValue(bonusType, out var valB) ? valB : 0;

            DrawStat(BaseParamToName(bonusType), valueA - valueB);
        }
    }

    /// <summary>
    /// Displays a single stat difference in the UI.
    /// </summary>
    /// <param name="name">The name of the stat.</param>
    /// <param name="value">The difference in the stat value.</param>
    private void DrawStat(string name, int value)
    {
        if (_statDiff == 0 && value != 0) ImGui.Separator();
        if (value != 0) _statDiff++;
        TextColored($"{name}: {(value > 0 ? $"+{value}" : $"{value}")}", value);
    }

    /// <summary>
    /// Displays text with a color based on the value (positive or negative).
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="value">The value that defines the color.</param>
    /// <param name="iLvl">Indicates whether the text is for the item level.</param>
    private static void TextColored(string text, int value, bool iLvl = false)
    {
        if (value == 0)
        {
            if (iLvl) ImGui.TextUnformatted(text);
            return;
        }

        var color = value > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
        ImGui.TextColored(color, text);
    }

    /// <summary>
    /// Converts a base parameter ID to its corresponding name.
    /// </summary>
    /// <param name="baseParam">The base parameter ID.</param>
    /// <returns>The name of the base parameter.</returns>
    private string BaseParamToName(uint baseParam)
    {
        return BaseParamSheet.GetRow(baseParam).Name.ExtractText().StripSoftHyphen();
    }
}