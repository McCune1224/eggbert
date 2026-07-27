using Godot;
using System;
using System.Threading.Tasks;

// Headless verifier for Quest auto-pin to HUD (QuestManager.GetPinnedQuest).
//
// Tests the new behavior where GetPinnedQuest() falls back to the first
// active quest when no explicit pin is set, and respects the "__unpinned__"
// sentinel when the user explicitly unpins.
//
// Run with: godot --headless --path . --script res://tests/VerifyQuestAutoPin.cs

public partial class VerifyQuestAutoPin : SceneTree
{
    private int _failures;

    private const string PinnedFlag = "quest_pinned_id";
    private const string UnpinnedSentinel = "__unpinned__";
    private const string BuiltInQuestId = "factory_gate_shift_end";
    private const string CompletionFlag = "arrested";

    public override async void _Initialize()
    {
        await ToSignal(Root, Window.SignalName.Ready);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (WorldFlags.Instance == null)
        {
            GD.PushError("WorldFlags autoload missing");
            Quit(1);
            return;
        }

        if (QuestManager.Instance == null)
        {
            GD.PushError("QuestManager autoload missing");
            Quit(1);
            return;
        }

        // Clear all flags for a clean slate.
        WorldFlags.Instance.ClearAll();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        TestFreshGameAutoPins();
        TestExplicitPin();
        TestUnpinSetsSentinel();
        TestRepinAfterUnpin();
        TestCompletedQuestFallback();
        await TestPersistedSentinel();

        if (_failures == 0)
        {
            GD.Print("[quest-pin] ALL OK");
            Quit(0);
        }
        else
        {
            GD.PushError($"[quest-pin] {_failures} failure(s)");
            Quit(1);
        }
    }

    // Test 1: Fresh game (empty flag) → auto-pins first active quest.
    private void TestFreshGameAutoPins()
    {
        WorldFlags.Instance.ClearAll();

        var pinned = QuestManager.Instance.GetPinnedQuest();
        if (!Assert(pinned != null, "FRESH GAME: GetPinnedQuest() should return first active quest (not null)"))
            return;
        Assert(pinned.Id == BuiltInQuestId, $"FRESH GAME: expected '{BuiltInQuestId}', got '{pinned.Id}'");
        GD.Print($"[quest-pin] FRESH GAME: auto-pinned quest '{pinned.Id}'");
    }

    private void TestExplicitPin()
    {
        WorldFlags.Instance.ClearAll();
        QuestManager.Instance.PinQuest(BuiltInQuestId);

        var pinned = QuestManager.Instance.GetPinnedQuest();
        if (!Assert(pinned != null, "EXPLICIT PIN: GetPinnedQuest() should return the pinned quest"))
            return;
        Assert(pinned.Id == BuiltInQuestId, $"EXPLICIT PIN: expected '{BuiltInQuestId}', got '{pinned.Id}'");
        GD.Print($"[quest-pin] EXPLICIT PIN: quest '{pinned.Id}' shows on HUD");
    }

    // Test 3: Unpin → returns null and stores sentinel in WorldFlags.
    private void TestUnpinSetsSentinel()
    {
        WorldFlags.Instance.ClearAll();
        QuestManager.Instance.PinQuest(BuiltInQuestId);
        QuestManager.Instance.UnpinQuest();

        var pinned = QuestManager.Instance.GetPinnedQuest();
        Assert(pinned == null, "UNPIN: GetPinnedQuest() should return null after unpin");

        var flagVal = WorldFlags.Instance.GetFlag(PinnedFlag);
        Assert(flagVal.AsString() == UnpinnedSentinel,
            $"UNPIN: flag value should be sentinel '{UnpinnedSentinel}', got '{flagVal.AsString()}'");
        GD.Print("[quest-pin] UNPIN: HUD hides (GetPinnedQuest = null, sentinel confirmed in WorldFlags)");
    }

    private void TestRepinAfterUnpin()
    {
        WorldFlags.Instance.ClearAll();
        QuestManager.Instance.PinQuest(BuiltInQuestId);
        QuestManager.Instance.UnpinQuest();
        QuestManager.Instance.PinQuest(BuiltInQuestId);

        var pinned = QuestManager.Instance.GetPinnedQuest();
        if (!Assert(pinned != null, "RE-PIN: GetPinnedQuest() should return quest after re-pinning"))
            return;
        Assert(pinned.Id == BuiltInQuestId, $"RE-PIN: expected '{BuiltInQuestId}', got '{pinned.Id}'");
        GD.Print($"[quest-pin] RE-PIN: quest '{pinned.Id}' shows on HUD again");
    }

    // Test 5: Completed pinned quest → returns null (no fallback active quests).
    private void TestCompletedQuestFallback()
    {
        WorldFlags.Instance.ClearAll();
        QuestManager.Instance.PinQuest(BuiltInQuestId);

        // Mark the final objective as completed.
        WorldFlags.Instance.SetFlag(CompletionFlag, true);

        var pinned = QuestManager.Instance.GetPinnedQuest();
        Assert(pinned == null,
            "COMPLETED: GetPinnedQuest() should return null when pinned quest is completed and no other active quests exist");
        GD.Print("[quest-pin] COMPLETED: GetPinnedQuest = null (pinned quest completed, no active fallback)");

        // Clean up the completion flag so it doesn't leak to other tests.
        WorldFlags.Instance.ClearFlag(CompletionFlag);
    }

    private async Task TestPersistedSentinel()
    {
        WorldFlags.Instance.ClearAll();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        // Set sentinel, serialize, clear, deserialize, then verify.
        WorldFlags.Instance.SetFlag(PinnedFlag, UnpinnedSentinel);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var serialized = WorldFlags.Instance.Serialize();
        WorldFlags.Instance.ClearAll();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        WorldFlags.Instance.Deserialize(serialized);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var pinned = QuestManager.Instance.GetPinnedQuest();
        Assert(pinned == null,
            $"PERSISTED SENTINEL: GetPinnedQuest() should return null after round-trip when flag is '{UnpinnedSentinel}'");
        GD.Print("[quest-pin] PERSISTED SENTINEL: HUD stays hidden after round-trip (GetPinnedQuest = null)");
    }
    private bool Assert(bool condition, string msg)
    {
        if (!condition)
        {
            GD.PushError($"[quest-pin] {msg}");
            _failures++;
            return false;
        }
        return true;
    }
}
