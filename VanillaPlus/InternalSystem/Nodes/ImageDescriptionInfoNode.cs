using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.InternalSystem.Nodes;

public class ImageDescriptionInfoNode : ResNode {

    private readonly ResNode imageContainerNode;
    private readonly ImGuiImageNode imageNode;
    private readonly BorderNineGridNode frameNode;

    private readonly TextNode description;

    private bool isImageHovered;
    private bool isImageEnlarged;

    public ImageDescriptionInfoNode() {
        description = new TextNode {
            AlignmentType = AlignmentType.TopLeft,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
        };
        description.AttachNode(this);

        imageContainerNode = new ResNode();
        imageContainerNode.AttachNode(this);

        imageNode = new ImGuiImageNode {
            FitTexture = true,
            DrawFlags = DrawFlags.ClickableCursor,
        };
        imageNode.AttachNode(imageContainerNode);

        frameNode = new BorderNineGridNode {
            Offsets = new Vector4(40.0f),
        };
        frameNode.AttachNode(imageNode);

        imageNode.AddEvent(AtkEventType.MouseClick, () => {
            if (!isImageEnlarged) {
                imageNode.Scale = new Vector2(2.5f, 2.5f);
            }
            else {
                if (isImageHovered) {
                    imageNode.Scale = new Vector2(1.05f, 1.05f);
                }
                else {
                    imageNode.Scale = Vector2.One;
                }
            }

            isImageEnlarged = !isImageEnlarged;
        });

        imageNode.AddEvent(AtkEventType.MouseOver, () => {
            if (isImageEnlarged) return;

            imageNode.Scale = new Vector2(1.05f, 1.05f);
            isImageHovered = true;
        });

        imageNode.AddEvent(AtkEventType.MouseOut, () => {
            if (isImageEnlarged) return;

            imageNode.Scale = Vector2.One;
            isImageHovered = false;
        });
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        imageContainerNode.Size = new Vector2(Width * 2.0f / 3.0f, Width * 2.0f / 3.0f);
        imageContainerNode.Position = new Vector2(Width / 6.0f, 16.0f);
        imageContainerNode.Origin = imageContainerNode.Bounds.Center;

        imageNode.Size = imageContainerNode.Size;
        imageNode.Position = new Vector2(0.0f, 0.0f);

        frameNode.Size = imageNode.Size + new Vector2(32.0f, 32.0f);
        frameNode.Position = new Vector2(-16.0f, -16.0f);

        description.Size = new Vector2(Width - 64.0f, Height - imageContainerNode.Bounds.Bottom - 32.0f);
        description.Position = new Vector2(32.0f, imageContainerNode.Bounds.Bottom + 16.0f);
    }

    public void LoadModificationInfo(LoadedModification modification) {
        if (modification.Modification.ImageName is { } imageName) {
            Task.Run(async () => {
                imageContainerNode.IsVisible = false;

                var texture = await Services.TextureProvider.GetFromFile(Assets.GetAssetPath(imageName)).RentAsync();
                imageNode.LoadTexture(texture);
                imageNode.TextureSize = texture.Size;

                if (texture.Width > texture.Height) {
                    var ratio = texture.Width / imageContainerNode.Width;
                    var multiplier = 1 / ratio;

                    imageNode.Width = imageContainerNode.Width;
                    imageNode.Height = texture.Height * multiplier;
                    imageNode.Y = (imageContainerNode.Width - imageContainerNode.Height) / 2.0f;
                    imageNode.X = 0.0f;
                }
                else {
                    var ratio = texture.Height / imageContainerNode.Width;
                    var multiplier = 1 / ratio;

                    imageNode.Height = imageContainerNode.Width;
                    imageNode.Width = texture.Width * multiplier;
                    imageNode.X = (imageContainerNode.Width - imageContainerNode.Width) / 2.0f;
                    imageNode.Y = 0.0f;
                }

                frameNode.Position = new Vector2(-16.0f, -16.0f);
                frameNode.Size = imageNode.Size + new Vector2(32.0f, 32.0f);

                imageNode.Position = imageContainerNode.Size / 2.0f - imageNode.Size / 2.0f;
                imageNode.Origin = imageNode.Bounds.Center;

                imageContainerNode.IsVisible = true;
            });
        }

        description.String = modification.Modification.ModificationInfo.Description;
    }

    public unsafe void AddInteractionNode(AtkUnitBase* addon) {
        addon->AdditionalFocusableNodes[0] = (AtkResNode*)imageNode;
    }
}
