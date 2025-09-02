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
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Extensions;

namespace VanillaPlus.Classes;

public unsafe class KeybindModal : Window, IDisposable {
    public required Action<HashSet<VirtualKey>> KeybindSetCallback { get; init; }

    private readonly HashSet<VirtualKey> combo = [VirtualKey.NO_KEY];
    private readonly IFontHandle largeAxisFontHandle;

    private readonly List<InputId> conflicts = [];
    
    public KeybindModal(string windowName) : base($"{windowName} Set Keybind Modal") {
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
        var keybindSpan = UIInputData.Instance()->GetKeybindSpan();
        foreach (var index in Enumerable.Range(0, keybindSpan.Length)) {
            ref var keybind = ref keybindSpan[index];
            if (keybind.IsKeybindMatch(combo)) {
                conflicts.Add((InputId)index);
            }
        }

        Services.KeyState.ResetKeyCombo(combo);
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
