using Dalamud.Plugin;
using Lumina.Excel.Sheets;

namespace SimpleCompare;

public sealed class Plugin : IDalamudPlugin
{
    private IDalamudPluginInterface PluginInterface { get; init; }
    private PluginUI PluginUi { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;

        pluginInterface.Create<Service>();
        PluginUi = new PluginUI();

        PluginInterface.UiBuilder.Draw += DrawUI;
        Service.GameGui.HoveredItemChanged += OnItemHover;
    }

    private void OnItemHover(object? sender, ulong itemId)
    {
        if (itemId > 2_000_000)
        {
            PluginUi.InvItem = null;
            return;
        }

        var wasHq = false;
        if (itemId > 1_000_000)
        {
            wasHq = true;
            itemId -= 1_000_000;
        }

        var item = Service.Data.GetExcelSheet<Item>().GetRow((uint)itemId);

        PluginUi.InvItem = new InvItem(item, wasHq);
    }

    public void Dispose()
    {
        PluginUi.Dispose();
        Service.GameGui.HoveredItemChanged -= OnItemHover;
    }

    private void DrawUI()
    {
        if (!Service.ClientState.IsLoggedIn) return;

        // dont crash game!
        try
        {
            PluginUi.Draw();
        }
        catch (System.Exception ex)
        {
            Service.PluginLog.Fatal(ex.ToString());
        }
    }
}