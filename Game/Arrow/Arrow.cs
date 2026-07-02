// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Main;
using Godot;

namespace Chromonia.Arrow;

public enum ArrowState
{
    Normal,
    Stunned,
    Immune
}

public partial class Arrow : Sprite2D
{
    [Export] private Material _stunnedMaterial = null!;
    [Export] private Material _immuneMaterial = null!;

    private const float PulsateMinGlow = 0.9f;
    private const float RotationTime = 0.25f;
    private const float RotationSpeed = Mathf.Pi / RotationTime; // Radians per second
    private const float ImmuneTime = 2.0f;

    public Energy CurrentEnergy { get; } = new();
    public ArrowState State { get; private set; } = ArrowState.Normal;

    private float _stateDurationRemaining;
    private float _targetRotation;

    public override void _Ready()
    {
        base._Ready();

        CurrentEnergy.CurrentTint = Energy.Tint.A;
        SetColor();

        _targetRotation = Rotation;
    }

    private void SetColor()
    {
        var marker = CurrentEnergy.Marker;
        SelfModulate = new Color(marker.R * PulsateMinGlow, marker.G * PulsateMinGlow, marker.B * PulsateMinGlow);
    }

    public void Cycle()
    {
        CurrentEnergy.Cycle();
        SetColor();
    }

    public void SetDirection(Vector2 direction)
    {
        float target = direction.Angle() + Mathf.Pi / 2f;

        if (Mathf.IsEqualApprox(_targetRotation, target)) return;

        _targetRotation = Rotation + Mathf.AngleDifference(Rotation, target);
    }


    public void Die() => ChangeState(ArrowState.Stunned, 2.0f);

    public void StartImmunity(float duration) => ChangeState(ArrowState.Immune, duration);

    private void ChangeState(ArrowState newState, float duration = 0f)
    {
        State = newState;
        _stateDurationRemaining = duration;

        Material = newState switch
        {
            ArrowState.Normal => null,
            ArrowState.Stunned => _stunnedMaterial,
            ArrowState.Immune => _immuneMaterial,
            _ => Material
        };
        SetColor();
    }

    public override void _Process(double delta)
    {
        if (_stateDurationRemaining > 0f)
        {
            _stateDurationRemaining -= (float)delta;
            if (_stateDurationRemaining <= 0f)
            {
                if (State == ArrowState.Stunned)
                    ChangeState(ArrowState.Immune, ImmuneTime);
                else
                    ChangeState(ArrowState.Normal);
            }
        }

        // Handle Rotation
        if (!Mathf.IsEqualApprox(Rotation, _targetRotation))
            Rotation = Mathf.MoveToward(Rotation, _targetRotation, RotationSpeed * (float)delta);
    }
}