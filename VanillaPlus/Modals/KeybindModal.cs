using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
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

public unsafe class KeybindModal : Window, IDisposable {
    public required Action<HashSet<VirtualKey>> KeybindSetCallback { get; init; }

    private readonly HashSet<VirtualKey> combo = [VirtualKey.NO_KEY];
    private readonly IFontHandle largeAxisFontHandle;

    private readonly List<string> conflicts = [];
    
    public KeybindModal() : base("Set Keybind Modal") {
        WindowName += $"##{new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name}";
        
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
        
        System.KeyListener.OnKeyPressed += KeyPressed;
    }

    private void KeyPressed(VirtualKey arg1, bool isPressed) {
        if (!IsOpen) return;
        if (!isPressed) return;

        combo.Clear();
        foreach (var key in Services.KeyState.GetValidVirtualKeys()) {
            if (Services.KeyState[(int)key]) {
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
            if (keybind.Key is SeVirtualKey.NO_KEY && keybind.AltKey is SeVirtualKey.NO_KEY) continue;

            var keyMatches = IsComboMatch(combo, keybind, false);
            var altKeyMatches = IsComboMatch(combo, keybind, true);

            if (keyMatches || altKeyMatches) {
                conflicts.Add(nameString);
            }
        }
        
        Services.KeyState.ResetKeyCombo(combo);
    }

    private static bool IsComboMatch(HashSet<VirtualKey> keyCombo, UIInputData.Keybind keybind, bool useAltCombo) {
        var key = useAltCombo ? keybind.AltKey : keybind.Key;
        var flags = useAltCombo ? keybind.AltModifier : keybind.Modifier;

        var comboModifies = keyCombo.Where(comboKey => comboKey is (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT)).ToList();
        var comboKey = keyCombo.FirstOrDefault(comboKey => comboKey is not (VirtualKey.CONTROL  or VirtualKey.MENU or VirtualKey.SHIFT));

        // If our base key doesn't match, we don't match.
        if ((int)comboKey != (int)key) return false;

        // Game combo does not have a modifier, but we do
        if (flags is 0 && comboModifies.Count != 0) return false;
        
        // Game combo wants Control, but we don't have Control in our combo
        if (flags.HasFlag(ModifierFlag.Ctrl) && !comboModifies.Contains(VirtualKey.CONTROL)) return false;
        
        // Game combo wants Alt, but we don't have Alt in our combo
        if (flags.HasFlag(ModifierFlag.Alt) && !comboModifies.Contains(VirtualKey.MENU)) return false;
        
        // Game combo wants Shift, but we don't have Shift in our combo
        if (flags.HasFlag(ModifierFlag.Shift) && !comboModifies.Contains(VirtualKey.SHIFT)) return false;

        return true;
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

    public void Dispose() {
        System.KeyListener.OnKeyPressed -= KeyPressed;
        largeAxisFontHandle.Dispose();
        this.RemoveFromWindowSystem();
    }
}
