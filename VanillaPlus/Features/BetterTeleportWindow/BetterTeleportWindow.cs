using System;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using InteropGenerator.Runtime;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BetterTeleportWindow;

public unsafe class BetterTeleportWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Teleport Window",
        Description = "Replaces the games Teleport window with a better custom made version.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "BetterTeleportWindow.png";

    // Hyper Experimental lol. Game go boom, probably.
    public override bool IsExperimental => true;

    private nint? originalFactoryCreateAddress;

    private RaptureAtkModule.AddonFactoryInfo.CreateDelegate? pinnedFactoryCreateMethod;

    internal static TeleportAddon? CustomTeleportAddon;
    internal static BetterTeleportWindowConfig? Config;

    public override void OnEnable() {
        Config = BetterTeleportWindowConfig.Load();

        Services.Framework.RunOnFrameworkThread(() => {
            var factoryInfo = RaptureAtkModule.Instance()->GetAddonFactoryInfo("Teleport");
            if (factoryInfo is null) return;

            originalFactoryCreateAddress = (nint?)factoryInfo->Create;
            pinnedFactoryCreateMethod = CreateCustomAddon;
            factoryInfo->Create = (delegate* unmanaged<RaptureAtkModule*, CStringPointer, uint, AtkValue*, AtkUnitBase*>) Marshal.GetFunctionPointerForDelegate(pinnedFactoryCreateMethod);
        });
    }

    public override void OnDisable() {
        // Immediately Dispose Instance or game go boom.
        AgentTeleport.Instance()->Hide();

        CustomTeleportAddon?.Dispose();
        CustomTeleportAddon = null;

        Services.Framework.RunOnFrameworkThread(() => {
            var factoryInfo = RaptureAtkModule.Instance()->GetAddonFactoryInfo("Teleport");
            if (factoryInfo is null) return;

            // This is dumb, but the compiler will warn otherwise.
            if (originalFactoryCreateAddress is null) {
                factoryInfo->Create = null;
            }
            else {
                factoryInfo->Create = (delegate* unmanaged<RaptureAtkModule*, CStringPointer, uint, AtkValue*, AtkUnitBase*>) originalFactoryCreateAddress;
            }

            pinnedFactoryCreateMethod = null;
            originalFactoryCreateAddress = null;
        });
    }

    private static AtkUnitBase* CreateCustomAddon(RaptureAtkModule* raptureAtkModule, CStringPointer addonName, uint valueCount, AtkValue* values) {
        try {
            if (Config is null) return null;

            // As we currently have no way to recycle the previous addon instance, dispose it
            CustomTeleportAddon?.Dispose();

            // Then allocate a new addon instance.
            CustomTeleportAddon = new TeleportAddon(Config) {
                InternalName = "Teleport",
                Title = "Teleport",
                Size = new Vector2(700.0f, 600.0f),
            };

            CustomTeleportAddon.InitializeForAddonFactory(valueCount, values);

            return CustomTeleportAddon;
        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        }

        return null;
    }
}
