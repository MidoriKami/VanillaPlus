using System;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using VanillaPlus.Features.HideUnwantedBanners.Nodes;

namespace VanillaPlus.Features.HideUnwantedBanners.Addons;

public class HideUnwantedBannersAddon(HideUnwantedBannersConfig config) : NativeAddon {
    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        listNode = new ListNode<BannerConfig, BannerConfigListItemNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            OptionsList = config.BannerSettings,
        };
        listNode.AttachNode(this);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        listNode?.Update();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        listNode = null;
        Task.Run(config.Save);
    }

    private ListNode<BannerConfig, BannerConfigListItemNode>? listNode;
}
