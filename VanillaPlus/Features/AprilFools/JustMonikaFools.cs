using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
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
    
    protected override void OnEnable() {
        monikaAddon = new MonikaAddon {
            InternalName = "JustMonika",
            Title = "Just Monika",
            Size = new Vector2(650.0f, 350.0f),
        };

        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    protected override void OnDisable() {
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;

        monikaAddon?.Dispose();
        monikaAddon = null;
    }

    private void OnTerritoryChanged(ushort obj)
        => monikaAddon?.Open();
}
