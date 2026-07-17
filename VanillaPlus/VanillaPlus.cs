using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;
using VanillaPlus.Classes;
using VanillaPlus.Native.Addons;
using static VanillaPlus.Utilities.Localization;

namespace VanillaPlus;

public sealed class VanillaPlus : IAsyncDalamudPlugin {
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    public async Task LoadAsync(CancellationToken cancellationToken) {
        System.SystemConfig = await SystemConfiguration.Load();
        if (System.SystemConfig.SafeMode) {
            Service<IPluginLog>.Get().Warning("VanillaPlus is in safe mode. Modules will be loaded sequentially.");
        }

        KamiToolKitLibrary.Initialize(PluginInterface, "VanillaPlus");
        KamiToolKitLibrary.SetResourceManager(Strings.ResourceManager);

        SetCultureInfo(PluginInterface.UiLanguage);
        PluginInterface.LanguageChanged += SetCultureInfo;

        System.ModificationBrowserAddon = new ModificationBrowserAddon {
            InternalName = "VanillaPlusConfig",
            Title = Strings.ModificationBrowserTitle,
            Size = new Vector2(836.0f, 650.0f),
        };

        System.SeasonEventAddon = new SeasonEventAddon {
            InternalName = "SeasonalEventNotice",
            Title = Strings.SeasonEventNotice_Title,
            Size = new Vector2(500.0f, 275.0f),
        };

        Service<ICommandManager>.Get().AddHandler("/vanillaplus", new CommandInfo(CommandHandler) {
            DisplayOrder = 1,
            ShowInHelp = true,
            HelpMessage = Strings.CommandHelpOpenBrowser,
        });

        Service<ICommandManager>.Get().AddHandler("/plus", new CommandInfo(CommandHandler) {
            DisplayOrder = 2,
            ShowInHelp = true,
            HelpMessage = Strings.CommandHelpOpenBrowser,
        });

        PluginInterface.UiBuilder.OpenConfigUi += System.ModificationBrowserAddon.Open;
        Service<IClientState>.Get().Login += OnLogin;

        System.KeyListener = new KeyListener();
        System.ModificationManager = new ModificationManager();

        await System.ModificationManager.LoadModulesAsync();

        AutoOpenBrowser(System.SystemConfig.IsDebugMode);

        if (Service<IClientState>.Get().IsLoggedIn) {
            OnLogin();
        }
    }

    public async ValueTask DisposeAsync() {
        System.KeyListener.Dispose();

        Service<ICommandManager>.Get().RemoveHandler("/vanillaplus");
        Service<ICommandManager>.Get().RemoveHandler("/plus");

        PluginInterface.UiBuilder.OpenConfigUi -= System.ModificationBrowserAddon.Open;
        Service<IClientState>.Get().Login -= OnLogin;

        PluginInterface.LanguageChanged -= SetCultureInfo;

        await System.ModificationBrowserAddon.DisposeAsync();
        await System.SeasonEventAddon.DisposeAsync();
        await System.ModificationManager.DisposeAsync();
        await Service<IFramework>.Get().RunOnFrameworkThread(KamiToolKitLibrary.Dispose);
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

        Service<IFramework>.Get().RunSafely(System.ModificationBrowserAddon.Open);
    }

    private static void CommandHandler(string command, string arguments) {
        if (command is not ("/vanillaplus" or "/plus")) return;

        switch (arguments.Split('/')) {
            case [""] or [] or null:
                System.ModificationBrowserAddon.Toggle();
                break;

            case ["debug"]:
                System.SystemConfig.IsDebugMode = !System.SystemConfig.IsDebugMode;
                Service<IChatGui>.Get().Print($"Debug mode is now {(System.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Service<IPluginLog>.Get().Info($"Debug mode is now {(System.SystemConfig.IsDebugMode ? "Enabled" : "Disabled")}");
                Task.Run(System.SystemConfig.Save);

                if (!System.ModificationBrowserAddon.IsOpen) {
                    System.ModificationBrowserAddon.Open();
                }
                break;

            case ["safemode"]:
                System.SystemConfig.SafeMode = !System.SystemConfig.SafeMode;
                Service<IChatGui>.Get().Print($"Safemode is now {(System.SystemConfig.SafeMode ? "Enabled" : "Disabled")}", "VanillaPlus");
                Service<IPluginLog>.Get().Info($"Safemode is now {(System.SystemConfig.SafeMode ? "Enabled" : "Disabled")}");
                Task.Run(System.SystemConfig.Save);
                break;
        }
    }
}
