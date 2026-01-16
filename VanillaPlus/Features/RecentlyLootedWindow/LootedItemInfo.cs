using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public record LootedItemInfo(uint ItemId, uint IconId, ReadOnlySeString Name, int Quantity);
