// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Scripts;
using Godot;

namespace Chromonia.Scenes;

public partial class Arrow : Sprite2D
{
    private const float PulsateScale = 1.25f;
    private const float PulsateDuration = 0.25f;
    private const float PulsateMinGlow = 0.9f;
    private const float PulsateMaxGlow = 1.1f;

    private float _timePassed;
    private Vector2 _baseScale;
    private float _targetRotation;
    private const float RotationSpeed = Mathf.Pi / 0.25f; // Radians per second to complete rotation in 0.25s

    public Energy CurrentEnergy { get; } = new();

    public void Cycle()
    {
        CurrentEnergy.Cycle();
        _timePassed = 0f; // Reset the pulse phase, just like restarting the tween
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction == Vector2.Zero) return;

        float newTargetRotation = direction.Angle() + Mathf.Pi / 2f;

        if (Mathf.IsEqualApprox(_targetRotation, newTargetRotation)) return;

        _targetRotation = newTargetRotation;

        float currentRotation = Rotation;
        float angleDiff = Mathf.AngleDifference(currentRotation, _targetRotation);

        // We set the target to the continuous unwrapped angle to make MoveToward easier
        _targetRotation = currentRotation + angleDiff;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Handle Pulsate
        _timePassed += (float)delta;
        float progress = (-Mathf.Cos(_timePassed * Mathf.Pi / PulsateDuration) + 1.0f) / 2.0f;

        Scale = _baseScale * Mathf.Lerp(1.0f, PulsateScale, progress);

        var minColor = CurrentEnergy.Marker * PulsateMinGlow;
        var maxColor = CurrentEnergy.Line * PulsateMaxGlow;
        SelfModulate = minColor.Lerp(maxColor, progress);

        // Handle Rotation
        if (float.IsNaN(_targetRotation)) return;
        if (!Mathf.IsEqualApprox(Rotation, _targetRotation))
            Rotation = Mathf.MoveToward(Rotation, _targetRotation, RotationSpeed * (float)delta);
    }

    public override void _Ready()
    {
        base._Ready();
        _baseScale = Scale;
        CurrentEnergy.CurrentTint = Energy.Tint.A;
        SelfModulate = CurrentEnergy.Marker * PulsateMinGlow;
        _targetRotation = Rotation;
    }
}