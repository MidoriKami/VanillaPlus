using System.Threading.Tasks;

namespace VanillaPlus.Features.AprilFools;

public abstract class FoolsModule {
    public required AprilFoolsConfig Config { set; protected get; }
    public abstract bool IsEnabledByConfig { get; }

    private bool isEnabled;

    public async Task Toggle(bool newState) {
        if (isEnabled && !newState) {
            await DisableAsync();
        }
        else if (!isEnabled && newState) {
            await EnableAsync();
        }
    }

    public async Task EnableAsync() {
        if (!isEnabled) {
            await OnEnable();
            isEnabled = true;
        }
    }

    public async Task DisableAsync() {
        if (isEnabled) {
            await OnDisable();
            isEnabled = false;
        }
    }

    protected abstract Task OnEnable();
    protected abstract Task OnDisable();
}
