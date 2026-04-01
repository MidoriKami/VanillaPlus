using System;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class CheckBoxConfig : BaseConfigEntry {
    public required bool InitialState { get; set; }
    public Action<bool>? ToggleAction { get; init; } 

    public override NodeBase BuildNode() {
        return new CheckboxNode {
            OnClick = OnOptionChanged,
            Height = 24.0f,
            String = Label,
            IsChecked = InitialState,
        };
    }

    private void OnOptionChanged(bool newValue) {
        InitialState = newValue;
        ToggleAction?.Invoke(newValue);
        MemberInfo.SetValue(Config, newValue);
        Config.Save();
    }
}
