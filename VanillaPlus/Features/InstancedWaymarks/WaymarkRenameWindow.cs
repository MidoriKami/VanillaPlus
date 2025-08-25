using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace VanillaPlus.Features.InstancedWaymarks;

public class WaymarkRenameWindow : Window {
    private readonly InstancedWaymarksConfig config;
    private readonly uint cfc;
    private readonly int index;
    private readonly Action callback;

    public WaymarkRenameWindow(InstancedWaymarksConfig config, uint cfc, int index, Action callback) : base("Waymark Rename Window", ImGuiWindowFlags.AlwaysAutoResize) {
        this.config = config;
        this.cfc = cfc;
        this.index = index;
        this.callback = callback;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(250.0f, 100.0f),
        };
    }

    public override void Draw() {
        ImGuiHelpers.ScaledDummy(5.0f);

        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();

        var dutyName = Services.DataManager.GetExcelSheet<ContentFinderCondition>().GetRow(cfc).Name;
        
        config.NamedWaymarks.TryAdd(cfc, []);
        config.NamedWaymarks[cfc].TryAdd(index, dutyName.ToString());
        var inputLabel = config.NamedWaymarks[cfc][index];
        
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputText("###RenameTextInput", ref inputLabel, 35, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue)) {
            config.NamedWaymarks[cfc][index] = inputLabel;
            config.Save();
            callback();
        }

        config.NamedWaymarks[cfc][index] = inputLabel;

        var buttonSize = ImGuiHelpers.ScaledVector2(100.0f, 23.0f);
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - buttonSize.X);
        if (ImGui.Button("Save & Close", buttonSize)) {
            config.Save();
            callback();
        }
    }
}
