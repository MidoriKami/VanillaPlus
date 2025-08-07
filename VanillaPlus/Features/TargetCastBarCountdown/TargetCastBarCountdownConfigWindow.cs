using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public class TargetCastBarCountdownConfigWindow : Window {
    private readonly TargetCastBarCountdownConfig config;
    private readonly Action drawNodeConfigs;
    private readonly Action saveNodeStyle;

    public TargetCastBarCountdownConfigWindow(TargetCastBarCountdownConfig config, Action drawNodeConfigs, Action saveNodeStyle) : base("Target Cast Bar Countdown") {
        this.config = config;
        this.drawNodeConfigs = drawNodeConfigs;
        this.saveNodeStyle = saveNodeStyle;
        
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(650.0f, 800.0f),
        };
    }

    public override void Draw() {
        if (ImGui.Checkbox("Show on Primary Target Cast Bar", ref config.PrimaryTarget)) config.Save();
        if (ImGui.Checkbox("Show on Focus Target Cast Bar", ref config.FocusTarget)) config.Save();
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        drawNodeConfigs();
    }

    public override void OnClose() {
        config.Save();
        saveNodeStyle();
    }
}
