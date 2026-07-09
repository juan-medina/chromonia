// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Main;
using Godot;

namespace Chromonia.Enemies;

public partial class BlobEnemy : RigidBody2D
{
    public Energy BaseEnergy { get; private set; }
    public Energy CurrentEnergy { get; private set; }
    public float Radius { get; private set; }
    public bool IsDissolving { get; private set; }
    private Color DisplayColor { get; set; }

    public const string GroupName = "Blobs";
    public const float DissolveTime = 0.5F;

    public BlobEnemy(Energy energy, float radius)
    {
        BaseEnergy = energy;
        CurrentEnergy = energy;
        Radius = radius;

        AddToGroup(GroupName);

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
            Shape = new CircleShape2D { Radius = Radius }
        };
        AddChild(shape);
    }

    public BlobEnemy()
    {
    }


    public void SetMerged(bool isMerged) => CurrentEnergy = isMerged ? Energy.Combined : BaseEnergy;


    public override void _Ready()
    {
        DisplayColor = CurrentEnergy.Fill();

        var shader = ResourceLoader.Load<Shader>("res://Enemies/blob_soft.gdshader");
        Material = new ShaderMaterial { Shader = shader };
    }

    public override void _Process(double delta)
    {
        if (IsDissolving) return;
        // Smoothly transition the display color towards the target logical color
        DisplayColor = DisplayColor.Lerp(CurrentEnergy.Fill(), (float)delta * 10.0f);
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        RemoveFromGroup(GroupName);
        base._ExitTree();
    }

    public void Dissolve()
    {
        if (IsDissolving) return;
        IsDissolving = true;

        SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
        SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);

        CreateTween().TweenProperty(this, "scale", Vector2.Zero, DissolveTime)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
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