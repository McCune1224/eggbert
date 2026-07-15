# Eggbert — Feature Idea Bucket

Not a roadmap. Not phases. Just a pile of stuff that would be fun. Pull from here whenever you want something to build.

---

## Dialog & Text

### Narrator Lines
An Undertale-style narrator that comments on what you interact with. A second dialog speaker that's always there — the world's voice. Could be a separate speaker in dialog data (`Narrator: The egg sits there. It's an egg.`).

### Conditional Dialog Branches
NPCs that say different things based on WorldFlags: items you're holding, bosses you've beaten, how many times you've talked to them. Already partially supported by WorldFlags branching — expand it to more NPCs.

### NPC Gossip Chains
Talk to NPC A → they mention something about NPC B. Talk to NPC B → they reference what A said. Makes the prison feel like a real social space where word travels.

### Readable Objects
Signs, posters, books, scribbled notes on walls. Simple interact-triggered dialog lines. Cheap worldbuilding. A book in the cell that's "Eggs Isle: A History" — gives Egguardo's quiz answers.

### Phone Calls
Payphone in the rec room. One call home to Eggatha. Changes dialog depending on where you are in the story.

### Tattle / Check Action
Undertale's "Check" — a button that shows flavor text for any NPC or object. Could just be an extra dialog line per entity. Low effort, high personality.

### Dialog Skipping That Feels Good
Hold-to-fast-forward (already exists). Tap-to-advance to next line with a brief pause so you don't accidentally skip a full page.

---

## Overworld Puzzles

### Weighted Pressure Plate
Hold it down with a PushBlock OR stand on it yourself. Opens door only while depressed. Requires blocking it with something.

### Color-Coded Locks
Three doors, three keys of matching colors. Keys are scattered through the zone. Find all three, open all three. Bread-and-butter dungeon design.

### Conveyor Belt
Tiles that push the player in a set direction. Can be used for maze sections, timing puzzles, or pushing blocks along a track.

### Teleport Pads
Paired warp tiles. Step on one → appear at the other. Can chain them for puzzle routing.

### Moving Platform
A platform that follows a path (AnimationPlayer or Path2D). Player rides it to reach otherwise inaccessible areas. Jump off timing.

### Dark Room + Flashlight
A pitch-black room. Player has a limited-radius light. Navigate by feel. Could have things that only appear in the dark.

### Pipe Maze
Enter pipe → appear at exit. Underground shortcut network. Simple but satisfying to find all the connections.

### Directional Block
PushBlock that only moves in the direction you hit it. Once it stops, push again from a new angle. Classic Sokobon-lite.

### Fake Walls
A wall that looks solid but you can walk through. Hidden room behind it. Classic secret-hunting.

### Floor Spikes / Hazards
Tiles that damage the player on contact. Telegraph with animation. Learn the safe path. Could have timed retraction.

### Timed Pressure Plate Sequence
Three plates, one door. Must step on them in order within a time window. Miss the window → reset.

### Laser / Light Beam Puzzles
A light beam from a source. Place mirrors (pushable?) to redirect it to a sensor. Opens door.

---

## NPC Behaviors

### Patrolling NPC
Walks a set path between 2-3 waypoints. Stops and faces the player when interacted with. Resumes patrol after dialog. Makes halls feel lived in.

### Fleeing NPC
Runs away when the player gets close. Can be chased down for a unique dialog line or item. Scared of the player, or just shy.

### Sleeping NPC
Asleep in a bed/bench. Can be woken up (grumpy dialog) or snuck past. Maybe has a key visible but you can't take it without waking them.

### Merchant NPC
Sells items in exchange for ??? (if we add currency later) or barters for other items. Could be simple: "Give me a hardboiled egg and I'll give you this key."

### Quest-Giver NPC
Gives a simple fetch/kill/knowledge task. Tracks completion via WorldFlag. "Bring me three eggs from the kitchen" — simple, satisfying.

### Rumor Mill NPC
Repeats rumors about the prison. Dialog changes each time you talk to them (pulls from a rotating pool). Some rumors are true, some are false, some are just funny.

### The Complainer
Same dialog every time, but slightly more exaggerated. Each interaction adds one more complaint. Starts with "The food here is bad" → "The food here is bad AND the beds are lumpy" → etc.

---

## World Feel & Atmosphere

### Zone Transition Stingers
Brief musical sting when entering a new zone. Could be a single chord, a jingle, or a sound effect. Sets the mood for the area you're entering.

### Flickering Lights
Prison lights that buzz and flicker. AnimationPlayer on a Light2D + ambient buzz SFX loop. Cheap, huge atmosphere.

### Hanging Signs
Signs that sway gently (sine-wave animation). Visual interest in empty corridors.

### Ambient Particles Per Zone
- **Factory**: Dust motes in beams of light
- **Courtyard**: Falling leaves, drifting clouds
- **Kitchen**: Rising steam from vents
- **Sewers**: Dripping water, rising bubbles
- **Prison**: Dust, faint motes
- **Beach**: Seafoam, drifting sand particles

### Weather Events
Occasional rain that passes through. Window in the prison shows it. No gameplay impact, just mood.

### Background Animals / Critters
Rats that scuttle across the floor (animated sprite, loops). Birds on the courtyard walls. Cockroach in the cell that you can talk to for some reason.

### Echoey Reverb Zones
Certain areas (sewers, empty hallways) apply a reverb effect to footsteps and dialog. Simple AudioBus effect.

### Footstep Sounds
Different floor types → different footstep sounds (stone, metal, grass, water). Minor but noticeable.

### Door Sounds
Each door type gets an open/close sound. Key doors get a jingle. The little audio feedback loop is satisfying.

### Wind Howl
In outdoor areas (courtyard, beach), a low wind ambience that swells and fades. Helps sell the prison-island isolation.

---

## Inventory & Items

### Keyring UI
All keys in one "keyring" tab in the inventory. Don't take up normal inventory space — keys are tracked by WorldFlags.

### Key Item Descriptions That Change
Use Read action on a key item. First time: "A rusty key." After using it: "A rusty key. Served its purpose." Small detail, big feel.

### Equipment Stat Preview
When hovering over equipment in inventory, show what stats will change. "DEF +3, HP +10" — shows current vs projected.

### Hidden Items
Items that are only visible/reachable after doing something else. A key hidden behind a bookshelf that only moves after pulling the right switch.

---

## Secrets & Exploration (Linear-ish)

### Hidden Paths
Off the main path, a slightly darker tile that's actually a passage. Not a dead end — leads to a small reward room. Undertale-style "hidden route."

### Breakable Walls
Wall cracks that you can interact with. Breaks open to reveal a small alcove with an item. Requires no special tool — just interact.

### Post-Dialog Rewards
NPC gives you something after exhausting their dialog tree. Encourages talking to everyone.

### Revisiting Old Areas
Return to a previous zone after a story beat → something has changed. New NPC, new item, new dialog. Makes the world feel dynamic.

### Sequence Breaks
Players who do things out of order get unique dialog acknowledging it. "You weren't supposed to be here yet but okay."

### The One Obvious Secret
A door that's clearly a secret but you can't open it yet. Drive the player crazy. When they finally find the key/switch it's incredibly satisfying.

---

## Quality of Life

### Speed Options on First Boot
A "Text Speed" picker on first launch: Fast / Medium / Slow. Saves to settings.

### Interaction Prompt Customization
Show/hide the "Press E" prompt. Some players like it, some find it intrusive.

### Persistent Dialog Log
Scroll back through the last N lines of dialog during a conversation. For when you're not mashing and actually want to read.

### Save Icon
Brief animated icon when the game autosaves. Subtle, reassuring.

---

## Long Shot / Might Be Too Much Work

### Rhythm Game Section
The Great Toast God fight as a simple rhythm game? Bullet patterns timed to a beat. Probably scope creep but it's a cool image.

### Multiple Outfits
Eggbert finds different hats/accessories in the world. Purely cosmetic, shown on the sprite. Gotta collect 'em all.

### Photo Mode
Pause and hide UI. Just look at the pretty pixel art. Maybe a subtle filter.

### Fishing Mini-Game
You're on an island. There's water. Fishing is the most natural mini-game in existence. Could be a simple timing press.

### Recipe System
Combine two items to make a new one. Hardboiled + ??? = ???. Unnecessary but fun.
