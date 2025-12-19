using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

public static class TextureProviderExtensions {
    extension(ITextureProvider textureProvider) {
        public ISharedImmediateTexture PlaceholderTexture => textureProvider.GetFromGameIcon(60042);
    }
}
