using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.InstancedWaymarks;

public class InstancedWaymarks : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InstancedWaymarks,
        Description = Strings.ModificationDescription_InstancedWaymarks,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("WaymarkPresetPlugin", "MemoryMarker"),
    };

    private uint previousCfc;
    private int slotClicked = -1;
    private InstancedWaymarksConfig? config;
    private RenameAddon? renameWindow;

    public override string ImageName => "InstanceWaymarks.png";

    public override async Task OnEnableAsync() {
        config = await InstancedWaymarksConfig.Load();

        renameWindow ??= new RenameAddon {
            Size = new Vector2(250.0f, 150.0f),
            InternalName = "WaymarkRename",
            Title = Strings.InstancedWaymarks_RenameWindowTitle,
            AutoSelectAll = true,
        };

        Service<IClientState>.Get().TerritoryChanged += OnTerritoryChanged;
        Service<IContextMenu>.Get().OnMenuOpened += OnMenuOpened;

        Service<IAddonLifecycle>.Get().RegisterListener(AddonEvent.PreDraw, "FieldMarker", OnFieldMarkerDraw);

        SaveWaymarks(0);

        unsafe {
            var currentCfc = GameMain.Instance()->CurrentContentFinderConditionId;
            if (currentCfc is not 0) {
                LoadWaymarks(currentCfc);
                previousCfc = currentCfc;
            }
        }
    }

    public override async Task OnDisableAsync() {
        Service<IAddonLifecycle>.Get().UnregisterListener(OnFieldMarkerDraw);
        Service<IClientState>.Get().TerritoryChanged -= OnTerritoryChanged;
        Service<IContextMenu>.Get().OnMenuOpened -= OnMenuOpened;

        LoadWaymarks(0);

        await Task.WhenAll(renameWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        renameWindow = null;

        config = null;
    }

    private unsafe void OnTerritoryChanged(uint u) {
        var currentCfc = GameMain.Instance()->CurrentContentFinderConditionId;

        SaveWaymarks(previousCfc);
        LoadWaymarks(currentCfc);

        previousCfc = currentCfc;
    }

    private unsafe void OnMenuOpened(IMenuOpenedArgs args) {
        if (args.AddonName is not "FieldMarker") return;

        slotClicked = AgentFieldMarker.Instance()->PageIndexOffset;
        ref var slotMarkerData = ref FieldMarkerModule.Instance()->Presets[slotClicked];

        args.AddMenuItem(new MenuItem {
            Name = Strings.InstancedWaymarks_RenameMenuLabel,
            OnClicked = RenameContextMenuAction,
            UseDefaultPrefix = true,
            IsEnabled =
                GameMain.Instance()->CurrentContentFinderConditionId == slotMarkerData.ContentFinderConditionId &&
                GameMain.Instance()->CurrentContentFinderConditionId is not 0 &&
                slotMarkerData.ContentFinderConditionId is not 0,
        });
    }

    private unsafe void OnFieldMarkerDraw(AddonEvent type, AddonArgs args) {
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

    private unsafe void RenameContextMenuAction(IMenuItemClickedArgs menuItemClickedArgs) {
        if (slotClicked is -1) return;
        if (config is null) return;
        if (renameWindow is null) return;

        var cfc = GameMain.Instance()->CurrentContentFinderConditionId;
        string defaultName;

        if (config.NamedWaymarks.TryGetValue(cfc, out var mapping) && mapping.TryGetValue(slotClicked, out var name)) {
            defaultName = name;
        }
        else {
            defaultName = Service<IDataManager>.Get().GetExcelSheet<ContentFinderCondition>().GetRow(cfc).Name.ToString();
        }

        renameWindow.OnRenameComplete = newString => {
            config.NamedWaymarks.TryAdd(cfc, []);
            config.NamedWaymarks[cfc].TryAdd(slotClicked, newString.ToString());
            config.NamedWaymarks[cfc][slotClicked] = newString.ToString();
            Task.Run(config.Save);
        };

        renameWindow.DefaultString = defaultName;
        renameWindow.Toggle();
    }

    private static unsafe void SaveWaymarks(uint contentFinderCondition) {
        Service<IPluginLog>.Get().Debug($"Saving Waymarks for Duty: {contentFinderCondition}", "InstancedWaymarks");

        var address = Unsafe.AsPointer(ref FieldMarkerModule.Instance()->Presets[0]);
        var size = sizeof(FieldMarkerPreset) * FieldMarkerModule.Instance()->Presets.Length;

        var dataFilePath = GetDataFileInfo(contentFinderCondition).FullName;
        var dataSpan = new Span<byte>(address, size);

        FilesystemUtil.WriteAllBytesSafe(dataFilePath, dataSpan.ToArray());
    }

    private static unsafe void LoadWaymarks(uint contentFinderCondition) {
        Service<IPluginLog>.Get().Debug($"Loading Waymarks for Duty: {contentFinderCondition}", "InstancedWaymarks");

        var address = Unsafe.AsPointer(ref FieldMarkerModule.Instance()->Presets[0]);
        var size = sizeof(FieldMarkerPreset) * FieldMarkerModule.Instance()->Presets.Length;

        var dataFilePath = GetDataFileInfo(contentFinderCondition).FullName;
        var result = File.ReadAllBytes(dataFilePath);

        if (result.Length < size) {
            Service<IPluginLog>.Get().Debug("No data to load, creating new file.", "InstancedWaymarks");
            result = new byte[size];
            FilesystemUtil.WriteAllBytesSafe(dataFilePath, result);
        }

        Marshal.Copy(result, 0, (nint)address, size);
    }

    private static FileInfo GetDataFileInfo(uint contentFinderCondition) {
        var directoryInfo = new DirectoryInfo(Path.Combine(Data.DataPath, "InstancedWaymarks"));
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
