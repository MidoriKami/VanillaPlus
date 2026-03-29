namespace VanillaPlus.Features.AprilFools;

public abstract class FoolsModule {
    public required AprilFoolsConfig Config { set; protected get; }
    public abstract bool IsEnabledByConfig { get; }

    private bool isEnabled;
    
    public void Toggle(bool newState) {
        if (isEnabled && !newState) {
            Disable();
        }
        else if (!isEnabled && newState) {
            Enable();
        }
    }

    public void Enable() {
        if (!isEnabled) {
            OnEnable();
            isEnabled = true;
        }
    }

    public void Disable() {
        if (isEnabled) {
            OnDisable();
            isEnabled = false;
        }
    }

    protected abstract void OnEnable();
    protected abstract void OnDisable();
}
