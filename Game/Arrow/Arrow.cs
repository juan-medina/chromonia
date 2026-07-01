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
    [Export] private Shader _blinkShader = null!;

    private const float PulsateMinGlow = 0.9f;
    private const float RotationSpeed = Mathf.Pi / 0.25f; // Radians per second to complete rotation in 0.25s
    private static readonly Color DeadColor = Colors.Red;
    private static readonly Color ImmuneColor = Colors.LightGray;

    public Energy CurrentEnergy { get; } = new();
    public ArrowState State { get; private set; } = ArrowState.Normal;

    private float _stateDurationRemaining;
    private float _targetRotation;

    public override void _Ready()
    {
        base._Ready();

        CurrentEnergy.CurrentTint = Energy.Tint.A;
        var m1 = CurrentEnergy.Marker;
        SelfModulate = new Color(m1.R * PulsateMinGlow, m1.G * PulsateMinGlow, m1.B * PulsateMinGlow);
        _targetRotation = Rotation;
    }

    public void Cycle()
    {
        CurrentEnergy.Cycle();
        var m2 = CurrentEnergy.Marker;
        SelfModulate = new Color(m2.R * PulsateMinGlow, m2.G * PulsateMinGlow, m2.B * PulsateMinGlow);
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


    public void Die() => ChangeState(ArrowState.Stunned, 2.0f);

    public void StartImmunity(float duration) => ChangeState(ArrowState.Immune, duration);

    private void ChangeState(ArrowState newState, float duration = 0f)
    {
        State = newState;
        _stateDurationRemaining = duration;

        switch (newState)
        {
            case ArrowState.Normal:
            {
                var mNorm = CurrentEnergy.Marker;
                SelfModulate = new Color(mNorm.R * PulsateMinGlow, mNorm.G * PulsateMinGlow, mNorm.B * PulsateMinGlow);
                Material = null;
                break;
            }

            case ArrowState.Stunned:
            {
                var mStun = CurrentEnergy.Marker;
                SelfModulate = new Color(mStun.R * PulsateMinGlow, mStun.G * PulsateMinGlow, mStun.B * PulsateMinGlow);
            }
            {
                var smRed = new ShaderMaterial { Shader = _blinkShader };
                smRed.SetShaderParameter("blink_color", DeadColor);
                Material = smRed;
            }

                break;

            case ArrowState.Immune:
            {
                var mImm = CurrentEnergy.Marker;
                SelfModulate = new Color(mImm.R * PulsateMinGlow, mImm.G * PulsateMinGlow, mImm.B * PulsateMinGlow);
            }
            {
                var smWhite = new ShaderMaterial { Shader = _blinkShader };
                smWhite.SetShaderParameter("blink_color", ImmuneColor);
                Material = smWhite;
            }

                break;
            default:
                GD.PrintErr($"Arrow: Unhandled state {nameof(newState)}: {newState}");
                GetTree().Quit();
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (_stateDurationRemaining > 0f)
        {
            _stateDurationRemaining -= (float)delta;
            if (_stateDurationRemaining <= 0f)
            {
                if (State == ArrowState.Stunned)
                    ChangeState(ArrowState.Immune, 2.0f);
                else
                    ChangeState(ArrowState.Normal);
            }
        }

        // Handle Rotation
        if (float.IsNaN(_targetRotation)) return;
        if (!Mathf.IsEqualApprox(Rotation, _targetRotation))
            Rotation = Mathf.MoveToward(Rotation, _targetRotation, RotationSpeed * (float)delta);
    }
}