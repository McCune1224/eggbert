# NPC Gossip Chain Pattern

## Flag Convention

```
gossip_<npcA>_about_<npcB>
```

Set when NPC A mentions NPC B in dialog. NPC B checks this flag in their own dialog to respond.

## How it works

1. NPC A's cutscene sets `gossip_A_about_B = true` via a SetFlag step
2. NPC B's cutscene has a conditional step checking `gossip_A_about_B`
3. When player talks to B after hearing from A about them, B has new dialog

## Example

Waffles mentions he saw Croissant sneaking around.
Croissant, when asked, denies it — but only if Waffles already told the player.

### Waffles cutscene
```
Step 1: SayDialog "I saw Croissant sneaking around the kitchen last night."
Step 2: SetFlag gossip_waffles_about_croissant = true
Step 3: SayDialog "Don't tell him I said anything."
```

### Croissant cutscene
```
Step 1 (Condition: FlagNotSet gossip_waffles_about_croissant):
        "Bonjour. I have done nothing wrong."
Step 2 (Condition: FlagSet gossip_waffles_about_croissant):
        "Waffles told you? Zat fool. I was just... inspecting ze kitchen."
        "For... hygiene purposes. Yes."
Step 3: SayDialog "Now if you'll excuse me."
```

## Implementation

- No engine changes needed — uses existing CutsceneResource + CutsceneCondition + SetFlag
- Create `.tres` cutscene resource files for NPCs that currently use plain `DialogLines`
- NPC's CutsceneTrigger.Cutscene points to the resource instead of using DialogLines
