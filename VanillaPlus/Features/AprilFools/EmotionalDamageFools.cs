using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// Adds emotional damage fly texts when any non-damage fly text appears.
/// </summary>
public class EmotionalDamageFools : FoolsModule {
    protected override void OnEnable()
        => Services.FlyTextGui.FlyTextCreated += OnFlyText;

    protected override void OnDisable() 
        => Services.FlyTextGui.FlyTextCreated -= OnFlyText;

    private void OnFlyText(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled) {
        if (!Config.EmotionalDamage) return;
        if (kind is FlyTextKind.Damage) return;

        Services.FlyTextGui.AddFlyText(
            FlyTextKind.Damage, 
            0, 
            67,
            (uint) val2, 
            "Emotional Damage", 
            string.Empty, 
            0,
            0,
            0
        );
    }
}
