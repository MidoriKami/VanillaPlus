using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.StickyShopCategories;

public unsafe class StickyShopCategories : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_StickyShopCategories"),
        Description = Strings("ModificationDescription_StickyShopCategories"),
        Type = ModificationType.GameBehavior,
        Authors = ["Era"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private StickyShopCategoriesData? config;

    public override void OnEnable() {
        config = StickyShopCategoriesData.Load();

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "InclusionShop", OnInclusionShopSetup);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InclusionShop", OnInclusionShopFinalize);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnInclusionShopFinalize, OnInclusionShopSetup);

        config?.Save();
        config = null;
    }

    private void OnInclusionShopSetup(AddonEvent type, AddonArgs args) {
        if (config is null) return;

        var shopId = args.AtkValueSpan[0].UInt;

        if (config.ShopConfigs.TryGetValue(shopId, out var currentShopConfig)) {
            var categoryDropDown = GetCategoryDropDown(args);
            categoryDropDown->SelectItem(currentShopConfig.Category);

            var agentInterface = AgentModule.Instance()->GetAgentByInternalId(AgentId.InclusionShop);
            agentInterface->SendCommand(1, [12, currentShopConfig.Category]);
            agentInterface->SendCommand(1, [13, currentShopConfig.SubCategory]);
        }
    }

    private void OnInclusionShopFinalize(AddonEvent type, AddonArgs args) {
        if (config is null) return;

        var shopId = args.AtkValueSpan[0].UInt;
        var dropDownCategoryIndex = GetCategoryDropDown(args)->GetSelectedItemIndex();
        var dropDownSubCategoryIndex = GetSubCategoryDropDown(args)->GetSelectedItemIndex();

        if (config.ShopConfigs.TryGetValue(shopId, out var shopConfig)) {
            shopConfig.Category = dropDownCategoryIndex;
            shopConfig.SubCategory = dropDownSubCategoryIndex;
        }
        else {
            config.ShopConfigs.Add(shopId, new ShopConfig {
                Category = dropDownCategoryIndex,
                SubCategory = dropDownSubCategoryIndex,
            });
        }

        Services.PluginLog.Debug($"Saving Values: {dropDownCategoryIndex}, {dropDownSubCategoryIndex}");
        
        config.Save();
    }

    private static AtkComponentDropDownList* GetCategoryDropDown(AddonArgs args) 
        => (AtkComponentDropDownList*) args.GetAddon<AtkUnitBase>()->GetComponentByNodeId(7);

    private static AtkComponentDropDownList* GetSubCategoryDropDown(AddonArgs args) 
        => (AtkComponentDropDownList*) args.GetAddon<AtkUnitBase>()->GetComponentByNodeId(9);
}
