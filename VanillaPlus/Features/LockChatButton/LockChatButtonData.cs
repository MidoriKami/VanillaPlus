using VanillaPlus.Classes;

namespace VanillaPlus.Features.LockChatButton;

public class LockChatButtonData : GameModificationData<LockChatButtonData> {

    protected override string FileName => "LockChatButton";

    public bool IsLocked;
}
