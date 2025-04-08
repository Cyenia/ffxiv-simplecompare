using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace SimpleCompare;

public class Service
{
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
}