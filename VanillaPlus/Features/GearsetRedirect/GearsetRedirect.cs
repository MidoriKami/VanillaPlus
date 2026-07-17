using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Components.Configuration;
using KamiToolKit.Components.Search;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.GearsetRedirect.Addons;
using VanillaPlus.Features.GearsetRedirect.Config;
using VanillaPlus.Features.GearsetRedirect.Nodes;

namespace VanillaPlus.Features.GearsetRedirect;

public class GearsetRedirect : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_GearsetRedirect,
        Description = Strings.ModificationDescription_GearsetRedirect,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    internal static NewRedirectionAddon? CreateRedirectionAddon;

    private Hook<RaptureGearsetModule.Delegates.EquipGearset>? gearsetChangedHook;
    private GearsetRedirectConfig? config;
    private ConfigurationAddon<GearsetRedirectionEntry, GearsetEntryListItemNode, GearsetEntryConfigNode>? configWindow;

    private GearsetSearchAddon? gearsetSearchAddon;

    public override async Task OnEnableAsync() {
        config = await GearsetRedirectConfig.Load();

        unsafe {
            CreateRedirectionAddon = new NewRedirectionAddon {
                Size = new Vector2(400.0f, 200.0f),
                InternalName = "CreateGearsetRedirection",
                Title = "New Gearset Redirection",
            };

            gearsetSearchAddon = new GearsetSearchAddon {
                Size = new Vector2(275.0f, 555.0f),
                InternalName = "GearsetSearch",
                Title = Strings.SearchAddon_GearsetTitle,
                AllowMultiselect = true,
            };

            configWindow = new ConfigurationAddon<GearsetRedirectionEntry, GearsetEntryListItemNode, GearsetEntryConfigNode> {
                Size = new Vector2(600.0f, 525.0f),
                InternalName = "GearsetRedirectConfig",
                Title = Strings.GearsetRedirect_ConfigTitle,
                OptionsList = config.GearsetEntries,
                SaveConfig = () => Task.Run(config.Save),
                GetEntrySearchString = entry => RaptureGearsetModule.Instance()->GetGearset(entry.TargetGearsetId)->NameString,
                AddClicked = OnAddClicked,
                RemoveClicked = OnRemoveClicked,
            };

            gearsetChangedHook = Service<IGameInteropProvider>.Get().HookFromAddress<RaptureGearsetModule.Delegates.EquipGearset>(RaptureGearsetModule.Addresses.EquipGearset.Value, OnGearsetChanged);
            gearsetChangedHook?.Enable();
        }

        OpenConfigAction = () => {
            if (Service<IClientState>.Get().IsLoggedIn) {
                configWindow.Toggle();
            }
        };
    }

    public override async Task OnDisableAsync() {
        gearsetChangedHook?.Dispose();
        gearsetChangedHook = null;

        await Task.WhenAll(
            configWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            gearsetSearchAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            CreateRedirectionAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );
        configWindow = null;
        gearsetSearchAddon = null;

        config = null;
    }

    private void OnRemoveClicked(GearsetRedirectionEntry removedEntry) {
        if (config is null) return;

        config.GearsetEntries.Remove(removedEntry);
        Task.Run(config.Save);
    }

    private void OnAddClicked() {
        if (config is null) return;

        gearsetSearchAddon?.ConfirmedSelections = selections => {
            foreach (var gearset in selections) {
                if (!config.GearsetEntries.Any(entry => entry.TargetGearsetId == gearset.Id)) {
                    config.GearsetEntries.Add(new GearsetRedirectionEntry {
                        TargetGearsetId = gearset.Id,
                        Redirections = [],
                    });
                }
            }

            configWindow?.OptionsList = config.GearsetEntries;
            Task.Run(config.Save);
        };

        gearsetSearchAddon?.Open();
    }

    private unsafe int OnGearsetChanged(RaptureGearsetModule* thisPtr, int gearsetId, byte glamourPlateId) {
        try {
            if (config?.GearsetEntries.FirstOrDefault(entry => entry.TargetGearsetId == gearsetId) is { } redirection) {
                var targetRedirection = redirection.Redirections.FirstOrDefault(info => Service<IClientState>.Get().TerritoryType == info.TerritoryType);
                if (targetRedirection is not null) {
                    gearsetId = targetRedirection.AlternateGearsetId;
                }
            }
        }
        catch (Exception e) {
            Service<IPluginLog>.Get().Exception(e);
        }

        return gearsetChangedHook!.Original(thisPtr, gearsetId, glamourPlateId);
    }
}
