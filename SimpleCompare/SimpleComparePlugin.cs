using System;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using SimpleCompare.Models;
using SimpleCompare.Services;
using SimpleCompare.UI;

namespace SimpleCompare;

/// <summary>
/// Represents the main entry point for the SimpleCompare plugin.
/// </summary>
public sealed class SimpleComparePlugin : IDalamudPlugin
{
    /// <summary>
    /// Gets the Dalamud plugin interface used to create services and manage the plugin lifecycle.
    /// </summary>
    private readonly IDalamudPluginInterface _pluginInterface;

    /// <summary>
    /// Gets the UI component of the plugin responsible for rendering the interface.
    /// </summary>
    private readonly PluginUI _pluginUi;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleComparePlugin"/> class.
    /// </summary>
    /// <param name="pluginInterface">The Dalamud plugin interface.</param>
    public SimpleComparePlugin(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;

        // Creating and Initializing Services
        pluginInterface.Create<Service>();
        _pluginUi = new PluginUI();

        // Register UI drawing and item hover events
        _pluginInterface.UiBuilder.Draw += DrawUI;
        Service.GameGui.HoveredItemChanged += OnItemHover;
    }

    /// <summary>
    /// Handles the event when an item is hovered in the game.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="itemId">The ID of the hovered item.</param>
    private void OnItemHover(object? sender, ulong itemId)
    {
        _pluginUi.HoveredItem = ProcessItemId(itemId);
    }

    /// <summary>
    /// Processes the item ID and retrieves the corresponding inventory item.
    /// </summary>
    /// <param name="itemId">The ID of the item.</param>
    /// <returns>The inventory item or null if invalid.</returns>
    private static InventoryItem? ProcessItemId(ulong itemId)
    {
        if (itemId is > 2_000_000 or <= 0) return null;

        var wasHq = false;
        if (itemId > 1_000_000)
        {
            wasHq = true;
            itemId -= 1_000_000;
        }

        var item = Service.Data.GetExcelSheet<Item>().GetRow((uint)itemId);
        return new InventoryItem(item, wasHq);
    }

    /// <summary>
    /// Disposes of the plugin, cleaning up resources and unregistering events.
    /// </summary>
    public void Dispose()
    {
        _pluginUi.Dispose();
        _pluginInterface.UiBuilder.Draw -= DrawUI;
        Service.GameGui.HoveredItemChanged -= OnItemHover;
    }

    /// <summary>
    /// Draws the UI of the plugin when the client is logged in.
    /// </summary>
    private void DrawUI()
    {
        if (!Service.ClientState.IsLoggedIn) return;

        // Prevent game crashes by catching exceptions
        try
        {
            _pluginUi.Draw();
        }
        catch (Exception ex)
        {
            Service.PluginLog.Fatal($"An error occurred while drawing the UI: {ex}");
        }
    }
}