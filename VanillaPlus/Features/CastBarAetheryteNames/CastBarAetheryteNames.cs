using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.CastBarAetheryteNames;

public unsafe class CastBarAetheryteNames : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_CastBarAetheryteNames,
        Description = Strings.ModificationDescription_CastBarAetheryteNames,
        Authors = ["Haselnussbomber"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new HaselTweaksCompatibilityModule("CastBarAetheryteNames"),
    };

    private Hook<Telepo.Delegates.Teleport>? teleportHook;
    private TeleportInfo? teleportInfo;

    public override string ImageName => "CastBarAetheryteNames.png";

    public override void OnEnable() {
        teleportHook = Services.GameInteropProvider.HookFromAddress<Telepo.Delegates.Teleport>(Telepo.MemberFunctionPointers.Teleport, OnTeleport);
        teleportHook?.Enable();
        
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "_CastBar", OnCastBarRefresh);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnCastBarRefresh);
        
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        
        teleportHook?.Dispose();
        teleportHook = null;
        
        teleportInfo = null;
    }
    
    private void OnTerritoryChanged(ushort obj)
        => teleportInfo = null;

    private void OnCastBarRefresh(AddonEvent type, AddonArgs args) {
        if (teleportInfo is not { } info) return;
        if (Services.ObjectTable.LocalPlayer is not { IsCasting: true, CastActionId: 5 }) return;

        var textNode = args.GetAddon<AddonCastBar>()->GetTextNodeById(4);
        if (textNode == null) return;
        
        var aetheryte = Services.DataManager.GetExcelSheet<Aetheryte>().GetRow(info.AetheryteId);

        switch (info) {
            case { IsApartment: true }:
                textNode->SetText(Services.DataManager.GetAddonText(8518));
                break;
            
            case { IsSharedHouse: true }:
                textNode->SetText(Services.SeStringEvaluator.EvaluateFromAddon(8519, [(uint)info.Ward, (uint)info.Plot]));
                break;
            
            case { } when aetheryte.PlaceName.IsValid:
                textNode->SetText(aetheryte.PlaceName.Value.Name.ToString());
                break;
        }
    }

    private bool OnTeleport(Telepo* thisPtr, uint aetheryteId, byte subIndex) {
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
            Services.PluginLog.Error(e, "Exception in OnTeleport");
        }
        
        return teleportHook!.Original(thisPtr, aetheryteId, subIndex);
    }
}
