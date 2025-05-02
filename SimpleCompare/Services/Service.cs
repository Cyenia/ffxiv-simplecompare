using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace SimpleCompare.Services;

/// <summary>
/// Provides access to various Dalamud plugin services.
/// </summary>
public class Service
{
    /// <summary>
    /// Gets the ClientState service, which provides information about the current state of the client.
    /// </summary>
    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;

    /// <summary>
    /// Gets the Data Manager service, which provides access to game data.
    /// </summary>
    [PluginService]
    public static IDataManager Data { get; private set; } = null!;

    /// <summary>
    /// Gets the Game GUI service, which provides methods for interacting with the game interface.
    /// </summary>
    [PluginService]
    public static IGameGui GameGui { get; private set; } = null!;

    /// <summary>
    /// Gets the plugin log service, which allows logging of messages for debugging or informational purposes.
    /// </summary>
    [PluginService]
    public static IPluginLog PluginLog { get; private set; } = null!;

    /// <summary>
    /// Gets the KeyState service, which provides information about the current state of input keys.
    /// </summary>
    [PluginService]
    public static IKeyState KeyState { get; private set; } = null!;
}