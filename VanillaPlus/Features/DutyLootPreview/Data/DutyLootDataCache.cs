using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Text.ReadOnly;
using LuminaSupplemental.Excel.Model;
using LuminaSupplemental.Excel.Services;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.DutyLootPreview.Data;

public class DutyLootDataCache : IDisposable {
    public enum CacheState { Empty, Loading, Loaded }

    public event Action? OnChanged;
    public CacheState State { get; private set; } = CacheState.Empty;

    private readonly Debouncer loadDebouncer = new();

    public void Dispose() => loadDebouncer.Dispose();
    private ConcurrentDictionary<(uint, uint), DungeonBoss> dungeonBossIndex = new(); // (cfcId, fightNo)
    private ConcurrentDictionary<uint, DungeonChest> dungeonChestIndex = new(); // (cfcId, chest rowId)
    private ConcurrentDictionary<uint, DutyLootData> dutyLootByContentId = new();

    private static readonly ReadOnlySeString DungeonChestSource = "Dungeon Chest";

    public DutyLootData ReadDutyLootData(uint contentId) {
        if (State != CacheState.Loaded) { return DutyLootData.Empty(contentId); }
        if (dutyLootByContentId.TryGetValue(contentId, out var data)) {
            return data;
        }
        return DutyLootData.Empty(contentId);
    }

    public void LoadCacheAsync(bool forceReload = false, uint? onlyContentId = null) {
        if (!forceReload && State == CacheState.Loaded) return;
        loadDebouncer.Run(ct => LoadCacheAsync(ct, onlyContentId));
    }

    public void ClearCache() {
        loadDebouncer.Cancel();
        dungeonBossIndex.Clear();
        dungeonChestIndex.Clear();
        dutyLootByContentId.Clear();
        State = CacheState.Empty;
        OnChanged?.Invoke();
    }

    private void LoadCacheAsync(CancellationToken ct, uint? onlyContentId) {
        dungeonBossIndex.Clear();
        dungeonChestIndex.Clear();
        dutyLootByContentId.Clear();
        State = CacheState.Loading;
        OnChanged?.Invoke();

        try {
            foreach (var drop in LoadItems<DungeonBossDrop>(CsvLoader.DungeonBossDropResourceName)) {
                if (ct.IsCancellationRequested) return;
                if (onlyContentId.HasValue && drop.ContentFinderConditionId != onlyContentId) continue;
                AddBossSource(drop.ContentFinderConditionId, drop.FightNo, drop.ItemId);
            }

            foreach (var drop in LoadItems<DungeonBossChest>(CsvLoader.DungeonBossChestResourceName)) {
                if (ct.IsCancellationRequested) return;
                if (onlyContentId.HasValue && drop.ContentFinderConditionId != onlyContentId) continue;
                AddBossSource(drop.ContentFinderConditionId, drop.FightNo, drop.ItemId);
            }

            foreach (var drop in LoadItems<DungeonChestItem>(CsvLoader.DungeonChestItemResourceName)) {
                if (ct.IsCancellationRequested) return;
                AddDungeonChestSource(drop.ChestId, drop.ItemId, onlyContentId);
            }

            State = CacheState.Loaded;
        }
        catch (Exception ex) {
            Services.PluginLog.Error(ex, "Failed to load duty loot");
            State = CacheState.Empty;
        }
        finally {
            OnChanged?.Invoke();
        }
    }

    private void AddBossSource(uint cfcId, uint fightNo, uint itemId) {
        if (itemId == 0) return;

        var boss = GetDungeonBoss(cfcId, fightNo);
        if (boss == null) return;

        var bossName = Services.SeStringEvaluator.EvaluateObjStr(ObjectKind.BattleNpc, boss.BNpcNameId);
        if (string.IsNullOrEmpty(bossName)) return;

        var dutyLootData = dutyLootByContentId.GetOrAdd(cfcId, DutyLootData.Empty(cfcId));
        var item = dutyLootData.GetOrAddItem(itemId);
        if (item == null) return;

        item.Sources.Add(bossName);
    }

    private void AddDungeonChestSource(uint chestRowId, uint itemId, uint? onlyContentId) {
        if (itemId == 0) return;

        var chest = GetDungeonChest(chestRowId);
        if (chest == null) return;

        var cfcId = chest.ContentFinderConditionId;
        if (onlyContentId.HasValue && cfcId != onlyContentId) return;

        var dutyLootData = dutyLootByContentId.GetOrAdd(cfcId, DutyLootData.Empty(cfcId));
        var item = dutyLootData.GetOrAddItem(itemId);
        if (item == null) return;

        item.Sources.Add(DungeonChestSource);
    }

    private DungeonBoss? GetDungeonBoss(uint cfcId, uint fightNo) {
        if (dungeonBossIndex.IsEmpty) {
            foreach (var boss in LoadItems<DungeonBoss>(CsvLoader.DungeonBossResourceName)) {
                dungeonBossIndex[(boss.ContentFinderConditionId, boss.FightNo)] = boss;
            }
        }

        return dungeonBossIndex.GetValueOrDefault((cfcId, fightNo));
    }

    private DungeonChest? GetDungeonChest(uint chestRowId) {
        if (dungeonChestIndex.IsEmpty) {
            foreach (var chest in LoadItems<DungeonChest>(CsvLoader.DungeonChestResourceName)) {
                dungeonChestIndex[chest.RowId] = chest;
            }
        }

        return dungeonChestIndex.GetValueOrDefault(chestRowId);
    }

    private static List<T> LoadItems<T>(string resourceName) where T : ICsv, new() => CsvLoader.LoadResource<T>(
        resourceName: resourceName,
        includesHeaders: false,
        out _,
        out _,
        Services.DataManager.GameData,
        Services.DataManager.GameData.Options.DefaultExcelLanguage
    );
}
