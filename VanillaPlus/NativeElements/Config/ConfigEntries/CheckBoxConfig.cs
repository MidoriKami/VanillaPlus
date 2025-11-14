using KamiToolKit.Nodes;
using KamiToolKit.System;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class CheckBoxConfig : BaseConfigEntry {
    public required bool InitialState { get; init; }

    public override NodeBase BuildNode() {
        return new CheckboxNode {
            OnClick = OnOptionChanged,
            Height = 24.0f,
            String = Label,
            IsChecked = InitialState,
        };
    }

    private void OnOptionChanged(bool newValue) {
        MemberInfo.SetValue(Config, newValue);
        Config.Save();
    }
}
