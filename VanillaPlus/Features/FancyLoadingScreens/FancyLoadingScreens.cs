using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using KamiToolKit.Timelines;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FancyLoadingScreens;

public class FancyLoadingScreens : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FancyLoadingScreens,
        Description = Strings.ModificationDescription_FancyLoadingScreens,
        Type = ModificationType.UserInterface,
        Authors = ["goat", "MapleRecall", "MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("Dalamud.LoadingImage"),
    };

    private Hook<Telepo.Delegates.Teleport>? teleportHook;

    private AddonController? locationTitleController;
    private TimelineNode<SimpleImageNode>? artworkImageNode;
    private bool isTeleporting;

    public override async Task OnEnableAsync() {
        unsafe {
            teleportHook = Services.Hooker.HookFromAddress<Telepo.Delegates.Teleport>(Telepo.MemberFunctionPointers.Teleport, OnTeleport);
            teleportHook?.Enable();

            locationTitleController = new AddonController {
                AddonName = "_LocationTitle",
                OnSetup = OnLocationTitleSetup,
                OnDraw = OnLocationTitleDraw,
                OnFinalize = OnLocationTitleFinalize,
            };
        }

        await Services.Framework.RunSafely(() => locationTitleController.Enable());

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostHide, "_LocationTitle", OnLoadingScreenHide);
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public override async Task OnDisableAsync() {
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Services.AddonLifecycle.UnregisterListener(OnLoadingScreenHide);

        teleportHook?.Dispose();
        teleportHook = null;

        await Services.Framework.RunSafely(() => {
            locationTitleController?.Dispose();
        });
        locationTitleController = null;

        artworkImageNode = null;
    }

    private unsafe void OnLocationTitleSetup(AtkUnitBase* addon) {
        artworkImageNode = new TimelineNode<SimpleImageNode> {
            ContentNode = {
                FitTexture = true,
            },

            LabelsetTimeline = new TimelineBuilder()
                .BeginFrameSet(1, 480)
                .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
                .AddLabel(480, 0, AtkTimelineJumpBehavior.PlayOnce, 0)
                .EndFrameSet()
                .Build(),

            ContentTimeline = new TimelineBuilder()
                .BeginFrameSet(1, 480)
                .AddFrame(1, scale: new Vector2(1.0f, 1.0f), alpha: 0)
                .AddFrame(60, scale: new Vector2(1.1f, 1.1f), alpha: 255)
                .AddFrame(480, scale: new Vector2(1.4f, 1.4f), alpha: 255)
                .EndFrameSet()
                .Build(),
        };
        artworkImageNode.AttachNode(addon, NodePosition.AsFirstChild);
    }

    private unsafe void OnLocationTitleDraw(AtkUnitBase* addon) {
        var screenSize = (Vector2) AtkStage.Instance()->ScreenSize;
        var rootScale = addon->RootNode->Scale;

        artworkImageNode?.Position = -addon->RootNode->Position / rootScale;
        artworkImageNode?.Size = screenSize / rootScale;
        artworkImageNode?.ContentNode.Origin = screenSize / rootScale / 2.0f;
    }

    private unsafe void OnLocationTitleFinalize(AtkUnitBase* addon) {
        artworkImageNode?.Dispose();
        artworkImageNode = null;
    }

    private void OnTerritoryChanged(uint territoryId) {
        if (!isTeleporting) {
            SetLoadingScreenImage(territoryId);
        }
    }

    private void SetLoadingScreenImage(uint territoryId) {
        if (artworkImageNode is null) return;

        if (!Services.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territory)) return;
        artworkImageNode?.ContentNode.TexturePath = territory.LoadingImagePath;
        artworkImageNode?.Timeline?.PlayAnimation(1, true);
        artworkImageNode?.IsVisible = true;
    }

    private void OnLoadingScreenHide(AddonEvent type, AddonArgs args) {
        artworkImageNode?.IsVisible = false;
        isTeleporting = false;
    }

    private unsafe bool OnTeleport(Telepo* thisPtr, uint aetheryteId, byte subIndex) {
        var accepted = teleportHook!.Original(thisPtr, aetheryteId, subIndex);

        try {
            if (accepted && Services.DataManager.GetExcelSheet<Aetheryte>().TryGetRow(aetheryteId, out var aetheryte)) {
                isTeleporting = true;
                SetLoadingScreenImage(aetheryte.Territory.RowId);
            }
        }
        catch (Exception exception) {
            Services.PluginLog.Exception(exception);
        }

        return accepted;
    }
}
