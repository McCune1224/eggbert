using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

// Headless runtime test for CutsceneController.RunDialogBranch.
// Each scenario uses a fresh, single-purpose branch so the test never depends
// on node ordering or on the walker's post-jump behavior. Aborts on the
// first failed assertion so subsequent scenarios can't mask the real failure.

public partial class VerifyDialogBranch : SceneTree
{
	public override async void _Initialize()
	{
		// Autoloads attach to root after this MainLoop starts. Wait one frame.
		await ToSignal(Root, Window.SignalName.Ready);
		await ToSignal(this, SceneTree.SignalName.ProcessFrame);

		if (WorldFlags.Instance == null)
		{
			GD.PushError("WorldFlags autoload missing");
			this.Quit(1);
			return;
		}
		if (CutsceneController.Instance == null)
		{
			this.Quit(1);
			return;
		}

		if (!await ScenarioSetFlagsOnEnter()) return;
		if (!await ScenarioChoiceFlagYes()) return;
		if (!await ScenarioChoiceFlagNo()) return;
		if (!await ScenarioJumpTerminalYes()) return;
		if (!await ScenarioJumpTerminalNo()) return;
		if (!await ScenarioMissingTarget()) return;
		if (!await ScenarioConditionFilter()) return;

		CutsceneController.Instance.PromptChoicesOverride = null;
		GD.Print("[branch] ALL OK");
		this.Quit(0);
	}

	// ---- Scenario 1: a single node with SetFlagsOnEnter must set the flag.
	private async Task<bool> ScenarioSetFlagsOnEnter()
	{
		WorldFlags.Instance.ClearAll();
		var branch = new DialogBranch
		{
			Nodes = new Godot.Collections.Array<DialogNode>
			{
				new DialogNode
				{
					Id = "greeting",
					SetFlagsOnEnter = new[] { "greeted" },
				},
			},
		};
		CutsceneController.Instance.PromptChoicesOverride = null;
		if (!await RunAndWait(branch, "greeting")) return false;
		if (!AssertFlag("greeted", true, "SetFlagsOnEnter sets the flag when the node is entered"))
			return false;
		return true;
	}

	// ---- Scenario 2: pick the first response (index 0) — only its flag is set.
	private async Task<bool> ScenarioChoiceFlagYes()
	{
		WorldFlags.Instance.ClearAll();
		CutsceneController.Instance.PromptChoicesOverride = _ => 0;
		// Both responses have empty NextNodeId, so the branch ends after the choice.
		var branch = new DialogBranch
		{
			Nodes = new Godot.Collections.Array<DialogNode>
			{
				new DialogNode
				{
					Id = "question",
					Responses = new[]
					{
						new DialogResponse { Text = "Yes", NextNodeId = "", SetFlagOnSelect = "chose_yes" },
						new DialogResponse { Text = "No",  NextNodeId = "", SetFlagOnSelect = "chose_no"  },
					},
				},
			},
		};
		if (!await RunAndWait(branch, "question")) return false;
		if (!AssertFlag("chose_yes", true,  "Yes response flag is set when the first option is chosen")) return false;
		if (!AssertFlag("chose_no",  false, "No response flag is NOT set when the first option is chosen")) return false;
		if (CutsceneController.Instance.LastChoiceIndex != 0)
		{
			GD.PushError($"LastChoiceIndex should be 0, got {CutsceneController.Instance.LastChoiceIndex}");
			this.Quit(1);
			return false;
		}
		GD.Print("[branch] + LastChoiceIndex=0 after picking the first response");
		return true;
	}

	// ---- Scenario 3: pick the second response (index 1).
	private async Task<bool> ScenarioChoiceFlagNo()
	{
		WorldFlags.Instance.ClearAll();
		CutsceneController.Instance.PromptChoicesOverride = _ => 1;
		var branch = new DialogBranch
		{
			Nodes = new Godot.Collections.Array<DialogNode>
			{
				new DialogNode
				{
					Id = "question",
					Responses = new[]
					{
						new DialogResponse { Text = "Yes", NextNodeId = "", SetFlagOnSelect = "chose_yes" },
						new DialogResponse { Text = "No",  NextNodeId = "", SetFlagOnSelect = "chose_no"  },
					},
				},
			},
		};
		if (!await RunAndWait(branch, "question")) return false;
		if (!AssertFlag("chose_no",  true,  "No response flag is set when the second option is chosen")) return false;
		if (!AssertFlag("chose_yes", false, "Yes response flag is NOT set when the second option is chosen")) return false;
		if (CutsceneController.Instance.LastChoiceIndex != 1)
		{
			GD.PushError($"LastChoiceIndex should be 1, got {CutsceneController.Instance.LastChoiceIndex}");
			this.Quit(1);
			return false;
		}
		GD.Print("[branch] + LastChoiceIndex=1 after picking the second response");
		return true;
	}

	// ---- Scenario 4: jumping to yes_path must NOT walk into no_path (sibling fall-through).
	// Branch: [greeting, question, yes_path, no_path]
	//   greeting  -> SetFlagsOnEnter: "greeted"  (linear, no responses)
	//   question  -> 2 responses, jumping to yes_path / no_path with SetFlagOnSelect
	//   yes_path  -> SetFlagsOnEnter: "yes_arrived"
	//   no_path   -> SetFlagsOnEnter: "no_arrived"  (must stay FALSE)
	private async Task<bool> ScenarioJumpTerminalYes()
	{
		WorldFlags.Instance.ClearAll();
		CutsceneController.Instance.PromptChoicesOverride = _ => 0;  // choose "Yes" -> yes_path
		var branch = BuildJumpBranch();
		if (!await RunAndWait(branch, "greeting")) return false;
		if (!AssertFlag("greeted",     true,  "greeting SetFlagsOnEnter fires when the branch starts there")) return false;
		if (!AssertFlag("chose_yes",   true,  "Yes response flag is set on the chosen path")) return false;
		if (!AssertFlag("yes_arrived", true,  "yes_path SetFlagsOnEnter fires after the jump")) return false;
		if (!AssertFlag("chose_no",    false, "No response flag is NOT set when Yes was chosen")) return false;
		if (!AssertFlag("no_arrived",  false, "no_path is NOT walked into after jumping to yes_path")) return false;
		return true;
	}

	// ---- Scenario 5: same branch, pick No — yes_path must NOT fire.
	private async Task<bool> ScenarioJumpTerminalNo()
	{
		WorldFlags.Instance.ClearAll();
		CutsceneController.Instance.PromptChoicesOverride = _ => 1;  // choose "No" -> no_path
		var branch = BuildJumpBranch();
		if (!await RunAndWait(branch, "greeting")) return false;
		if (!AssertFlag("greeted",     true,  "greeting SetFlagsOnEnter fires when the branch starts there")) return false;
		if (!AssertFlag("chose_no",    true,  "No response flag is set on the chosen path")) return false;
		if (!AssertFlag("no_arrived",  true,  "no_path SetFlagsOnEnter fires after the jump")) return false;
		if (!AssertFlag("chose_yes",   false, "Yes response flag is NOT set when No was chosen")) return false;
		if (!AssertFlag("yes_arrived", false, "yes_path is NOT walked into after jumping to no_path")) return false;
		return true;
	}

	private static DialogBranch BuildJumpBranch() => new DialogBranch
	{
		Nodes = new Godot.Collections.Array<DialogNode>
		{
			new DialogNode { Id = "greeting", SetFlagsOnEnter = new[] { "greeted" } },
			new DialogNode
			{
				Id = "question",
				Responses = new[]
				{
					new DialogResponse { Text = "Yes", NextNodeId = "yes_path", SetFlagOnSelect = "chose_yes" },
					new DialogResponse { Text = "No",  NextNodeId = "no_path",  SetFlagOnSelect = "chose_no"  },
				},
			},
			new DialogNode { Id = "yes_path", SetFlagsOnEnter = new[] { "yes_arrived" } },
			new DialogNode { Id = "no_path",  SetFlagsOnEnter = new[] { "no_arrived"  } },
		},
	};
	// ---- Scenario 4: a response that points to a non-existent target ends cleanly.
	private async Task<bool> ScenarioMissingTarget()
	{
		WorldFlags.Instance.ClearAll();
		CutsceneController.Instance.PromptChoicesOverride = _ => 0;
		var branch = new DialogBranch
		{
			Nodes = new Godot.Collections.Array<DialogNode>
			{
				new DialogNode
				{
					Id = "start",
					Responses = new[]
					{
						new DialogResponse { Text = "Wander off", NextNodeId = "nowhere", SetFlagOnSelect = "wandered" },
					},
				},
			},
		};
		if (!await RunAndWait(branch, "start")) return false;
		if (!AssertFlag("wandered", true, "Response flag is set even when NextNodeId is missing")) return false;
		return true;
	}

	// ---- Scenario 5: condition filtering — only the met-condition response is presented.
	private async Task<bool> ScenarioConditionFilter()
	{
		WorldFlags.Instance.ClearAll();
		// WorldFlags is empty; "absent_flag" is not set, so the FlagSet response must be filtered.
		var capturedChoices = new List<string>();
		CutsceneController.Instance.PromptChoicesOverride = choices =>
		{
			capturedChoices.Clear();
			capturedChoices.AddRange(choices);
			return 0;
		};
		var branch = new DialogBranch
		{
			Nodes = new Godot.Collections.Array<DialogNode>
			{
				new DialogNode
				{
					Id = "ask",
					Responses = new[]
					{
						new DialogResponse
						{
							Text = "Conditional (only when flag set)",
							NextNodeId = "",
							Condition = new CutsceneCondition { Type = ConditionType.FlagSet, FlagKey = "absent_flag" },
						},
						new DialogResponse { Text = "Always available", NextNodeId = "" },
					},
				},
			},
		};
		if (!await RunAndWait(branch, "ask")) return false;
		if (capturedChoices.Count != 1 || capturedChoices[0] != "Always available")
		{
			GD.PushError($"Conditional response should be filtered out. Got: [{string.Join(", ", capturedChoices)}]");
			this.Quit(1);
			return false;
		}
		GD.Print("[branch] + Response with unmet Condition is filtered out of the choice list");
		return true;
	}

	// ---- helpers --------------------------------------------------------------

	private async Task<bool> RunAndWait(DialogBranch branch, string startNodeId)
	{
		var toSignal = CutsceneController.Instance.ToSignal(
			CutsceneController.Instance,
			CutsceneController.SignalName.DialogBranchFinished);
		_ = CutsceneController.Instance.RunDialogBranch(branch, startNodeId);
		await toSignal;
		return true;
	}

	private static bool AssertFlag(string key, bool expected, string label)
	{
		var actual = WorldFlags.Instance.HasFlag(key);
		if (actual != expected)
		{
			GD.PushError($"{label}: expected '{key}'={expected}, got {actual}");
			((SceneTree)Engine.GetMainLoop()).Quit(1);
			return false;
		}
		GD.Print($"[branch] + {label}");
		return true;
	}
}
