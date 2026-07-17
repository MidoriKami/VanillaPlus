using System;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.CastBarAetheryteNames;

public class CastBarAetheryteNames : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_CastBarAetheryteNames,
        Description = Strings.ModificationDescription_CastBarAetheryteNames,
        Authors = ["Haselnussbomber"],
        Type = ModificationType.GameBehavior,
        CompatibilityModule = new HaselTweaksCompatibilityModule("CastBarAetheryteNames"),
    };

    private Hook<Telepo.Delegates.Teleport>? teleportHook;
    private TeleportInfo? teleportInfo;

    public override string ImageName => "CastBarAetheryteNames.png";

    public override Task OnEnableAsync() {
        unsafe {
            teleportHook = IGameInteropProvider.Get().HookFromAddress<Telepo.Delegates.Teleport>(Telepo.MemberFunctionPointers.Teleport, OnTeleport);
            teleportHook?.Enable();
        }

        IClientState.Get().TerritoryChanged += OnTerritoryChanged;
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostRefresh, "_CastBar", OnCastBarRefresh);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnCastBarRefresh);
        IClientState.Get().TerritoryChanged -= OnTerritoryChanged;

        teleportHook?.Dispose();
        teleportHook = null;

        teleportInfo = null;

        return Task.CompletedTask;
    }

    private void OnTerritoryChanged(uint u)
        => teleportInfo = null;

    private unsafe void OnCastBarRefresh(AddonEvent type, AddonArgs args) {
        if (teleportInfo is not { } info) return;
        if (IObjectTable.Get().LocalPlayer is not { IsCasting: true, CastActionId: 5 }) return;

        var textNode = args.GetAddon<AddonCastBar>()->GetTextNodeById(4);
        if (textNode == null) return;

        var aetheryte = IDataManager.Get().GetExcelSheet<Aetheryte>().GetRow(info.AetheryteId);

        switch (info) {
            case { IsApartment: true }:
                textNode->SetText(IDataManager.Get().GetAddonText(8518));
                break;

            case { IsSharedHouse: true }:
                textNode->SetText(ISeStringEvaluator.Get().EvaluateFromAddon(8519, [(uint)info.Ward, (uint)info.Plot]));
                break;

            case { } when aetheryte.PlaceName.IsValid:
                textNode->SetText(aetheryte.PlaceName.Value.Name.ToString());
                break;
        }
    }

    private unsafe bool OnTeleport(Telepo* thisPtr, uint aetheryteId, byte subIndex) {
        try {
            teleportInfo = null;

            if (thisPtr->TeleportList.Count is 0) {
                thisPtr->UpdateAetheryteList();
            }

            foreach (var teleportEntry in thisPtr->TeleportList) {
                if (teleportEntry.AetheryteId == aetheryteId && teleportEntry.SubIndex == subIndex) {
                    teleportInfo = teleportEntry;
                    break;
                }
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }

        return teleportHook!.Original(thisPtr, aetheryteId, subIndex);
    }
}
