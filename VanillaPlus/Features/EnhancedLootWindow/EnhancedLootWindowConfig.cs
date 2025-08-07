using VanillaPlus.Classes;

namespace VanillaPlus.Features.EnhancedLootWindow;

public class EnhancedLootWindowConfig : GameModificationConfig<EnhancedLootWindowConfig> {
    protected override string FileName => "EnhancedLootWindow.config.json";
   
    public bool MarkUnobtainableItems = true;
    public bool MarkAlreadyObtainedItems = true;
}
