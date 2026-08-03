extends GPUParticles2D

@export var emission_rate: float = 5.0

enum ParticlePreset {
	DUST,
	LEAVES,
	STEAM,
	BUBBLES,
	SEAFOAM,
	FIREFLIES,
	NONE,
}

@export var preset: ParticlePreset = ParticlePreset.NONE

func _ready() -> void:
	if preset == ParticlePreset.NONE:
		emitting = false
		return
	_apply_preset(preset)
	amount = maxi(1, roundi(emission_rate))
	emitting = true

func _apply_preset(value: ParticlePreset) -> void:
	var material := process_material as ParticleProcessMaterial
	if material == null:
		material = ParticleProcessMaterial.new()
		process_material = material
	material.color = Color(1.0, 1.0, 1.0, 0.5)
	local_coords = false
	one_shot = false
	explosiveness = 0.0
	randomness = 0.3
	match value:
		ParticlePreset.DUST:
			lifetime = 4.0
			material.gravity = Vector3(0.0, 10.0, 0.0)
			material.spread = 180.0
		ParticlePreset.LEAVES:
			lifetime = 6.0
			material.gravity = Vector3(0.0, 20.0, 0.0)
			material.spread = 45.0
		ParticlePreset.STEAM:
			lifetime = 3.0
			material.gravity = Vector3(0.0, -15.0, 0.0)
			material.spread = 90.0
		ParticlePreset.BUBBLES:
			lifetime = 5.0
			material.gravity = Vector3(0.0, -30.0, 0.0)
			material.spread = 120.0
		ParticlePreset.SEAFOAM:
			lifetime = 4.0
			material.gravity = Vector3.ZERO
			material.spread = 60.0
		ParticlePreset.FIREFLIES:
			lifetime = 8.0
			material.gravity = Vector3.ZERO
			material.spread = 360.0

func burst(count: int = 10) -> void:
	amount = maxi(1, count)
	one_shot = true
	restart()
