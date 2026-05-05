using System.Collections.Generic;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentLookingForGroup;

namespace VanillaPlus.Features.PartyFinderPresets;

public unsafe class PresetEntry {
    public string Name = "Name Not Set";

    // Data from Agent
    public ushort MinimumAverageItemLevel;
    public bool RequireMinimumAverageItemLevel;

    // Data from RecruitmentSub
    public DutyCategory DutyCategory;
    public ushort SelectedDutyId;
    public Objective Objective;
    public CompletionStatus CompletionStatus;
    public LootRule LootRule;
    public ushort Password = 10000;
    public Language Language;
    public bool LimitToCurrentWorld;
    public bool OnePlayerPerJob;
    public string Description = string.Empty;
    public List<ulong> SlotFlags = [];

    public void LoadFromCurrentState() {
        var agent = Instance();

        MinimumAverageItemLevel = agent->AvgItemLv;
        RequireMinimumAverageItemLevel = agent->AvgItemLvEnabled == 1;

        ref var recruitmentSub = ref agent->StoredRecruitmentInfo;
        DutyCategory = recruitmentSub.SelectedCategory;
        SelectedDutyId = recruitmentSub.SelectedDutyId;
        Objective = recruitmentSub.Objective;
        CompletionStatus = recruitmentSub.CompletionStatus;
        LootRule = recruitmentSub.LootRule;
        Password = recruitmentSub.Password;
        Language = recruitmentSub.LanguageFlags;
        LimitToCurrentWorld = recruitmentSub.LimitRecruitingToWorld == 1;
        OnePlayerPerJob = recruitmentSub.OnePlayerPerJob == 1;
        Description = recruitmentSub.CommentString;

        SlotFlags.Clear();
        foreach (var slot in recruitmentSub.SlotFlags) {
            SlotFlags.Add(slot);
        }
    }

    public void ApplyPreset() {
        var agent = Instance();

        agent->AvgItemLv = MinimumAverageItemLevel;
        agent->AvgItemLvEnabled = (byte)(RequireMinimumAverageItemLevel ? 1 : 0);

        ref var recruitmentSub = ref agent->StoredRecruitmentInfo;
        recruitmentSub.SelectedCategory = DutyCategory;
        recruitmentSub.SelectedDutyId = SelectedDutyId;
        recruitmentSub.Objective = Objective;
        recruitmentSub.CompletionStatus = CompletionStatus;
        recruitmentSub.LootRule = LootRule;
        recruitmentSub.Password = Password;
        recruitmentSub.LanguageFlags = Language;
        recruitmentSub.LimitRecruitingToWorld = (byte)(LimitToCurrentWorld ? 1 : 0);
        recruitmentSub.OnePlayerPerJob = (byte)(OnePlayerPerJob ? 1 : 0);
        recruitmentSub.CommentString = Description;

        ApplyRoleSelection();
    }

    public void ApplyRoleSelection() {
        var agent = Instance();
        ref var recruitmentSub = ref agent->StoredRecruitmentInfo;

        if (SlotFlags.Count == recruitmentSub.SlotFlags.Length) {
            foreach (var index in Enumerable.Range(0, recruitmentSub.SlotFlags.Length)) {
                recruitmentSub.SlotFlags[index] = SlotFlags[index];
            }
        }
    }
}
