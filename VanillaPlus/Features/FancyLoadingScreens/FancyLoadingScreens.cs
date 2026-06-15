using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes.Simplified;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.FancyLoadingScreens;

public class FancyLoadingScreens : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FancyLoadingScreens,
        Description = Strings.ModificationDescription_FancyLoadingScreens,
        Type = ModificationType.UserInterface,
        Authors = ["goat", "Maple", "MapleRecall"],
        CompatibilityModule = new PluginCompatibilityModule("Dalamud.LoadingImage"),
    };

    public override bool IsExperimental => true;

    private const string LoadingBannerAddonName = "_LocationTitle";
    private const uint ArtNodeId = 6;
    private const float LoadingArtAspectRatio = 16.0f / 9.0f;

    private const string NowLoadingAddonName = "NowLoading";

    private FancyLoadingScreensConfig? config;
    private ConfigAddon? configAddon;

    private AddonController? nowLoadingController;
    private SimpleImageNode? nowLoadingArtNode;
    private bool nowLoadingArtGeometryCached;
    private uint appliedNowLoadingTerritoryId;
    private string? appliedNowLoadingTexturePath;
    private float nowLoadingArtAlpha;

    private bool wasLoading;
    private uint sourceTerritoryId;

    // Early teleport destination, retired once GameMain reports the real destination mid-load.
    private uint hookedTerritoryId;
    private uint appliedTerritoryId;
    private string? appliedTexturePath;

    // Cached once per load so the parent's zoom animation does not cause jitter.
    private bool artGeometryCached;
    private float cachedArtWidth;
    private float cachedArtHeight;
    private float cachedArtPosX;
    private float cachedArtPosY;

    // Walk transitions discover the destination mid-load, so they need a short fade-in.
    private const float FadeInDurationSeconds = 0.4f;
    private float currentAlpha;

    private Hook<Telepo.Delegates.Teleport>? teleportHook;

    public override async Task OnEnableAsync() {
        config = await FancyLoadingScreensConfig.Load();

        configAddon = new ConfigAddon {
            Config = config,
            InternalName = "FancyLoadingScreensConfig",
            Title = Strings.FancyLoadingScreens_ConfigTitle,
        };
        configAddon.AddCategory(Strings.FancyLoadingScreens_CategoryGeneral)
            .AddCheckbox(Strings.FancyLoadingScreens_LabelInstancedLoad, nameof(config.ShowOnInstancedLoad))
            .AddTooltip(Strings.FancyLoadingScreens_LabelInstancedLoadNote);
        OpenConfigAction = configAddon.Toggle;

        unsafe {
            teleportHook = Services.Hooker.HookFromAddress<Telepo.Delegates.Teleport>(Telepo.MemberFunctionPointers.Teleport, OnTeleport);
            teleportHook?.Enable();

            nowLoadingController = new AddonController {
                AddonName = NowLoadingAddonName,
                OnFinalize = OnNowLoadingFinalize,
            };
        }

        await Services.Framework.Run(nowLoadingController.Enable);

        Services.Framework.Update += OnFrameworkUpdate;
    }

    public override async Task OnDisableAsync() {
        Services.Framework.Update -= OnFrameworkUpdate;

        teleportHook?.Dispose();
        teleportHook = null;

        await Services.Framework.Run(() => {
            ForceHideArtNode();
            nowLoadingController?.Dispose();
            nowLoadingArtNode?.Dispose();
        });
        nowLoadingController = null;
        nowLoadingArtNode = null;

        await (configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configAddon = null;
        config = null;

        wasLoading = false;
        sourceTerritoryId = 0;
        hookedTerritoryId = 0;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        try {
            var isLoading = Services.Condition.IsBetweenAreas;
            var deltaSeconds = (float)framework.UpdateDelta.TotalSeconds;

            if (isLoading && !wasLoading) {
                ForceHideArtNode();
                ResetLoadState();
                sourceTerritoryId = Services.ClientState.TerritoryType;
            }

            if (isLoading) {
                TryApplyDestinationArt(deltaSeconds);
                TryApplyBlackScreenArt(deltaSeconds);
            }

            if (!isLoading && wasLoading) {
                ResetLoadState();
                hookedTerritoryId = 0;
            }

            wasLoading = isLoading;
        }
        catch (Exception exception) {
            Services.Framework.Update -= OnFrameworkUpdate;
            ForceHideArtNode();
            Services.PluginLog.Exception(exception);
        }
    }

    private unsafe void TryApplyDestinationArt(float deltaSeconds) {
        var (destinationTerritoryId, fromHook) = ResolveDestinationTerritory();
        if (destinationTerritoryId is 0) return;

        var bannerAddon = RaptureAtkUnitManager.Instance()->GetAddonByName(LoadingBannerAddonName);
        if (bannerAddon is null || !bannerAddon->IsVisible) return;

        var artNode = bannerAddon->GetNodeById<AtkImageNode>(ArtNodeId);
        if (artNode is null) return;
        if (artNode->PartsList is null) return;

        if (destinationTerritoryId != appliedTerritoryId || appliedTexturePath is null) {
            var texturePath = BuildLoadingImagePath(destinationTerritoryId);
            if (texturePath is null) return;

            artNode->LoadTexture(texturePath);
            appliedTerritoryId = destinationTerritoryId;
            appliedTexturePath = texturePath;
            artGeometryCached = false;

            // Teleports ride the loading screen fade; walk transitions need their own fade-in.
            currentAlpha = fromHook ? 255.0f : 0.0f;
        }

        if (currentAlpha < 255.0f) {
            var step = 255.0f / FadeInDurationSeconds * Math.Max(deltaSeconds, 0.0f);
            currentAlpha = Math.Min(255.0f, currentAlpha + step);
        }

        var resNode = (AtkResNode*)artNode;
        ApplyArtGeometry(resNode);
        resNode->SetAlpha((byte)currentAlpha);
        resNode->ToggleVisibility(currentAlpha >= 1.0f);
    }

    private unsafe void TryApplyBlackScreenArt(float deltaSeconds) {
        var bannerAddon = RaptureAtkUnitManager.Instance()->GetAddonByName(LoadingBannerAddonName);
        var bannerVisible = bannerAddon is not null && bannerAddon->IsVisible;

        // The destination is only published mid-load; the banner path handles non-black-screen loads.
        var destinationTerritoryId = GameMain.Instance()->NextTerritoryTypeId;
        var isCrossTerritory = destinationTerritoryId is not 0 && destinationTerritoryId != sourceTerritoryId;

        var showArt = config is { ShowOnInstancedLoad: true }
                      && Services.ClientState.IsLoggedIn
                      && !bannerVisible
                      && isCrossTerritory;

        if (!showArt) {
            if (nowLoadingArtNode is not null) {
                nowLoadingArtNode.IsVisible = false;
            }
            return;
        }

        // NowLoading persists after login, so the node is attached lazily on the first qualifying load.
        if (nowLoadingArtNode is null) {
            EnsureNowLoadingArtNode();
            if (nowLoadingArtNode is null) return;
        }

        if (destinationTerritoryId != appliedNowLoadingTerritoryId || appliedNowLoadingTexturePath is null) {
            var texturePath = BuildLoadingImagePath(destinationTerritoryId, skipInstancedContent: false);
            if (texturePath is null) {
                nowLoadingArtNode.IsVisible = false;
                return;
            }

            nowLoadingArtNode.LoadTexture(texturePath);
            appliedNowLoadingTerritoryId = destinationTerritoryId;
            appliedNowLoadingTexturePath = texturePath;
            nowLoadingArtGeometryCached = false;
            nowLoadingArtAlpha = 0.0f;
        }

        // A brief black screen before the art appears is expected; fade in once it is ready.
        if (nowLoadingArtAlpha < 1.0f) {
            nowLoadingArtAlpha = Math.Min(1.0f, nowLoadingArtAlpha + Math.Max(deltaSeconds, 0.0f) / FadeInDurationSeconds);
        }

        ApplyNowLoadingArtGeometry();
        nowLoadingArtNode.Alpha = nowLoadingArtAlpha;
        nowLoadingArtNode.IsVisible = true;
    }

    private unsafe void EnsureNowLoadingArtNode() {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(NowLoadingAddonName);
        if (addon is null) return;

        nowLoadingArtGeometryCached = false;

        nowLoadingArtNode = new SimpleImageNode {
            IsVisible = false,
            WrapMode = WrapMode.Stretch,
            FitTexture = true,
        };

        // First child so the spinner draws on top of the art rather than behind it.
        nowLoadingArtNode.AttachNode(addon, NodePosition.AsFirstChild);
    }

    private unsafe void OnNowLoadingFinalize(AtkUnitBase* addon) {
        nowLoadingArtNode?.Dispose();
        nowLoadingArtNode = null;
    }

    private unsafe void ApplyNowLoadingArtGeometry() {
        if (nowLoadingArtGeometryCached || nowLoadingArtNode is null) return;

        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(NowLoadingAddonName);
        if (addon is null || addon->RootNode is null) return;

        var root = addon->RootNode;
        var scaleX = root->ScaleX <= 0.0f ? 1.0f : root->ScaleX;
        var scaleY = root->ScaleY <= 0.0f ? 1.0f : root->ScaleY;

        ref var screen = ref AtkStage.Instance()->ScreenSize;

        var targetWidth = screen.Width / scaleX;
        var targetHeight = screen.Height / scaleY;

        var drawWidth = targetWidth;
        var drawHeight = drawWidth / LoadingArtAspectRatio;
        var drawPosX = -root->ScreenX / scaleX;
        var drawPosY = -root->ScreenY / scaleY;

        if (drawHeight > targetHeight) {
            drawHeight = targetHeight;
            drawWidth = drawHeight * LoadingArtAspectRatio;
            drawPosX -= (drawWidth - targetWidth) * 0.5f;
        }
        else {
            drawPosY -= (drawHeight - targetHeight) * 0.5f;
        }

        nowLoadingArtNode.Scale = Vector2.One;
        nowLoadingArtNode.Size = new Vector2(drawWidth, drawHeight);
        nowLoadingArtNode.Position = new Vector2(drawPosX, drawPosY);
        nowLoadingArtGeometryCached = true;
    }

    private unsafe (uint TerritoryId, bool FromHook) ResolveDestinationTerritory() {
        var next = GameMain.Instance()->NextTerritoryTypeId;
        if (next is not 0 && next != sourceTerritoryId) {
            var matchedHint = hookedTerritoryId == next;
            hookedTerritoryId = 0;
            return (next, matchedHint);
        }

        if (hookedTerritoryId is not 0 && hookedTerritoryId != sourceTerritoryId) {
            return (hookedTerritoryId, true);
        }

        return (0, false);
    }

    private unsafe bool OnTeleport(Telepo* thisPtr, uint aetheryteId, byte subIndex) {
        var accepted = teleportHook!.Original(thisPtr, aetheryteId, subIndex);

        try {
            if (accepted && Services.DataManager.GetExcelSheet<Aetheryte>().TryGetRow(aetheryteId, out var aetheryte)) {
                var territoryId = aetheryte.Territory.RowId;
                hookedTerritoryId = territoryId != Services.ClientState.TerritoryType ? territoryId : 0;
            }
        }
        catch (Exception exception) {
            Services.PluginLog.Exception(exception);
        }

        return accepted;
    }

    private unsafe void ApplyArtGeometry(AtkResNode* node) {
        if (!artGeometryCached) {
            ref var screen = ref AtkStage.Instance()->ScreenSize;

            var parentScaleX = 1.0f;
            var parentScaleY = 1.0f;
            var parentScreenX = 0.0f;
            var parentScreenY = 0.0f;

            var parent = node->ParentNode;
            if (parent is not null) {
                parentScreenX = parent->ScreenX;
                parentScreenY = parent->ScreenY;

                for (var ancestor = parent; ancestor is not null; ancestor = ancestor->ParentNode) {
                    parentScaleX *= ancestor->ScaleX;
                    parentScaleY *= ancestor->ScaleY;
                }
            }

            if (parentScaleX <= 0.0f) parentScaleX = 1.0f;
            if (parentScaleY <= 0.0f) parentScaleY = 1.0f;

            var targetWidth = screen.Width / parentScaleX;
            var targetHeight = screen.Height / parentScaleY;

            var drawWidth = targetWidth;
            var drawHeight = drawWidth / LoadingArtAspectRatio;
            var drawPosX = -parentScreenX / parentScaleX;
            var drawPosY = -parentScreenY / parentScaleY;

            if (drawHeight > targetHeight) {
                drawHeight = targetHeight;
                drawWidth = drawHeight * LoadingArtAspectRatio;
                drawPosX -= (drawWidth - targetWidth) * 0.5f;
            }
            else {
                drawPosY -= (drawHeight - targetHeight) * 0.5f;
            }

            cachedArtWidth = drawWidth;
            cachedArtHeight = drawHeight;
            cachedArtPosX = drawPosX;
            cachedArtPosY = drawPosY;
            artGeometryCached = true;
        }

        node->SetScale(1.0f, 1.0f);
        node->SetWidth((ushort)cachedArtWidth);
        node->SetHeight((ushort)cachedArtHeight);
        node->SetPositionFloat(cachedArtPosX, cachedArtPosY);
    }

    private void ResetLoadState() {
        appliedTerritoryId = 0;
        appliedTexturePath = null;
        appliedNowLoadingTerritoryId = 0;
        appliedNowLoadingTexturePath = null;
        artGeometryCached = false;
        nowLoadingArtGeometryCached = false;
        currentAlpha = 0.0f;
        nowLoadingArtAlpha = 0.0f;
    }

    private unsafe void ForceHideArtNode() {
        try {
            var bannerAddon = RaptureAtkUnitManager.Instance()->GetAddonByName(LoadingBannerAddonName);
            if (bannerAddon is null) return;

            var artNode = bannerAddon->GetNodeById<AtkImageNode>(ArtNodeId);
            if (artNode is null) return;

            var resNode = (AtkResNode*)artNode;
            resNode->ToggleVisibility(false);
            resNode->SetAlpha(255);
        }
        catch (Exception exception) {
            Services.PluginLog.Exception(exception);
        }
    }

    private static string? BuildLoadingImagePath(uint territoryId, bool skipInstancedContent = true) {
        if (!Services.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territory)) return null;

        if (skipInstancedContent) {
            // Instanced content (dungeons, trials, raids, ...) already shows its own dedicated loading art.
            var isInstancedContent = Services.DataManager.GetExcelSheet<ContentFinderCondition>()
                .Any(condition => condition.ContentLinkType == 1 && condition.TerritoryType.RowId == territoryId);
            if (isInstancedContent) return null;
        }

        if (!Services.DataManager.GetExcelSheet<LoadingImage>().TryGetRow(territory.LoadingImage.RowId, out var loadingImage)) return null;

        var imageName = loadingImage.Name.ExtractText();
        if (string.IsNullOrEmpty(imageName)) return null;

        return $"ui/loadingimage/{imageName}_hr1.tex";
    }
}
