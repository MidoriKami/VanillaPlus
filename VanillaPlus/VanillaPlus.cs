using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using KamiToolKit;
using VanillaPlus.Classes;
using VanillaPlus.InternalSystem;
using VanillaPlus.NativeElements.Addons;
using static VanillaPlus.Utilities.Localization;

namespace VanillaPlus;

public sealed class VanillaPlus : IAsyncDalamudPlugin {
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    public Task LoadAsync(CancellationToken cancellationToken) {
        PluginInterface.Create<Services>();

        PluginSystem.SystemConfig = SystemConfiguration.Load();

        KamiToolKitLibrary.Initialize(Services.PluginInterface, "VanillaPlus");
        KamiToolKitLibrary.SetResourceManager(Strings.ResourceManager);

        SetCultureInfo(Services.PluginInterface.UiLanguage);
        Services.PluginInterface.LanguageChanged += SetCultureInfo;

        PluginSystem.ModificationBrowserAddon = new ModificationBrowserAddon {
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

        Services.PluginInterface.UiBuilder.OpenConfigUi += PluginSystem.ModificationBrowserAddon.Open;
        Services.ClientState.Login += OnLogin;

        PluginSystem.KeyListener = new KeyListener();
        PluginSystem.ModificationManager = new ModificationManager();

        AutoOpenBrowser(PluginSystem.SystemConfig.IsDebugMode);

        if (Services.ClientState.IsLoggedIn) {
            OnLogin();
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() {
        PluginSystem.KeyListener.Dispose();

        Services.PluginInterface.UiBuilder.OpenConfigUi -= PluginSystem.ModificationBrowserAddon.Open;
        Services.ClientState.Login -= OnLogin;

        Services.CommandManager.RemoveHandler("/vanillaplus");
        Services.PluginInterface.LanguageChanged -= SetCultureInfo;

        PluginSystem.ModificationBrowserAddon.Dispose();
        PluginSystem.SeasonEventAddon.Dispose();

        await PluginSystem.ModificationManager.DisposeAsync();
        await Services.Framework.RunOnFrameworkThread(KamiToolKitLibrary.Dispose);
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

        PluginSystem.ModificationBrowserAddon.Open();
    }

    private static void CommandHandler(string command, string arguments) {
        if (command is not ("/vanillaplus" or "/plus")) return;

        switch (arguments.Split('/')) {
            case [""] or [] or null:
                PluginSystem.ModificationBrowserAddon.Open();
                break;

            case ["debug"]:
                PluginSystem.SystemConfig.IsDebugMode = !PluginSystem.SystemConfig.IsDebugMode;
                Services.ChatGui.Print($"Debug mode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Services.PluginLog.Info($"Debug mode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}");
                PluginSystem.SystemConfig.Save();

                if (!PluginSystem.ModificationBrowserAddon.IsOpen) {
                    PluginSystem.ModificationBrowserAddon.Open();
                }
                break;

            case ["safemode"]:
                PluginSystem.SystemConfig.SafeMode = !PluginSystem.SystemConfig.SafeMode;
                Services.ChatGui.Print($"Safemode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Services.PluginLog.Info($"Safemode is now {(PluginSystem.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}");
                PluginSystem.SystemConfig.Save();
                break;
        }
    }
}
