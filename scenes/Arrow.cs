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

    private Tween _pulsateTween = null!;
    private Vector2 _baseScale;

    private Energy.Tint CurrentTint
    {
        get => CurrentEnergy.CurrentTint;
        set
        {
            CurrentEnergy.CurrentTint = value;
            RestartPulsate();
        }
    }

    public Energy CurrentEnergy { get; } = new();

    public void Cycle()
    {
        CurrentTint = CurrentTint switch
        {
            Energy.Tint.A => Energy.Tint.B,
            Energy.Tint.B => Energy.Tint.A,
            _ => CurrentTint
        };
    }

    public override void _Ready()
    {
        base._Ready();
        _baseScale = Scale;
        CurrentEnergy.CurrentTint = Energy.Tint.A;

        AddPulsate();
    }

    private void RestartPulsate()
    {
        _pulsateTween.Kill();
        AddPulsate();
    }

    private void AddPulsate()
    {
        var minColor = CurrentEnergy.Line * PulsateMinGlow;
        var maxColor = CurrentEnergy.Fill * PulsateMaxGlow;

        SelfModulate = minColor;

        _pulsateTween = CreateTween().SetLoops().SetParallel();

        _pulsateTween.TweenProperty(this, "scale", _baseScale * PulsateScale, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _pulsateTween.TweenProperty(this, "self_modulate", maxColor, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        _pulsateTween.Chain().TweenProperty(this, "scale", _baseScale, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _pulsateTween.Chain().TweenProperty(this, "self_modulate", minColor, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}