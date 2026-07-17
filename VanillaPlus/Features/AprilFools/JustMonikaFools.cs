using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// Displays a window with a picture of Monika from Doki Doki Literature Club.
/// The window will reappear on each login and can simply be dismissed at any time.
/// </summary>
public class JustMonikaFools : FoolsModule {
    private class MonikaAddon : NativeAddon {
        protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
            base.OnSetup(addon, atkValueSpan);

            new ImGuiImageNode {
                Position = ContentStartPosition,
                Size = ContentSize,
                TexturePath = Assets.GetAssetPath("justmonika.png"),
                FitTexture = true,
            }.AttachNode(this);
        }
    }

    private MonikaAddon? monikaAddon;

    public override bool IsEnabledByConfig
        => Config.JustMonika;

    protected override Task OnEnable() {
        monikaAddon = new MonikaAddon {
            InternalName = "JustMonika",
            Title = "Just Monika",
            Size = new Vector2(650.0f, 350.0f),
        };

        Service<IClientState>.Get().TerritoryChanged += OnTerritoryChanged;

        return Task.CompletedTask;
    }

    protected override async Task OnDisable() {
        Service<IClientState>.Get().TerritoryChanged -= OnTerritoryChanged;

        await Task.WhenAll(monikaAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        monikaAddon = null;
    }

    private void OnTerritoryChanged(uint u)
        => monikaAddon?.Open();
}
