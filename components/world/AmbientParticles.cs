using Godot;

/// <summary>
/// Zone-specific ambient particle system (dust, leaves, steam, bubbles, etc.)
/// Configures a GpuParticles2D with a ParticleProcessMaterial per preset.
/// </summary>
public partial class AmbientParticles : GpuParticles2D
{
    [Export] public float EmissionRate { get; set; } = 5f;

    public enum ParticlePreset { Dust, Leaves, Steam, Bubbles, Seafoam, Fireflies, None }

    [Export] public ParticlePreset Preset { get; set; } = ParticlePreset.None;

    public override void _Ready()
    {
        if (Preset == ParticlePreset.None)
        {
            Emitting = false;
            return;
        }

        ApplyPreset(Preset);
        Amount = (int)EmissionRate;
        Emitting = true;
    }

    private void ApplyPreset(ParticlePreset preset)
    {
        var material = ProcessMaterial as ParticleProcessMaterial;
        if (material == null)
        {
            material = new ParticleProcessMaterial();
            ProcessMaterial = material;
        }

        material.Color = Colors.White with { A = 0.5f };
        LocalCoords = false;
        OneShot = false;
        Explosiveness = 0f;
        Randomness = 0.3f;

        switch (preset)
        {
            case ParticlePreset.Dust:
                Lifetime = 4f;
                material.Gravity = new Vector3(0, 10, 0);
                material.Spread = 180f;
                break;

            case ParticlePreset.Leaves:
                Lifetime = 6f;
                material.Gravity = new Vector3(0, 20, 0);
                material.Spread = 45f;
                break;

            case ParticlePreset.Steam:
                Lifetime = 3f;
                material.Gravity = new Vector3(0, -15, 0);
                material.Spread = 90f;
                break;

            case ParticlePreset.Bubbles:
                Lifetime = 5f;
                material.Gravity = new Vector3(0, -30, 0);
                material.Spread = 120f;
                break;

            case ParticlePreset.Seafoam:
                Lifetime = 4f;
                material.Gravity = Vector3.Zero;
                material.Spread = 60f;
                break;

            case ParticlePreset.Fireflies:
                Lifetime = 8f;
                material.Gravity = Vector3.Zero;
                material.Spread = 360f;
                break;
        }
    }

    // Helper: emit a short burst
    public void Burst(int count = 10)
    {
        Amount = count;
        OneShot = true;
        Restart();
    }
}
