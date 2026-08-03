class_name CutsceneStep
extends Resource

enum StepType { DIALOG, MOVE_NPC, MOVE_PLAYER, FACE_DIRECTION, PLAY_ANIMATION, CAMERA_MOVE, WAIT, SET_FLAG, FADE, PROMPT_CHOICE, LOCK_PLAYER, UNLOCK_PLAYER, STOP, DIALOG_BRANCH }

@export var type: StepType = StepType.DIALOG
@export var dialog_lines: Array[String] = []
@export var dialog_voice: DialogVoiceResource
@export var target_node: NodePath
@export var move_target := Vector2.ZERO
@export var move_duration := 0.5
@export var animation_node: NodePath
@export var animation_name := ""
@export var camera_offset := Vector2.ZERO
@export var wait_seconds := 0.0
@export var set_flag_key := ""
@export var set_flag_value: Variant = true
@export var fade_direction := "out"
@export var choice_options: PackedStringArray = []
@export var choice_flags: PackedStringArray = []
@export var dialog_branch: DialogBranch
@export var condition: CutsceneCondition
