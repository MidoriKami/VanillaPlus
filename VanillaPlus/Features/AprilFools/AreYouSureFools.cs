using System;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.AprilFools;

public class AreYouSureFools : FoolsModule {

    private class AreYouSureAddon : NativeAddon {
        protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
            base.OnSetup(addon, atkValueSpan);

            var windowNode = WindowNode as WindowNode;
            windowNode?.CloseButtonNode.IsEnabled = false;

            new TextNode {
                Size = new Vector2(ContentSize.X - 20.0f, 50.0f),
                Position = ContentStartPosition + new Vector2(10.0f, 0.0f),
                LineSpacing = 16,
                TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
                String = "Are you sure whatever you did was a good idea? Like really, super sure that was the right move?",
            }.AttachNode(this);

            new HoldButtonNode {
                Size = new Vector2(100.0f, 36.0f),
                Position = ContentStartPosition + new Vector2(ContentSize.X / 2.0f - 100.0f / 2.0f, ContentSize.Y - 32.0f),
                String = "Probably",
                OnClick = Close,
            }.AttachNode(this);
        }
    }

    public override bool IsEnabledByConfig
        => Config.AreYouSure;

    private AreYouSureAddon? areYouSureAddon;
    private AddonController<AddonSelectYesno>? addonController;

    protected override async Task OnEnable() {
        areYouSureAddon = new AreYouSureAddon {
            InternalName = "AreYouSure",
            Title = "Are you sure?",
            Size = new Vector2(325.0f, 150.0f),
            DisableClose = true,
        };

        unsafe {
            addonController = new AddonController<AddonSelectYesno> {
                AddonName = "SelectYesno",
                OnFinalize = OnSelectYesNoFinalize,
            };
        }

        await addonController.EnableAsync();
    }

    protected override async Task OnDisable() {
        await addonController.DisposeAsyncSafe();
        addonController = null;

        await areYouSureAddon.DisposeAsyncSafe();
        areYouSureAddon = null;
    }

    private unsafe void OnSelectYesNoFinalize(AddonSelectYesno* addon)
        => areYouSureAddon?.Open();
}
