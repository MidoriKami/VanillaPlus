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
public class JustMonikaFools : IFoolsModule {
    public required AprilFoolsConfig Config { get; set; }

    private class MonikaAddon : NativeAddon {
        protected override unsafe void OnSetup(AtkUnitBase* addon) {
            base.OnSetup(addon);

            new ImGuiImageNode {
                Position = ContentStartPosition,
                Size = ContentSize,
                TexturePath = Assets.GetAssetPath("justmonika.png"),
                FitTexture = true,
            }.AttachNode(this);
        }
    }

    private MonikaAddon? monikaAddon;

    public void Enable() {
        monikaAddon = new MonikaAddon {
            InternalName = "JustMonika",
            Title = "Just Monika",
            Size = new Vector2(1300.0f, 700.0f) / 2.0f,
        };
        
        Services.ClientState.Login += OnLogin;
    }

    public void Disable() {
        Services.ClientState.Login -= OnLogin;
        
        monikaAddon?.Dispose();
        monikaAddon = null;
    }

    private void OnLogin() {
        if (!Config.JustMonika) return;
        
        monikaAddon?.Open();
    }
}
