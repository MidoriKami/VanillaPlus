using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.InventoryCooldowns;

public record ControllerNodeset {
    public required AddonController<AddonInventoryGrid> Controller { get; init; }
    public List<InventoryCooldownTextNode> Nodes { get; init; } = [];
}
