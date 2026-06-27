// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Scripts;

public partial class BlobEnemy : RigidBody2D
{
    public Energy BlobEnergy { get; } = new();
    public Energy.Tint BaseTint { get; private set; }
    public float Radius { get; private set; }
    public bool IsDissolving { get; private set; }


    public BlobEnemy(Energy.Tint tint, float radius)
    {
        BaseTint = tint;
        BlobEnergy.CurrentTint = tint;
        Radius = radius;

        AddToGroup("Blobs");

        // Collision layers:
        // Layer 2 = Blob layer (what I am)
        // Mask 1 = Default environment layer (what I bounce against)
        CollisionLayer = 2;
        CollisionMask = 1;

        GravityScale = 0;
        LinearDampMode = DampMode.Replace;
        LinearDamp = 0;
        AngularDampMode = DampMode.Replace;
        AngularDamp = 0;

        PhysicsMaterialOverride = new PhysicsMaterial
        {
            Bounce = 1.0f,
            Friction = 0.0f
        };

        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = this.Radius }
        };
        AddChild(shape);
    }

    public Color DisplayColor { get; set; }

    public BlobEnemy()
    {
    }

    public override void _Ready()
    {
        DisplayColor = BlobEnergy.Fill;

        var shader = ResourceLoader.Load<Shader>("res://shaders/blob_soft.gdshader");
        Material = new ShaderMaterial { Shader = shader };
    }

    public override void _Process(double delta)
    {
        if (IsDissolving) return;
        // Smoothly transition the display color towards the target logical color
        DisplayColor = DisplayColor.Lerp(BlobEnergy.Fill, (float)delta * 10.0f);
        QueueRedraw();
    }

    public void Dissolve()
    {
        if (IsDissolving) return;
        IsDissolving = true;

        RemoveFromGroup("Blobs");

        SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);

        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.5f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        
        if (GetParent() is not BlobCluster)
        {
            tween.TweenCallback(Callable.From(QueueFree));
        }
    }

    public override void _Draw()
    {
        // Draw the blob slightly larger than its collision radius to account for the fuzzy edge
        float drawRadius = Radius * 1.5f;
        var rect = new Rect2(-drawRadius, -drawRadius, drawRadius * 2, drawRadius * 2);

        // Draw a solid rect. The blob_soft.gdshader will turn it into a soft radial gradient!
        DrawRect(rect, DisplayColor);
    }
}