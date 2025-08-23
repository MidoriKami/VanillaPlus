using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using static VanillaPlus.Features.StickyShopCategories.StickyShopCategoriesData;

namespace VanillaPlus.Features.StickyShopCategories;

public class StickyShopCategories : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Sticky Shop Categories",
        Description = "Remembers the selected category and subcategories for certain vendors.",
        Type = ModificationType.UserInterface,
        Authors = ["Era"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private bool hasSetCategory;
    private bool hasIgnoredFirstEvent;
    private ShopConfig? currentShopConfig;
    private StickyShopCategoriesData? config;

    public override void OnEnable() {
        config = StickyShopCategoriesData.Load();

        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InclusionShop", OnPreFinalize);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "InclusionShop", OnPreRefresh);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "InclusionShop", OnPostRefresh);
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnPreFinalize, OnPreRefresh, OnPostRefresh);

        config?.Save();
        config = null;
    }

    private unsafe void OnPreRefresh(AddonEvent type, AddonArgs args) {
        if (args is not AddonRefreshArgs actualArgs) return;

        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.InclusionShop);
        if (agent is null) return;

        var addon = (AtkUnitBase*)actualArgs.Addon.Address;
        if (addon is null) return;

        var categoryDropDown = GetCategoryDropDown(addon);
        if (categoryDropDown == null) return;

        var subcategoryDropDown = GetSubCategoryDropDown(addon);
        if (subcategoryDropDown == null) return;

        var shopText = addon->AtkValues[96].String;
        var shopRow = GetShopRow(shopText);

        if (currentShopConfig?.ShopId != shopRow) {
            currentShopConfig = GetShopConfig(shopText);
        }
        if (currentShopConfig == null) {
            currentShopConfig = new ShopConfig { ShopId = shopRow };
            hasSetCategory = true; // skip setting category for first time
            return;
        }

        if (!hasSetCategory) {
            hasSetCategory = true;

            var vals = (AtkValue*)actualArgs.AtkValues;
            vals[99].SetUInt(currentShopConfig.CategoryId);
            vals[100].SetUInt(currentShopConfig.SubCategoryId);

            agent->SendCommand(1, [12, currentShopConfig.CategoryIndex]);
            agent->SendCommand(1, [13, currentShopConfig.SubCategoryIndex]);

            categoryDropDown->SelectItem(currentShopConfig.CategoryIndex);
            subcategoryDropDown->SelectItem(currentShopConfig.SubCategoryIndex);
        }
    }

    private unsafe void OnPostRefresh(AddonEvent type, AddonArgs args) {
        if (args is not AddonRefreshArgs actualArgs) return;
        if (currentShopConfig == null) return;

        if (!hasIgnoredFirstEvent) {
            hasIgnoredFirstEvent = true;
            return;
        }

        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.InclusionShop);
        if (agent == null) return;

        var addon = (AtkUnitBase*)actualArgs.Addon.Address;
        if (addon == null) return;

        var categoryDropDown = GetCategoryDropDown(addon);
        if (categoryDropDown == null) return;

        var subcategoryDropDown = GetSubCategoryDropDown(addon);
        if (subcategoryDropDown == null) return;

        currentShopConfig.CategoryId = addon->AtkValues[99].UInt;
        currentShopConfig.SubCategoryId = addon->AtkValues[100].UInt;
        currentShopConfig.CategoryIndex = categoryDropDown->GetSelectedItemIndex();
        currentShopConfig.SubCategoryIndex = subcategoryDropDown->GetSelectedItemIndex();
    }

    private static unsafe AtkComponentDropDownList* GetCategoryDropDown(AtkUnitBase* addon) {
        var componentNode = addon->GetComponentByNodeId(7);
        if (componentNode->GetComponentType() is ComponentType.DropDownList) {
            return (AtkComponentDropDownList*)componentNode;
        }

        return null;
    }

    private static unsafe AtkComponentDropDownList* GetSubCategoryDropDown(AtkUnitBase* addon) {
        var componentNode = addon->GetComponentByNodeId(9);
        if (componentNode->GetComponentType() is ComponentType.DropDownList) {
            return (AtkComponentDropDownList*)componentNode;
        }

        return null;
    }

    private void OnPreFinalize(AddonEvent type, AddonArgs args) {
        hasIgnoredFirstEvent = false;
        hasSetCategory = false;

        if (currentShopConfig != null) {
            SaveShopConfig(currentShopConfig);
        }
    }

    private ShopConfig? GetShopConfig(string searchText) {
        if (config is null) return null;

        var rowId = GetShopRow(searchText);
        if (rowId is 0) return null;

        return config.ShopConfigs.FirstOrDefault(x => x.ShopId == rowId);
    }

    private void SaveShopConfig(ShopConfig cfg) {
        var existingConfig = config!.ShopConfigs.FirstOrDefault(x => x.ShopId == cfg.ShopId);

        if (existingConfig is not null) {
            existingConfig.CategoryId = cfg.CategoryId;
            existingConfig.SubCategoryId = cfg.SubCategoryId;
            existingConfig.CategoryIndex = cfg.CategoryIndex;
            existingConfig.SubCategoryIndex = cfg.SubCategoryIndex;
        }
        else {
            config.ShopConfigs.Add(cfg);
        }

        config.Save();
    }

    private static uint GetShopRow(string searchText)
        => Services.DataManager.GetExcelSheet<InclusionShopWelcomText>()
            .FirstOrDefault(x => x.Unknown0.ExtractText().Contains(searchText, StringComparison.OrdinalIgnoreCase)).RowId;
}
