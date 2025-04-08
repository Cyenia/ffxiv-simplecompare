using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace SimpleCompare
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "SimpleCompare";

        private const string commandName = "/simplecompare";

        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            pluginInterface.Create<Service>();


            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.PluginUi = new PluginUI(this.Configuration);

            this.PluginInterface.UiBuilder.Draw += DrawUI;

            
            Service.GameGui.HoveredItemChanged += this.OnItemHover;
        }

        private void OnItemHover(object? sender, ulong itemId)
        {
            if (itemId > 2_000_000)
            {
                this.PluginUi.InvItem = null;
                return;
            }

            bool wasHQ = false;
            if (itemId > 1_000_000)
            {
                wasHQ = true;
                itemId -= 1_000_000;
            }

            var item = Service.Data.GetExcelSheet<Item>().GetRow((uint)itemId);

            this.PluginUi.InvItem = new InvItem(item, wasHQ);
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            Service.GameGui.HoveredItemChanged -= this.OnItemHover;
        }


        private void DrawUI()
        {
            if (Service.ClientState != null && Service.ClientState.IsLoggedIn)
            {
                // dont crash game!
                try
                {
                    this.PluginUi.Draw();
                }
                catch (System.Exception ex)
                {
                    Service.PluginLog.Fatal(ex.ToString());
                }
            }
        }

    }
}
