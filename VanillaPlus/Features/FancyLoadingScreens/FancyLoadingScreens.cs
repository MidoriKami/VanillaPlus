using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FancyLoadingScreens;

public unsafe class FancyLoadingScreens : GameModification {
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

    public override Task OnEnableAsync() {
        teleportHook = Services.Hooker.HookFromAddress<Telepo.Delegates.Teleport>(Telepo.MemberFunctionPointers.Teleport, OnTeleport);
        teleportHook?.Enable();

        Services.Framework.Update += OnFrameworkUpdate;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.Framework.Update -= OnFrameworkUpdate;

        teleportHook?.Dispose();
        teleportHook = null;

        var restore = Services.Framework.Run(ForceHideArtNode);

        wasLoading = false;
        sourceTerritoryId = 0;
        hookedTerritoryId = 0;

        return restore;
    }

    private void OnFrameworkUpdate(IFramework framework) {
        try {
            var isLoading = Services.Condition.IsBetweenAreas;
            var deltaSeconds = (float)framework.UpdateDelta.TotalSeconds;

            if (isLoading && !wasLoading) {
                ForceHideArtNode();

                sourceTerritoryId = Services.ClientState.TerritoryType;
                appliedTerritoryId = 0;
                appliedTexturePath = null;
                artGeometryCached = false;
                currentAlpha = 0.0f;
            }

            if (isLoading) {
                TryApplyDestinationArt(deltaSeconds);
            }

            if (!isLoading && wasLoading) {
                appliedTerritoryId = 0;
                appliedTexturePath = null;
                artGeometryCached = false;
                hookedTerritoryId = 0;
                currentAlpha = 0.0f;
            }

            wasLoading = isLoading;
        }
        catch (Exception exception) {
            Services.Framework.Update -= OnFrameworkUpdate;
            ForceHideArtNode();
            Services.PluginLog.Exception(exception);
        }
    }

    private void TryApplyDestinationArt(float deltaSeconds) {
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

    // Prefer the teleport hint until GameMain publishes the authoritative destination.
    private (uint TerritoryId, bool FromHook) ResolveDestinationTerritory() {
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

    private bool OnTeleport(Telepo* thisPtr, uint aetheryteId, byte subIndex) {
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

    // Compute local full-screen geometry once, then reapply it while the game resets the node layout.
    private void ApplyArtGeometry(AtkResNode* node) {
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

            cachedArtWidth = screen.Width / parentScaleX;
            cachedArtHeight = screen.Height / parentScaleY;
            cachedArtPosX = -parentScreenX / parentScaleX;
            cachedArtPosY = -parentScreenY / parentScaleY;
            artGeometryCached = true;
        }

        node->SetScale(1.0f, 1.0f);
        node->SetWidth((ushort)cachedArtWidth);
        node->SetHeight((ushort)cachedArtHeight);
        node->SetPositionFloat(cachedArtPosX, cachedArtPosY);
    }

    private void ForceHideArtNode() {
        try {
            currentAlpha = 0.0f;
            appliedTerritoryId = 0;
            appliedTexturePath = null;
            artGeometryCached = false;

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

    private static string? BuildLoadingImagePath(uint territoryId) {
        if (!Services.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territory)) return null;

        // Instanced content (dungeons, trials, raids, ...) already shows its own dedicated loading art.
        var isInstancedContent = Services.DataManager.GetExcelSheet<ContentFinderCondition>()
            .Any(condition => condition.ContentLinkType == 1 && condition.TerritoryType.RowId == territoryId);
        if (isInstancedContent) return null;

        if (!Services.DataManager.GetExcelSheet<LoadingImage>().TryGetRow(territory.LoadingImage.RowId, out var loadingImage)) return null;

        var imageName = loadingImage.Name.ExtractText();
        if (string.IsNullOrEmpty(imageName)) return null;

        return $"ui/loadingimage/{imageName}_hr1.tex";
    }
}
