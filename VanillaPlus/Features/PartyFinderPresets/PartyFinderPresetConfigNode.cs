using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.PartyFinderPresets;

public class PartyFinderPresetConfigNode : ConfigNode<PresetInfo> {

    private readonly CategoryTextNode renameCategoryNode;
    private readonly TextInputNode renameInputNode;
    private readonly TextButtonNode confirmButtonNode;

    public PartyFinderPresetConfigNode() {
        renameCategoryNode = new CategoryTextNode {
            String = "Input new name",
        };
        renameCategoryNode.AttachNode(this);

        renameInputNode = new TextInputNode {
            OnInputReceived = input => renameInputNode!.IsError = !PresetManager.IsValidFileName(input.ToString()),
        };
        renameInputNode.AttachNode(this);

        confirmButtonNode = new TextButtonNode {
            String = "Apply",
            OnClick = () => {
                if (ConfigurationOption is not null && !renameInputNode.IsError) {
                    PresetManager.RenamePreset(ConfigurationOption.Name, renameInputNode.String);

                    ConfigurationOption.Name = renameInputNode.String;
                    OptionChanged(ConfigurationOption);
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        confirmButtonNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        renameInputNode.Size = new Vector2(Width * 2.0f / 3.0f, 30.0f);
        renameInputNode.Position = Size / 2.0f - renameInputNode.Size / 2.0f;

        renameCategoryNode.Size = new Vector2(Width / 2.0f, 32.0f);
        renameCategoryNode.Position = new Vector2(renameInputNode.X, renameInputNode.Y - renameInputNode.Height);

        confirmButtonNode.Size = new Vector2(100.0f, 24.0f);
        confirmButtonNode.Position = new Vector2(renameInputNode.X + renameInputNode.Width - 100.0f, renameInputNode.Y + renameInputNode.Height);
    }

    protected override void OptionChanged(PresetInfo? option) {
        renameInputNode.String = option?.Name ?? string.Empty;
    }
}
