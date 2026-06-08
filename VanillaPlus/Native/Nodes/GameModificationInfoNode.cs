using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Native.Nodes;

public class GameModificationInfoNode : ResNode {

    private readonly ResNode noSelectionContainer;
    private readonly TextNode nothingSelectedText;

    private readonly ResNode titleContainer;

    private readonly TextNode titleText;
    private readonly HorizontalLineNode titleSeparator;
    private readonly TextNode categoryText;

    private readonly ImageDescriptionInfoNode imageDescriptionContainer;
    private readonly TextDescriptionInfoNode textDescriptionContainer;

    public GameModificationInfoNode() {
        noSelectionContainer = new ResNode();
        noSelectionContainer.AttachNode(this);

        nothingSelectedText = new TextNode {
            AlignmentType = AlignmentType.Center,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
            String = Strings.SelectionPrompt,
        };
        nothingSelectedText.AttachNode(noSelectionContainer);

        titleContainer = new ResNode {
            IsVisible = false,
        };
        titleContainer.AttachNode(this);

        titleText = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 22,
        };
        titleText.AttachNode(titleContainer);

        titleSeparator = new HorizontalLineNode();
        titleSeparator.AttachNode(titleContainer);

        categoryText = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 14,
            TextColor = ColorHelper.GetColor(3),
        };
        categoryText.AttachNode(titleContainer);

        imageDescriptionContainer = new ImageDescriptionInfoNode {
            IsVisible = false,
        };
        imageDescriptionContainer.AttachNode(this);

        textDescriptionContainer = new TextDescriptionInfoNode {
            IsVisible = false,
        };
        textDescriptionContainer.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        noSelectionContainer.Size = Size;
        nothingSelectedText.Size = Size;

        const float titleHeight = 65.0f;
        const float titleOffset = 14.0f;

        titleContainer.Size = new Vector2(Width, titleHeight);
        titleContainer.Position = new Vector2(0.0f, titleOffset);

        titleText.Size = new Vector2(Width, titleHeight / 2.0f - 2.0f);
        titleText.Position = new Vector2(0.0f, 0.0f);

        titleSeparator.Size = new Vector2(Width, 4.0f);
        titleSeparator.Position = new Vector2(0.0f, titleText.Bounds.Bottom);

        categoryText.Size = new Vector2(Width, titleHeight / 2.0f - 2.0f);
        categoryText.Position = new Vector2(0.0f, titleSeparator.Bounds.Bottom);

        imageDescriptionContainer.Size = new Vector2(Width, Height - titleHeight - titleOffset);
        imageDescriptionContainer.Position = new Vector2(0.0f, titleHeight + titleOffset);

        textDescriptionContainer.Size = new Vector2(Width, Height - titleHeight- titleOffset);
        textDescriptionContainer.Position = new Vector2(0.0f, titleHeight + titleOffset);
    }

    public void SetDisplayedGameModification(LoadedModification? gameModification) {
        if (gameModification is null) {
            ShowNoSelection();
        }
        else {
            titleText.String = gameModification.Modification.ModificationInfo.DisplayName;
            categoryText.String = gameModification.Modification.ModificationInfo.Type.Description;

            if (gameModification.Modification.ImageName is not null) {
                ShowImageDescription(gameModification);
            }
            else {
                ShowTextDescription(gameModification);
            }
        }
    }

    private void ShowNoSelection() {
        noSelectionContainer.IsVisible = true;
        titleContainer.IsVisible = false;
        imageDescriptionContainer.IsVisible = false;
        textDescriptionContainer.IsVisible = false;
    }

    private void ShowImageDescription(LoadedModification gameModification) {
        noSelectionContainer.IsVisible = false;
        titleContainer.IsVisible = true;
        imageDescriptionContainer.IsVisible = true;
        textDescriptionContainer.IsVisible = false;

        imageDescriptionContainer.LoadModificationInfo(gameModification);
    }

    private void ShowTextDescription(LoadedModification gameModification) {
        noSelectionContainer.IsVisible = false;
        titleContainer.IsVisible = true;
        imageDescriptionContainer.IsVisible = false;
        textDescriptionContainer.IsVisible = true;

        textDescriptionContainer.LoadModificationInfo(gameModification);
    }

    public unsafe void AddInteractionNode(AtkUnitBase* addon)
        => imageDescriptionContainer.AddInteractionNode(addon);
}
