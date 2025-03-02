```
res://
├── addons/                # Third-party addons like AsepriteWizard
├── assets/
│   ├── audio/
│   │   ├── music/         # BGM for different areas/combat
│   │   └── sfx/           # Sound effects
│   ├── fonts/             # Game fonts
│   ├── sprites/
│   │   ├── characters/    # Player and NPC sprites
│   │   ├── enemies/       # Enemy sprites for combat
│   │   ├── environment/   # World tiles, buildings, props
│   │   ├── items/         # Collectible items, power-ups
│   │   ├── ui/            # UI elements
│   │   └── effects/       # Visual effects, particles
│   └── shaders/           # Custom shader files
├── data/
│   ├── items.json         # Item definitions
│   ├── enemies.json       # Enemy stats and patterns
│   ├── dialogue.json      # NPC dialogue
│   └── levels.json        # Level/map data
├── scenes/
│   ├── autoload/          # Singleton scenes
│   ├── ui/                # Menu scenes, HUD components
│   ├── overworld/
│   │   ├── maps/          # Different overworld areas
│   │   ├── objects/       # Interactive objects
│   │   └── npcs/          # NPC scene instances
│   ├── combat/
│   │   ├── arena/         # Combat arena scenes
│   │   ├── bullets/       # Different bullet patterns
│   │   ├── enemies/       # Enemy instances for bullet hell
│   │   └── player/        # Player combat form
│   └── common/            # Shared scene components
├── scripts/
│   ├── core/              # Core game systems
│   ├── overworld/         # Overworld mechanics
│   ├── combat/            # Bullet hell mechanics
│   ├── ui/                # UI control scripts
│   └── utils/             # Helper functions, utilities
├── saves/                 # For save data
├── themes/                # UI themes
└── default_env.tres       # Default environment
```

mkdir -p \
  addons \
  assets/audio/music \
  assets/audio/sfx \
  assets/fonts \
  assets/sprites/characters \
  assets/sprites/enemies \
  assets/sprites/environment \
  assets/sprites/items \
  assets/sprites/ui \
  assets/sprites/effects \
  assets/shaders \
  data \
  scenes/autoload \
  scenes/ui \
  scenes/overworld/maps \
  scenes/overworld/objects \
  scenes/overworld/npcs \
  scenes/combat/arena \
  scenes/combat/bullets \
  scenes/combat/enemies \
  scenes/combat/player \
  scenes/common \
  scripts/core \
  scripts/overworld \
  scripts/combat \
  scripts/ui \
  scripts/utils \
  saves \
  themes
