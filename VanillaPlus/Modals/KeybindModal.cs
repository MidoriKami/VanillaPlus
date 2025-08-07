using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Extensions;

namespace VanillaPlus.Modals;

public unsafe class KeybindModal : Window {
    public required Action<HashSet<SeVirtualKey>> KeybindSetCallback { get; set; }

    private readonly HashSet<SeVirtualKey> combo = [SeVirtualKey.NO_KEY];
    private readonly IFontHandle largeAxisFontHandle;

    private readonly List<string> conflicts = [];
    
    public KeybindModal() : base("Set Keybind Modal") {
        this.AddToWindowSystem();

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500.0f, 333.0f),
        };
        
        largeAxisFontHandle = Services.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle {
            SizePt = 24.0f,
            FamilyAndSize = GameFontFamilyAndSize.Axis36,
            Italic = true,
            BaseSkewStrength = 16f,
        });
        
        System.KeyListener.OnSeKeyPressed += OnSeKeyPressed;
        
        Toggle();
    }

    private void OnSeKeyPressed(SeVirtualKey changedKey, bool isPressed) {
        if (!isPressed) return;

        combo.Clear();
        foreach (var key in Enum.GetValues<SeVirtualKey>()) {
            if (UIInputData.Instance()->IsKeyDown(key)) {
                combo.Add(key);
            }
        }

        conflicts.Clear();
        foreach (var keybindName in Enum.GetValues<InputId>()) {
            UIInputData.Keybind keybind;
            var nameString = keybindName.ToString();
            
            if (nameString.StartsWith("NUM_")) {
                nameString = nameString.Replace("NUM_", string.Empty);
            }
            
            using var utfString = new Utf8String(nameString);
            UIInputData.Instance()->GetKeybind(&utfString, &keybind);

            if (combo.Contains(keybind.Key)) {
                conflicts.Add(nameString);
            }
        }
    }

    public override void Draw() {
        DrawCurrentCombo();
        DrawConflicts();
        DrawCompletion();
    }

    private void DrawCurrentCombo() {
        using var child = ImRaii.Child("current_combo", new Vector2(ImGui.GetContentRegionMax().X, 125.0f * ImGuiHelpers.GlobalScale));
        if (!child) return;
        
        ImGui.Spacing();
        ImGui.Spacing();
        
        ImGui.Text("Input Desired Key Combo");
        ImGui.Separator();

        ImGui.SetCursorPosY(50.0f);
        
        using var font = largeAxisFontHandle.Push();
        ImGuiHelpers.CenteredText(string.Join(" + ", combo));
    }

    private void DrawConflicts() {
        using var child = ImRaii.Child("conflicts", new Vector2(ImGui.GetContentRegionMax().X, 125.0f * ImGuiHelpers.GlobalScale));
        if (!child) return;
        
        if (conflicts.Count is not 0) {
            ImGui.Text("Conflict(s) Detected:");
            ImGui.Separator();

            using var conflictList = ImRaii.Child("conflict_list", new Vector2(ImGui.GetContentRegionMax().X, 85.0f * ImGuiHelpers.GlobalScale));
            if (!conflictList) return;
            
            ImGui.TextWrapped(string.Join("\n", conflicts));
        }
    }

    private void DrawCompletion() {
        using var completion = ImRaii.Child("completion", ImGui.GetContentRegionAvail());
        if (!completion) return;
        
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 100.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button("Cancel", ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
            this.Close();
        }
        
        ImGui.SameLine(ImGui.GetContentRegionMax().X - 100.0f * ImGuiHelpers.GlobalScale * 2.0f - 5.0f);
        if (ImGui.Button("Accept", ImGuiHelpers.ScaledVector2(100.0f, 25.0f))) {
            KeybindSetCallback.Invoke(combo);
            this.Close();
        }
    }

    public override void OnClose() {
        System.KeyListener.OnSeKeyPressed -= OnSeKeyPressed;
        this.RemoveFromWindowSystem();
    }

    // private bool ComboContainsModifiers(UIInputData.Keybind keybind) {
    //     if (keybind.Modifier is 0) return false;
    //     
    //     var isAlt = keybind.Modifier is ModifierFlag.Alt;
    //     var isCtrl = keybind.Modifier is ModifierFlag.Ctrl;
    //     var isShift = keybind.Modifier is ModifierFlag.Shift;
    //
    //     if (isAlt && !combo.Contains(SeVirtualKey.MENU)) return false;
    //     if (isCtrl && !combo.Contains(SeVirtualKey.SHIFT)) return false;
    //     if (isShift && !combo.Contains(SeVirtualKey.CONTROL)) return false;
    //
    //     return true;
    // }
}
