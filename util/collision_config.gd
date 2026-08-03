class_name CollisionConfig
extends RefCounted

## Physics layers used by gameplay scenes. Values are bitmasks, not indices.
const PLAYER_LAYER: int = 1 << 0
const WALLS_LAYER: int = 1 << 1
const NPC_LAYER: int = 1 << 2
const BULLET_LAYER: int = 1 << 3
const INTERACTABLE_LAYER: int = 1 << 4
const ENEMY_LAYER: int = 1 << 5
const TRIGGER_AREA_LAYER: int = 1 << 6
const PLAYER_HITBOX_LAYER: int = 1 << 7
const ENEMY_HITBOX_LAYER: int = 1 << 8
const ITEM_LAYER: int = 1 << 9
