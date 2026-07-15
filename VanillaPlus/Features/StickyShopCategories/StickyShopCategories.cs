using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.StickyShopCategories;

public class StickyShopCategories : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_StickyShopCategories,
        Description = Strings.ModificationDescription_StickyShopCategories,
        Type = ModificationType.GameBehavior,
        Authors = ["Era"],
    };

    private StickyShopCategoriesData? config;

    public override async Task OnEnableAsync() {
        config = await StickyShopCategoriesData.Load();

        Services.GetService<IAddonLifecycle>().RegisterListener(AddonEvent.PostSetup, "InclusionShop", OnInclusionShopSetup);
        Services.GetService<IAddonLifecycle>().RegisterListener(AddonEvent.PreFinalize, "InclusionShop", OnInclusionShopFinalize);
    }

    public override async Task OnDisableAsync() {
        Services.GetService<IAddonLifecycle>().UnregisterListener(OnInclusionShopFinalize, OnInclusionShopSetup);

        if (config is not null) {
            await config.Save();
            config = null;
        }
    }

    private unsafe void OnInclusionShopSetup(AddonEvent type, AddonArgs args) {
        if (config is null) return;

        var shopId = args.ValueSpan[0].UInt;

        if (config.ShopConfigs.TryGetValue(shopId, out var currentShopConfig)) {
            var categoryDropDown = GetCategoryDropDown(args);
            categoryDropDown->SelectItem(currentShopConfig.Category);

            var agentInterface = AgentModule.Instance()->GetAgentByInternalId(AgentId.InclusionShop);
            agentInterface->SendCommand(1, [12, currentShopConfig.Category]);
            agentInterface->SendCommand(1, [13, currentShopConfig.SubCategory]);
        }
    }

    private unsafe void OnInclusionShopFinalize(AddonEvent type, AddonArgs args) {
        if (config is null) return;

        var shopId = args.ValueSpan[0].UInt;
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

        Services.PluginLog.Debug($"Saving Values: {dropDownCategoryIndex}, {dropDownSubCategoryIndex}", "StickyShopCategories");

        Task.Run(config.Save);
    }

    private static unsafe AtkComponentDropDownList* GetCategoryDropDown(AddonArgs args)
        => (AtkComponentDropDownList*)args.GetAddon<AtkUnitBase>()->GetComponentByNodeId(7);

    private static unsafe AtkComponentDropDownList* GetSubCategoryDropDown(AddonArgs args)
        => (AtkComponentDropDownList*)args.GetAddon<AtkUnitBase>()->GetComponentByNodeId(9);
}
