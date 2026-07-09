// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Main;
using Godot;

namespace Chromonia.Enemies;

public partial class BlobCluster : Node2D
{
    private RigidBody2D _core = null!;
    private float _speed;
    private Energy _energy;
    private bool IsDissolving { get; set; }


    private const int MinBlobs = 4;
    private const int MaxBlobs = 6;
    private const int MinRadius = 10;
    private const int MaxRadius = 20;

    // Noise to create erratic movement
    private FastNoiseLite _noise = null!;
    private double _timePassed;

    public BlobCluster()
    {
    }

    public BlobCluster(Energy energy, float speed)
    {
        _energy = energy;
        _speed = speed;
        _noise = new FastNoiseLite { Seed = (int)GD.Randi(), Frequency = 0.5f };
    }

    public override void _Ready()
    {
        // Create the invisible Core physics body that drives the cluster
        _core = new RigidBody2D
        {
            GravityScale = 0,
            LinearDampMode = RigidBody2D.DampMode.Replace,
            LinearDamp = 0.5f,
            CollisionLayer = 2,
            CollisionMask = 1,
            PhysicsMaterialOverride = new PhysicsMaterial { Bounce = 1.0f, Friction = 0.0f }
        };

        // The core needs a collision shape to bounce off walls. We keep it small.
        _core.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 10f } });
        AddChild(_core);

        float angle = (float)GD.RandRange(0, Mathf.Tau);
        _core.LinearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _speed;

        int subBlobs = GD.RandRange(MinBlobs, MaxBlobs);
        for (int i = 0; i < subBlobs; i++)
        {
            float radius = GD.RandRange(MinRadius, MaxRadius);
            var blob = new BlobEnemy(_energy, radius);

            // Random offset so they don't spawn exactly inside each other
            float spawnAngle = (float)GD.RandRange(0, Mathf.Tau);
            blob.Position = new Vector2(Mathf.Cos(spawnAngle), Mathf.Sin(spawnAngle)) * 30f;

            AddChild(blob);

            var spring = new DampedSpringJoint2D
            {
                NodeA = _core.GetPath(),
                NodeB = blob.GetPath(),
                Length = 5f,
                RestLength = 0f,
                Stiffness = 150f,
                Damping = 2f,
                // Sub-blobs in the same cluster won't physically collide with the core
                // (though they might collide with each other depending on physics layers)
                DisableCollision = true
            };
            AddChild(spring);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDissolving) return;

        _timePassed += delta;

        // Use noise to smoothly steer the cluster instead of pushing it
        // The noise value is between -1 and 1. We multiply by a turning speed (e.g., 3.0 radians/sec).
        float turnRate = _noise.GetNoise1D((float)_timePassed * 50f) * 3.0f;

        _core.LinearVelocity = _core.LinearVelocity.Rotated(turnRate * (float)delta);

        // Strictly enforce the exact speed at all times so it never slows down or speeds up
        if (_core.LinearVelocity.LengthSquared() > 0.1f)
            _core.LinearVelocity = _core.LinearVelocity.Normalized() * _speed;
    }

    public void Dissolve()
    {
        if (IsDissolving) return;
        IsDissolving = true;

        SetPhysicsProcess(false);
        _core.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        _core.SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);

        foreach (var child in GetChildren())
            if (child is BlobEnemy blob)
                blob.Dissolve();

        var tween = CreateTween();
        tween.TweenInterval(0.5f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}