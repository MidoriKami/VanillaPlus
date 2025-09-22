using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class GearsetRedirectConfigWindow : Window {
    private readonly GearsetRedirectConfig config;
    private RaptureGearsetModule.GearsetEntry* selectedTargetEntry;
    private TerritoryType? selectedTerritory;

    private readonly List<TerritoryType> zoneList;
    
    public GearsetRedirectConfigWindow(GearsetRedirectConfig config) : base("Gearset Redirect Config Window") {
        this.config = config;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(400.0f, 400.0f),
        };

        zoneList = Services.DataManager.GetExcelSheet<TerritoryType>()
            .Where(territory => territory.PlaceName is { IsValid:true, RowId: not 0 })
            .OrderBy(territory => territory.PlaceName.Value.Name.ToString())
            .ToList();
        
        IsOpen = true;
    }

    public override void Draw() {
        if (!Services.ClientState.IsLoggedIn) {
            ImGui.TextColored(KnownColor.OrangeRed.Vector(), "Not logged in");
            return;
        }

        using var table = ImRaii.Table("redirection_table", 2);
        if (!table) return;

        foreach (var gearset in RaptureGearsetModule.Instance()->Entries.PointerEnumerator()) {
            // Valid Gearsets cannot have an empty name
            if (gearset->NameString == string.Empty) continue;
            using var id = ImRaii.PushId(gearset->Id.ToString());
            
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            
            DrawGearsetLabel(gearset);

            ImGui.TableNextColumn();
            RedirectInfo? removalSet = null;
            
            if (config.Redirections.TryGetValue(gearset->Id, out var redirection)) {
                foreach (var redirectionEntry in redirection) {
                    var territoryInfo = Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(redirectionEntry.TerritoryType);
                    
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) {
                        removalSet = redirectionEntry;
                    }
                    
                    ImGui.SameLine();
                    
                    ImGui.Text($"redirect to {redirectionEntry.AlternateGearsetId} when in {territoryInfo.PlaceName.Value.Name.ToString()}");
                    
                    ImGui.Spacing();
                }
                
                if (removalSet is not null) {
                    redirection.Remove(removalSet);
                }
            }

            using (var _ = Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), ImGuiHelpers.ScaledVector2(150.0f, 24.0f))) {
                    ImGui.OpenPopup("RedirectionModal");
                }
            }
            
            ImGui.SetNextWindowSizeConstraints(new Vector2(300.0f, 225.0f), Vector2.PositiveInfinity);
            if (ImGui.BeginPopupModal("RedirectionModal")) {
                ImGui.Text("Redirect this gearset:");

                using (ImRaii.PushIndent()) {
                    DrawGearsetLabel(gearset);
                }

                ImGui.Spacing();
                ImGui.Text("To this gearset:");

                using (ImRaii.PushIndent()) {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    using var dropdown = ImRaii.Combo("##GearsetCombo", GetGearsetNameString(selectedTargetEntry), ImGuiComboFlags.HeightLarge);
                    if (dropdown) {
                        foreach (var gearsetEntry in RaptureGearsetModule.Instance()->Entries.PointerEnumerator()) {
                            if (gearsetEntry->NameString == string.Empty) continue;
                            if (gearsetEntry->Id == gearset->Id) continue;
                            using var popupId = ImRaii.PushId(gearsetEntry->Id.ToString());

                            var cursorPosition = ImGui.GetCursorPos();
                            if (ImGui.Selectable($"##GearsetSelectable", gearsetEntry == selectedTargetEntry, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 24.0f))) { 
                                selectedTargetEntry = gearsetEntry;
                            }

                            ImGui.SetCursorPos(cursorPosition);
                            DrawGearsetLabel(gearsetEntry);
                        }
                    }
                }
                
                ImGui.Spacing();
                ImGui.Text("When in this Zone:");
                
                using (ImRaii.PushIndent()) {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    using var dropdown = ImRaii.Combo("##TerritorySelect", GetTerritoryNameString(selectedTerritory), ImGuiComboFlags.HeightLargest);
                    if (dropdown) {
                        foreach (var territory in zoneList) {
                            if (ImGui.Selectable(GetTerritoryNameString(territory), selectedTerritory is not null && selectedTerritory.Value.RowId == territory.RowId)) {
                                selectedTerritory = territory;
                            }
                        }
                    }
                }
                
                ImGui.Spacing();

                var currentZone = Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(Services.ClientState.TerritoryType);
                ImGui.Text($"You are currently in: {GetTerritoryNameString(currentZone)}");
                

                ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 28.0f * ImGuiHelpers.GlobalScale);
                
                if (ImGui.Button("Add", ImGuiHelpers.ScaledVector2(100.0f, 24.0f))) {
                    if (selectedTerritory is not null && selectedTargetEntry is not null) {
                        config.Redirections.TryAdd(gearset->Id, []);

                        config.Redirections[gearset->Id].Add(new RedirectInfo {
                            TerritoryType = selectedTerritory.Value.RowId,
                            AlternateGearsetId = selectedTargetEntry->Id,
                        });
                        
                        config.Save();
                    }

                    selectedTerritory = null;
                    selectedTargetEntry = null;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 100.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(100.0f, 24.0f))) {
                    selectedTerritory = null;
                    selectedTargetEntry = null;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
            
            ImGui.Spacing();
        }
    }

    private static void DrawGearsetLabel(RaptureGearsetModule.GearsetEntry* gearset) {
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"{gearset->Id + 1}");
        ImGui.SameLine();
            
        ImGui.Image(Services.TextureProvider.GetFromGameIcon(62000 + gearset->ClassJob).GetWrapOrEmpty().Handle, new Vector2(24.0f, 24.0f));
        ImGui.SameLine();

        ImGui.AlignTextToFramePadding();
        ImGui.Text(gearset->NameString);
            
        ImGui.SameLine(200.0f * ImGuiHelpers.GlobalScale);
        ImGui.Text($"{SeIconChar.ItemLevel.ToIconString()}{gearset->ItemLevel}");
    }

    private static string GetGearsetNameString(RaptureGearsetModule.GearsetEntry* gearset)
        => gearset is null ? "Nothing Selected" : $"{gearset->Id + 1}. {gearset->NameString} {SeIconChar.ItemLevel.ToIconString()}{gearset->ItemLevel}";

    private static string GetTerritoryNameString(TerritoryType? territory) {
        if (territory is null) return "Nothing Selected";

        return $"{territory.Value.PlaceName.Value.Name.ToString()} ({territory.Value.RowId})";
    }

    public override void OnClose()
        => config.Save();
}
