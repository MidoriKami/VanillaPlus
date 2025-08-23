using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using static VanillaPlus.Features.StickyShopCategories.StickyShopCategoriesConfig;

namespace VanillaPlus.Features.StickyShopCategories;

public class StickyShopCategories : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Sticky Shop Categories",
        Description = "Remembers the selected category and subcategories for certain vendors.",
        Type = ModificationType.UserInterface,
        
        Authors = [ "Era" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private bool hasSetCategory = false;
    private bool hasIgnoredFirstEvent = false;
    private ShopConfig? currentShopConfig = null;
    private StickyShopCategoriesConfig? config;

    public override void OnEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "InclusionShop", OnPreFinalize);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "InclusionShop", OnPreRefresh);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "InclusionShop", OnPostRefresh);
        config = StickyShopCategoriesConfig.Load();
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(OnPreFinalize, OnPreRefresh, OnPostRefresh);
        config?.Save();
        config = null;
    }

    private unsafe void OnPreRefresh(AddonEvent type, AddonArgs args) {
        if (args is not AddonRefreshArgs actualArgs) {
            Services.PluginLog.Error("InclusionShop: OnPreRefresh received null or invalid args.");
            return;
        }
        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.InclusionShop);
        if (agent == null) return;
        var addon = (AtkUnitBase*)actualArgs.Addon.Address;
        if (addon == null)
            return;
        var categoryDropDown = (AtkComponentDropDownList*)addon->GetComponentByNodeId(7);
        if (categoryDropDown == null)
            return;
        var subCategoryDropDown = (AtkComponentDropDownList*)addon->GetComponentByNodeId(9);
        if (subCategoryDropDown == null)
            return;
        var shopText = addon->AtkValues[96].String;
        var shopRow = GetShopRow(shopText);
        if (shopRow < 0) {
            Services.PluginLog.Error($"InclusionShop: Could not find shop row for text: {shopText}");
            return;
        }
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

            var retVal = stackalloc AtkValue[1];
            var values = stackalloc AtkValue[2];

            AtkValue* vals = (AtkValue*)actualArgs.AtkValues;
            vals[99].SetUInt(currentShopConfig.CategoryId);
            vals[100].SetUInt(currentShopConfig.SubCategoryId);

            agent->SendCommand(1, [12, currentShopConfig.CategoryIndex]);
            agent->SendCommand(1, [13, currentShopConfig.SubCategoryIndex]);


            if (categoryDropDown != null && categoryDropDown->GetComponentType() == ComponentType.DropDownList)
                categoryDropDown->SelectItem(currentShopConfig.CategoryIndex);
            else
                Services.PluginLog.Error("[StickyShopCategories] OnPreRefresh Category DropDown is null or invalid.");
            if (subCategoryDropDown != null && subCategoryDropDown->GetComponentType() == ComponentType.DropDownList)
                subCategoryDropDown->SelectItem(currentShopConfig.SubCategoryIndex);
            else
                Services.PluginLog.Error("OnPreRefresh SubCategory DropDown is null or invalid.");
        }
    }

    private unsafe void OnPostRefresh(AddonEvent type, AddonArgs args) {
        if (!hasIgnoredFirstEvent) {
            hasIgnoredFirstEvent = true;
            return;
        }
        if (args is not AddonRefreshArgs actualArgs) {
            Services.PluginLog.Error("InclusionShop: OnPostRefresh received null or invalid args.");
            return;
        }
        if (currentShopConfig == null) return;
        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.InclusionShop);
        if (agent == null) return;
        var addon = RaptureAtkUnitManager.Instance()->GetAddonById((ushort)agent->GetAddonId());
        if (addon == null)
            return;
        var categoryDropDown = (AtkComponentDropDownList*)addon->GetComponentByNodeId(7);
        if (categoryDropDown == null)
            return;
        var subcategoryDropDown = (AtkComponentDropDownList*)addon->GetComponentByNodeId(9);
        if (subcategoryDropDown == null)
            return;
        currentShopConfig.CategoryId = addon->AtkValues[99].UInt;
        currentShopConfig.SubCategoryId = addon->AtkValues[100].UInt;
        currentShopConfig.CategoryIndex = categoryDropDown->GetSelectedItemIndex();
        currentShopConfig.SubCategoryIndex = subcategoryDropDown->GetSelectedItemIndex();
    }

    private unsafe void OnPreFinalize(AddonEvent type, AddonArgs args) {
        hasIgnoredFirstEvent = false;
        hasSetCategory = false;
        if (currentShopConfig != null)
            SaveShopConfig(currentShopConfig);
    }

    private ShopConfig? GetShopConfig(string searchText) {
        var rowId = GetShopRow(searchText);
        if (rowId == 0) {
            Services.PluginLog.Error($"InclusionShop: Could not find shop config for search text: {searchText}");
            return null;
        }
        var c = config!.ShopConfigs.FirstOrDefault(x => x.ShopId == rowId);
        if (c == null) {
            return null;
        }
        return c;
    }

    private void SaveShopConfig(ShopConfig cfg) {
        var existingConfig = config!.ShopConfigs.FirstOrDefault(x => x.ShopId == cfg.ShopId);
        if (existingConfig != null) {
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

    private static uint GetShopRow(string searchText) {
        var search = Services.DataManager.GetExcelSheet<InclusionShopWelcomText>().FirstOrDefault(x => x.Unknown0.ExtractText().Contains(searchText, StringComparison.OrdinalIgnoreCase));
        if (search.RowId < 0) {
            Services.PluginLog.Error($"InclusionShop: Could not find shop row for search text: {searchText}");
            return 0;
        }
        return search.RowId;
    }
}
