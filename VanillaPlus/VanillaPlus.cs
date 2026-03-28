using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using KamiToolKit;
using VanillaPlus.Classes;
using VanillaPlus.InternalSystem;
using VanillaPlus.NativeElements.Addons;
using static VanillaPlus.Utilities.Localization;

namespace VanillaPlus;

public sealed class VanillaPlus : IDalamudPlugin {
    public VanillaPlus(IDalamudPluginInterface pluginInterface) {
        DebugDelayStartup();

        pluginInterface.Create<Services>();
        PluginSystem.SystemConfig = SystemConfiguration.Load();

        SetCultureInfo(pluginInterface.UiLanguage);
        pluginInterface.LanguageChanged += SetCultureInfo;

        KamiToolKitLibrary.Initialize(pluginInterface, "VanillaPlus");

        PluginSystem.AddonModificationBrowser = new AddonModificationBrowser {
            InternalName = "VanillaPlusConfig",
            Title = Strings.ModificationBrowserTitle,
            Size = new Vector2(836.0f, 650.0f),
        };

        PluginSystem.SeasonEventAddon = new SeasonEventAddon {
            InternalName = "SeasonalEventNotice",
            Title = "Seasonal Event Notice",
            Size = new Vector2(500.0f, 275.0f),
        };

        Services.CommandManager.AddHandler("/vanillaplus", new CommandInfo(CommandHandler) {
            DisplayOrder = 1,
            ShowInHelp = true,
            HelpMessage = Strings.CommandHelpOpenBrowser,
        });

        Services.CommandManager.AddHandler("/plus", new CommandInfo(CommandHandler) {
            DisplayOrder = 2,
            ShowInHelp = true,
            HelpMessage = Strings.CommandHelpOpenBrowser,
        });

        Services.PluginInterface.UiBuilder.OpenConfigUi += PluginSystem.AddonModificationBrowser.Open;
        Services.ClientState.Login += OnLogin;

        PluginSystem.KeyListener = new KeyListener();
        PluginSystem.ModificationManager = new ModificationManager();

        AutoOpenBrowser(PluginSystem.SystemConfig.IsDebugMode);

        if (Services.ClientState.IsLoggedIn) {
            OnLogin();
        }
    }

    public void Dispose() {
        PluginSystem.KeyListener.Dispose();
        PluginSystem.ModificationManager.Dispose();

        Services.PluginInterface.UiBuilder.OpenConfigUi -= PluginSystem.AddonModificationBrowser.Open;
        Services.ClientState.Login -= OnLogin;

        Services.CommandManager.RemoveHandler("/vanillaplus");
        Services.PluginInterface.LanguageChanged -= SetCultureInfo;

        PluginSystem.AddonModificationBrowser.Dispose();

        KamiToolKitLibrary.Dispose();
    }
    
    private void OnLogin() {
        if (DateTime.Now.IsSeasonalEvent && DateTime.Now.Date > PluginSystem.SystemConfig.LastSeasonalNotice.Date) {
            PluginSystem.SeasonEventAddon.Open();
            PluginSystem.SystemConfig.LastSeasonalNotice = DateTime.Now.Date;
            PluginSystem.SystemConfig.Save();
        }
    }

    [Conditional("DEBUG")]
    private static void AutoOpenBrowser(bool enabled) {
        if (!enabled) return;

        PluginSystem.AddonModificationBrowser.Open();
    }

    [Conditional("DEBUG")]
    private static void DebugDelayStartup()
        => Thread.Sleep(TimeSpan.FromMilliseconds(500));

    private static void CommandHandler(string command, string arguments) {
        if (command is not ("/vanillaplus" or "/plus")) return;
        
        switch (arguments) {
            case "" or null:
                PluginSystem.AddonModificationBrowser.Open();
                break;
            
            case "debug":
                PluginSystem.SystemConfig.IsDebugMode = !PluginSystem.SystemConfig.IsDebugMode;
                Services.ChatGui.Print($"Debug mode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled": "Disabled")}", "VanillaPlus");
                Services.PluginLog.Info($"Debug mode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled": "Disabled")}");
                PluginSystem.SystemConfig.Save();

                if (!PluginSystem.AddonModificationBrowser.IsOpen) {
                    PluginSystem.AddonModificationBrowser.Open();
                }
                break;
        }
    }
}
