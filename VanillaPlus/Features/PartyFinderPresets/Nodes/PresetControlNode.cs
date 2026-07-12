using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.PartyFinderPresets.Nodes;

public sealed class PresetControlNode : SimpleComponentNode {
    private RenameAddon? renameAddon;

    private readonly TextHeaderNode headerNode;

    private readonly HorizontalFlexNode labelContainer;
    private readonly TextNode presetName;

    private readonly HorizontalFlexNode buttonContainer;

    private readonly TextButtonNode createUpdateButton;
    private readonly TextButtonNode deleteButton;

    public PresetControlNode() {
        headerNode = new TextHeaderNode {
            String = "Vanilla Plus - Preset Options",
        };
        headerNode.AttachNode(this);

        labelContainer = new HorizontalFlexNode {
            ItemSpacing = 4.0f,
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
            InitialNodes = [
                new TextNode {
                    String = "Currently Selected Preset:",
                    AlignmentType = AlignmentType.Center,
                },
                presetName = new TextNode {
                    String = "ERROR: PresetName",
                    AlignmentType = AlignmentType.Center,
                },
            ],
            IsVisible = false,
        };
        labelContainer.AttachNode(this);

        buttonContainer = new HorizontalFlexNode {
            ItemSpacing = 4.0f,
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
            InitialNodes = [
                createUpdateButton = new TextButtonNode {
                    OnClick = OnCreateUpdateClicked,
                    TextTooltip = "CollisionInit-uWu",
                },
                deleteButton = new TextButtonNode {
                    OnClick = OnDeleteClicked,
                },
            ],
        };
        buttonContainer.AttachNode(this);
    }

    protected override void Dispose(bool isNativeDestructor) {
        if (IsDisposed) return;

        renameAddon?.Dispose();
        renameAddon = null;

        base.Dispose(isNativeDestructor);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        headerNode.Size = new Vector2(Width, 20.0f);
        headerNode.Position = Vector2.Zero;

        labelContainer.Size = new Vector2(Width - 28.0f, 24.0f);
        labelContainer.Position = new Vector2(14.0f, 36.0f);
        labelContainer.RecalculateLayout();

        buttonContainer.Size = new Vector2(Width - 28.0f, 24.0f);
        buttonContainer.Position = new Vector2(14.0f, 72.0f);
        buttonContainer.RecalculateLayout();
    }

    private void OnCreateUpdateClicked() {
        createUpdateButton.HideTooltip();

        if (PresetEntry is null) {
            renameAddon = new RenameAddon {
                Title = "Party Finder Preset - Save Preset",
                InternalName = "PartyFinderPresetNew",
                AutoSelectAll = true,
                DefaultString = "Preset Name",
                OnRenameComplete = newName => {
                    var newPreset = new PresetEntry {
                        Name = newName.ToString(),
                    };

                    newPreset.LoadFromCurrentState();

                    Config.Presets.Add(newPreset);
                    Task.Run(Config.Save);

                    PresetEntry = newPreset;

                    renameAddon?.Dispose();
                    renameAddon = null;
                },
            };

            renameAddon.Open();
        }
        else {
            PresetEntry.LoadFromCurrentState();
            Task.Run(Config.Save);

            SetPreset(PresetEntry);
        }
    }

    private void OnDeleteClicked() {
        if (PresetEntry is null) return;

        Config.Presets.Remove(PresetEntry);
        Task.Run(Config.Save);

        PresetEntry = null;
    }

    private void SetPreset(PresetEntry preset) {
        labelContainer.IsVisible = true;
        presetName.String = preset.Name;

        createUpdateButton.String = "Update Selected Preset";
        createUpdateButton.TextTooltip = "[VanillaPlus] Update Selected Preset";
        createUpdateButton.IsEnabled = true;

        deleteButton.String = "Delete Selected Preset";
        deleteButton.IsEnabled = true;
    }

    private void ClearPreset() {
        labelContainer.IsVisible = false;

        createUpdateButton.String = "Create New Preset";
        createUpdateButton.TextTooltip = "[VanillaPlus] Create New Preset";
        createUpdateButton.IsEnabled = true;

        deleteButton.String = "Delete Selected Preset";
        deleteButton.IsEnabled = false;
    }

    public required PresetEntry? PresetEntry {
        get;
        set {
            field = value;

            if (value is not null) {
                SetPreset(value);
            }
            else {
                ClearPreset();
            }
        }
    }

    public required PartyFinderPresetConfig Config { get; set; }
}
