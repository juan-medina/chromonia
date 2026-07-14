// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Main;
using Godot;

namespace Chromonia.Enemies;

public partial class BlobEnemy : RigidBody2D
{
    [Export] public Energy Energy { get; set; }
    [Export] public float Radius { get; set; }
    [Export] private CollisionShape2D _collisionShape = null!;

    public Energy BaseEnergy { get; private set; }
    public Energy CurrentEnergy { get; private set; }
    public bool IsDissolving { get; private set; }
    private Color DisplayColor { get; set; }

    public const string GroupName = "Blobs";
    public const float DissolveTime = 0.5F;

    public void SetMerged(bool isMerged) => CurrentEnergy = isMerged ? Energy.Combined : BaseEnergy;

    public override void _Ready()
    {
        BaseEnergy = Energy;
        CurrentEnergy = Energy;
        DisplayColor = CurrentEnergy.Fill();

        ((CircleShape2D)_collisionShape.Shape).Radius = Radius;

        AddToGroup(GroupName);
    }

    public override void _Process(double delta)
    {
        if (IsDissolving) return;
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
        float drawRadius = Radius * 1.5f;
        var rect = new Rect2(-drawRadius, -drawRadius, drawRadius * 2, drawRadius * 2);
        DrawRect(rect, DisplayColor);
    }
}
