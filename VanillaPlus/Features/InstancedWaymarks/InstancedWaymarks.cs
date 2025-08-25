using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.InstancedWaymarks;

public unsafe class InstancedWaymarks : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Instanced Waymarks",
        Description = "Enables the use of all saved Waymark Slots per duty, instead of sharing them across all duties, with the option to name each slot.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("WaymarkPresetPlugin", "MemoryMarker"),
    };

    private uint previousCfc;
    private int slotClicked = -1;
    private InstancedWaymarksConfig? config;
    private WaymarkRenameWindow? renameWindow;
    
    public override void OnEnable() {
        config = InstancedWaymarksConfig.Load();
        
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        Services.ContextMenu.OnMenuOpened += OnMenuOpened;
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "FieldMarker", OnFieldMarkerDraw);
        
        SaveWaymarks(0);
        
        var currentCfc = GameMain.Instance()->CurrentContentFinderConditionId;
        if (currentCfc is not 0) {
            LoadWaymarks(currentCfc);
            previousCfc = currentCfc;
        }
    }

    public override void OnDisable() {    
        Services.AddonLifecycle.UnregisterListener(OnFieldMarkerDraw);
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Services.ContextMenu.OnMenuOpened -= OnMenuOpened;
        
        LoadWaymarks(0);

        renameWindow?.Close();
        renameWindow = null;
        
        config = null;
    }

    private void OnTerritoryChanged(ushort obj) {
        var currentCfc = GameMain.Instance()->CurrentContentFinderConditionId;

        SaveWaymarks(previousCfc);
        LoadWaymarks(currentCfc);

        previousCfc = currentCfc;
    }
    
    private void OnMenuOpened(IMenuOpenedArgs args) {
        if (args.AddonName is not "FieldMarker") return;

        slotClicked = AgentFieldMarker.Instance()->PageIndexOffset;
        ref var slotMarkerData = ref FieldMarkerModule.Instance()->Presets[slotClicked];

        args.AddMenuItem(new MenuItem {
            Name = "Rename",
            OnClicked = RenameContextMenuAction,
            UseDefaultPrefix = true,
            IsEnabled =
                GameMain.Instance()->CurrentContentFinderConditionId == slotMarkerData.ContentFinderConditionId &&
                GameMain.Instance()->CurrentContentFinderConditionId is not 0 &&
                slotMarkerData.ContentFinderConditionId is not 0,
        });
    }

    private void OnFieldMarkerDraw(AddonEvent type, AddonArgs args) {
        if (config is null) return;

        var selectedPage = args.GetAddon<AddonFieldMarker>()->SelectedPage;
        var cfc = GameMain.Instance()->CurrentContentFinderConditionId;

        foreach (var index in Enumerable.Range(0, 5)) {
            var presetIndex = selectedPage * 5 + index;
            var preset = FieldMarkerModule.Instance()->Presets[presetIndex];
            if (preset.ContentFinderConditionId is 0) continue;

            if (config.NamedWaymarks.TryGetValue(cfc, out var savedPresets)) {
                if (savedPresets.TryGetValue(presetIndex, out var label)) {
                    var button = args.GetAddon<AtkUnitBase>()->GetComponentButtonById((uint)(21 + index * 2));
                    if (button is not null) {
                        button->ButtonTextNode->SetText($"{presetIndex + 1}. {label}");
                    }
                }
            }
        }
    }
    
    private void RenameContextMenuAction(IMenuItemClickedArgs menuItemClickedArgs) {
        if (slotClicked is -1) return;
        if (config is null) return;

        var cfc = GameMain.Instance()->CurrentContentFinderConditionId;

        renameWindow ??= new WaymarkRenameWindow(config, cfc, slotClicked, () => {
            renameWindow?.Close();
            renameWindow?.RemoveFromWindowSystem();
            renameWindow = null;
        });

        renameWindow.AddToWindowSystem();
        renameWindow.Open();
    }
    
    private static void SaveWaymarks(uint contentFinderCondition) {
        Services.PluginLog.Debug($"Saving Waymarks for Duty: {contentFinderCondition}");

        var address = Unsafe.AsPointer(ref FieldMarkerModule.Instance()->Presets[0]);
        var size = sizeof(FieldMarkerPreset) * FieldMarkerModule.Instance()->Presets.Length;

        var dataFilePath = GetDataFileInfo(contentFinderCondition).FullName;
        var dataSpan = new Span<byte>(address, size);

        FilesystemUtil.WriteAllBytesSafe(dataFilePath, dataSpan.ToArray());
    }

    private static void LoadWaymarks(uint contentFinderCondition) {
        Services.PluginLog.Debug($"Loading Waymarks for Duty: {contentFinderCondition}");

        var address = Unsafe.AsPointer(ref FieldMarkerModule.Instance()->Presets[0]);
        var size = sizeof(FieldMarkerPreset) * FieldMarkerModule.Instance()->Presets.Length;

        var dataFilePath = GetDataFileInfo(contentFinderCondition).FullName;
        var result = File.ReadAllBytes(dataFilePath);

        if (result.Length < size) {
            Services.PluginLog.Debug("Not data to load, creating new file.");
            result = new byte[size];
            FilesystemUtil.WriteAllBytesSafe(dataFilePath, result);
        }

        Marshal.Copy(result, 0, (nint)address, size);
    }

    private static FileInfo GetDataFileInfo(uint contentFinderCondition) {
        var directoryInfo = new DirectoryInfo(Path.Combine(Config.DataPath, "InstancedWaymarks"));
        if (!directoryInfo.Exists) {
            directoryInfo.Create();
        }

        var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, $"{contentFinderCondition}.waymark.dat"));
        if (!fileInfo.Exists) {
            fileInfo.Create().Close();
        }

        return fileInfo;
    }
}
