using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace VanillaPlus;

/// <summary>
/// Add any dalamud services that your modifications require here
/// </summary>
public class Services {
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] public static IClientState ClientState { get; set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
    [PluginService] public static IDutyState DutyState { get; set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; set; } = null!; 
    [PluginService] public static IFramework Framework { get; set; } = null!;
    [PluginService] public static IDataManager DataManager { get; set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; set; } = null!;
    [PluginService] public static IGameGui GameGui { get; set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; set; } = null!;
    [PluginService] public static IGameInventory GameInventory { get; set; } = null!;
    [PluginService] public static IFateTable FateTable { get; set; } = null!;
    [PluginService] public static IKeyState KeyState { get; set; } = null!;
    [PluginService] public static ICondition Condition { get; set; } = null!;
    [PluginService] public static ISeStringEvaluator SeStringEvaluator { get; set; } = null!;
    [PluginService] public static IContextMenu ContextMenu { get; set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; set; } = null!;
    

    // I dislike the name GameInteropProvider, so this is my mini rebellion on a bad name.
    // It's just an alias, use whichever you want.
    public static IGameInteropProvider Hooker => GameInteropProvider;
}
