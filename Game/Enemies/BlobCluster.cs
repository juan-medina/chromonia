// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Main;
using Godot;

namespace Chromonia.Enemies;

public partial class BlobCluster : Node2D
{
    [Export] private RigidBody2D _core = null!;
    [Export] public float Speed { get; set; }
    [Export] public Energy Energy { get; set; }

    private bool IsDissolving { get; set; }

    private const int MinBlobs = 4;
    private const int MaxBlobs = 6;
    private const int MinRadius = 10;
    private const int MaxRadius = 20;

    private const float WobbleFrequency = 50f;
    private const float MaxWobbleAngle = 3.0f;

    private FastNoiseLite _noise = null!;
    private double _timePassed;

    public override void _Ready()
    {
        _noise = new FastNoiseLite { Seed = (int)GD.Randi(), Frequency = 0.5f };

        float angle = (float)GD.RandRange(0, Mathf.Tau);
        _core.LinearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Speed;

        int subBlobs = GD.RandRange(MinBlobs, MaxBlobs);
        for (int i = 0; i < subBlobs; i++)
        {
            float radius = GD.RandRange(MinRadius, MaxRadius);
            var blob = new BlobEnemy(Energy, radius);

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
                DisableCollision = true
            };
            AddChild(spring);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDissolving) return;

        _timePassed += delta;

        float wobbleRate = _noise.GetNoise1D((float)_timePassed * WobbleFrequency) * MaxWobbleAngle;
        _core.LinearVelocity = _core.LinearVelocity.Rotated(wobbleRate * (float)delta);

        if (_core.LinearVelocity.LengthSquared() > 0.1f)
            _core.LinearVelocity = _core.LinearVelocity.Normalized() * Speed;
    }

    public bool Dissolve()
    {
        if (IsDissolving) return false;
        IsDissolving = true;

        SetPhysicsProcess(false);
        _core.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        _core.SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);

        foreach (var child in GetChildren())
            if (child is BlobEnemy blob)
                blob.Dissolve();

        var tween = CreateTween();
        tween.TweenInterval(BlobEnemy.DissolveTime);
        tween.TweenCallback(Callable.From(QueueFree));
        return true;
    }
}
