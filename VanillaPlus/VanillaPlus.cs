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

    public async Task LoadAsync(CancellationToken cancellationToken) {
        PluginInterface.Create<Services>();

        System.SystemConfig = await SystemConfiguration.Load();
        if (System.SystemConfig.SafeMode) {
            Services.PluginLog.InternalWarning("VanillaPlus is in safe mode. Modules will not be loaded/unloaded in parallel.");
        }

        KamiToolKitLibrary.Initialize(Services.PluginInterface, "VanillaPlus");
        KamiToolKitLibrary.SetResourceManager(Strings.ResourceManager);

        SetCultureInfo(Services.PluginInterface.UiLanguage);
        Services.PluginInterface.LanguageChanged += SetCultureInfo;

        System.ModificationBrowserAddon = new ModificationBrowserAddon {
            InternalName = "VanillaPlusConfig",
            Title = Strings.ModificationBrowserTitle,
            Size = new Vector2(836.0f, 650.0f),
        };

        System.SeasonEventAddon = new SeasonEventAddon {
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

        Services.PluginInterface.UiBuilder.OpenConfigUi += System.ModificationBrowserAddon.Open;
        Services.ClientState.Login += OnLogin;

        System.KeyListener = new KeyListener();
        System.ModificationManager = new ModificationManager();

        AutoOpenBrowser(System.SystemConfig.IsDebugMode);

        if (Services.ClientState.IsLoggedIn) {
            OnLogin();
        }
    }

    public async ValueTask DisposeAsync() {
        System.KeyListener.Dispose();

        Services.PluginInterface.UiBuilder.OpenConfigUi -= System.ModificationBrowserAddon.Open;
        Services.ClientState.Login -= OnLogin;

        Services.CommandManager.RemoveHandler("/vanillaplus");
        Services.PluginInterface.LanguageChanged -= SetCultureInfo;

        if (!Services.Framework.IsFrameworkUnloading) {
            await System.ModificationBrowserAddon.DisposeAsync();
            await System.SeasonEventAddon.DisposeAsync();
            await System.ModificationManager.DisposeAsync();
        }

        await Services.Framework.RunOnFrameworkThread(KamiToolKitLibrary.Dispose);
    }

    private void OnLogin() {
        if (DateTime.Now.IsSeasonalEvent && DateTime.Now.Date > System.SystemConfig.LastSeasonalNotice.Date) {
            System.SeasonEventAddon.Open();
            System.SystemConfig.LastSeasonalNotice = DateTime.Now.Date;
            Task.Run(System.SystemConfig.Save);
        }
    }

    [Conditional("DEBUG")]
    private static void AutoOpenBrowser(bool enabled) {
        if (!enabled) return;

        Services.Framework.Run(System.ModificationBrowserAddon.Open);
    }

    private static void CommandHandler(string command, string arguments) {
        if (command is not ("/vanillaplus" or "/plus")) return;

        switch (arguments.Split('/')) {
            case [""] or [] or null:
                System.ModificationBrowserAddon.Toggle();
                break;

            case ["debug"]:
                System.SystemConfig.IsDebugMode = !System.SystemConfig.IsDebugMode;
                Services.ChatGui.Print($"Debug mode is now {(System.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Services.PluginLog.InternalInfo($"Debug mode is now {(System.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}");
                Task.Run(System.SystemConfig.Save);

                if (!System.ModificationBrowserAddon.IsOpen) {
                    System.ModificationBrowserAddon.Open();
                }
                break;

            case ["safemode"]:
                System.SystemConfig.SafeMode = !System.SystemConfig.SafeMode;
                Services.ChatGui.Print($"Safemode is now {(System.SystemConfig.SafeMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Services.PluginLog.InternalInfo($"Safemode is now {(System.SystemConfig.SafeMode ? "Enabled" : "Disabled")}");
                Task.Run(System.SystemConfig.Save);
                break;
        }
    }
}
