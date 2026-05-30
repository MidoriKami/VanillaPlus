using System.Numerics;
using System.Threading.Tasks;
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
        protected override Task BuildUiAsync() {
            new ImGuiImageNode {
                Position = ContentStartPosition,
                Size = ContentSize,
                TexturePath = Assets.GetAssetPath("justmonika.png"),
                FitTexture = true,
            }.AttachNode(this);

            return Task.CompletedTask;
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

        Services.ClientState.TerritoryChanged += OnTerritoryChanged;

        return Task.CompletedTask;
    }

    protected override async Task OnDisable() {
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;

        await Task.WhenAll(monikaAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        monikaAddon = null;
    }

    private void OnTerritoryChanged(uint u)
        => Task.Run(() => monikaAddon?.OpenAsync());
}
