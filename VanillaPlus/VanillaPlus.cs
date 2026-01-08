using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using KamiToolKit;
using VanillaPlus.Classes;
using VanillaPlus.InternalSystem;
using static VanillaPlus.Utilities.Localization;

namespace VanillaPlus;

public sealed class VanillaPlus : IDalamudPlugin {
    public VanillaPlus(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services>();
        PluginSystem.SystemConfig = SystemConfiguration.Load();

        SetCultureInfo(pluginInterface.UiLanguage);
        pluginInterface.LanguageChanged += SetCultureInfo;

        KamiToolKitLibrary.Initialize(pluginInterface);

        PluginSystem.AddonModificationBrowser = new AddonModificationBrowser {
            InternalName = "VanillaPlusConfig",
            Title = Strings.ModificationBrowserTitle,
            Size = new Vector2(836.0f, 650.0f),
        };

        Services.CommandManager.AddHandler("/vanillaplus", new CommandInfo(Handler) {
            DisplayOrder = 1,
            ShowInHelp = true,
            HelpMessage = Strings.CommandHelpOpenBrowser,
        });

        Services.CommandManager.AddHandler("/plus", new CommandInfo(Handler) {
            DisplayOrder = 2,
            ShowInHelp = true,
            HelpMessage = Strings.CommandHelpOpenBrowser,
        });

        PluginSystem.WindowSystem = new WindowSystem("VanillaPlus");
        Services.PluginInterface.UiBuilder.Draw += PluginSystem.WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OpenModificationBrowser;

        PluginSystem.KeyListener = new KeyListener();
        PluginSystem.ModificationManager = new ModificationManager();

        AutoOpenBrowser(PluginSystem.SystemConfig.IsDebugMode);
    }

    public void Dispose() {
        PluginSystem.KeyListener.Dispose();
        PluginSystem.ModificationManager.Dispose();

        foreach (var (_, agentInfo) in AgentInterfaceExtensions.HookedAgents) {
            agentInfo.ReceiveEventHook?.Dispose();
        }
        AgentInterfaceExtensions.HookedAgents.Clear();

        Services.PluginInterface.UiBuilder.OpenConfigUi -= OpenModificationBrowser;
        Services.PluginInterface.UiBuilder.Draw -= PluginSystem.WindowSystem.Draw;
        PluginSystem.WindowSystem.RemoveAllWindows();

        Services.CommandManager.RemoveHandler("/vanillaplus");
        Services.PluginInterface.LanguageChanged -= SetCultureInfo;

        PluginSystem.AddonModificationBrowser.Dispose();

        KamiToolKitLibrary.Dispose();
    }

    [Conditional("DEBUG")]
    private static void AutoOpenBrowser(bool enabled) {
        if (!enabled) return;

        PluginSystem.AddonModificationBrowser.Open();
    }

    private static void Handler(string command, string arguments) {
        if (command is not ("/vanillaplus" or "/plus")) return;
        
        switch (arguments) {
            case "" or null:
                PluginSystem.AddonModificationBrowser.Open();
                break;
            
            case "debug":
                PluginSystem.SystemConfig.IsDebugMode = !PluginSystem.SystemConfig.IsDebugMode;
                Services.ChatGui.Print($"Debug mode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled": "Disabled")}", "VanillaPlus");
                PluginSystem.SystemConfig.Save();
                break;
        }
    }

    private void OpenModificationBrowser()
        => PluginSystem.AddonModificationBrowser.Open();
}
