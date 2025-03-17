using Godot;
using System;

public partial class CombatOatmeal : Area2D
{
    //TODO: Look into other optimizations for bullet efficiency
    private AnimationPlayer _animationPlayer;
    private bool _isFiring = false;
    private float _attackCooldown = 2.0f; // Seconds between attacks
    private float _timeSinceLastAttack = 0.0f;

    public override void _Ready()
    {
        GD.Print("WHATS GOOD ITS OAT TIME");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void _Process(double delta)
    {
        _timeSinceLastAttack += (float)delta;

        if (!_isFiring && _timeSinceLastAttack >= _attackCooldown)
        {
            Attack1();
            _timeSinceLastAttack = 0;
        }
    }

    public void Fire()
    {
        Vector2 target = CombatTargeter.GetPlayerPosition();
        GD.Print("Target is at ", target);
        _animationPlayer.Play("default");
    }

    // Fire 3-5 bullets in a spread pattern towards the player
    public void Attack1()
    {
        if (!_isFiring)
        {
            _animationPlayer.Play("default");
            _isFiring = true;
            Random rand = new Random();
            int numBullets = rand.Next(3, 6);
            float spreadAngle = 30.0f; // 30 degree spread
            float angleStep = spreadAngle / (numBullets > 1 ? numBullets - 1 : 1);

            Vector2 targetPos = CombatTargeter.GetPlayerPosition();
            Vector2 directionToPlayer = (targetPos - GlobalPosition).Normalized();
            float baseAngle = Mathf.Atan2(directionToPlayer.Y, directionToPlayer.X);
            float startAngle = baseAngle - Mathf.DegToRad(spreadAngle / 2);

            for (int i = 0; i < numBullets; i++)
            {

                RedBullet bullet = ResourceLoader.Load<PackedScene>("res://scenes/combat/bullets/RedBullet.tscn").Instantiate<RedBullet>();
                // RedBullet bullet = _bulletPool[i];
                // Remove from parent if it's already in the tree
                if (bullet.GetParent() != null)
                    bullet.GetParent().RemoveChild(bullet);

                // Position bullet at the enemy
                bullet.GlobalPosition = GlobalPosition;

                // Calculate bullet direction based on spread
                float bulletAngle = startAngle + Mathf.DegToRad(angleStep * i);
                Vector2 direction = new Vector2(
                    Mathf.Cos(bulletAngle),
                    Mathf.Sin(bulletAngle)
                );

                // Set the bullet's direction using its own method
                bullet.SetDirection(direction, 250.0f); // Speed increased for better effect
                GD.Print("Firing bullet at direction: ", direction);

                // Add to scene tree
                GetParent().AddChild(bullet);

                // Reset the bullet's alive time (if needed)
                bullet.ResetLifetime();
            }
            _isFiring = false;
        }
    }
}
