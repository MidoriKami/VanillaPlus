namespace VanillaPlus.Features.AprilFools;

public interface IFoolsModule {
    AprilFoolsConfig Config { set; }

    void Enable();
    void Disable();
}
