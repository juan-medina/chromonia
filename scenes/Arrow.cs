// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Scenes;

public partial class Arrow : Sprite2D
{
    public enum Tint
    {
        A,
        B
    }

    private static readonly Color AColor = new(2.5f, 0.6f, 0.2f);
    private static readonly Color ALineColor = new(1.5f, 0.0f, 0.0f);

    private static readonly Color BColor = new(0.2f, 0.6f, 2.5f);
    private static readonly Color BLineColor = new(0.0f, 0.0f, 1.5f);

    private const float PulsateScale = 1.25f;
    private const float PulsateDuration = 0.25f;
    private const float PulsateMinGlow = 0.9f;
    private const float PulsateMaxGlow = 1.1f;

    private Tint _currentTint;
    private Tween _pulsateTween = null!;
    private Vector2 _baseScale;

    public Tint CurrentTint
    {
        get => _currentTint;
        private set
        {
            _currentTint = value;
            RestartPulsate();
        }
    }

    public Color TintColor => CurrentTint == Tint.A ? AColor : BColor;
    public Color LineColor => CurrentTint == Tint.A ? ALineColor : BLineColor;

    public void Cycle()
    {
        CurrentTint = CurrentTint switch
        {
            Tint.A => Tint.B,
            Tint.B => Tint.A,
            _ => CurrentTint
        };
    }

    public override void _Ready()
    {
        base._Ready();
        _baseScale = Scale;
        _currentTint = Tint.A;

        AddPulsate();
    }

    private void RestartPulsate()
    {
        _pulsateTween.Kill();
        AddPulsate();
    }

    private void AddPulsate()
    {
        var minColor = TintColor * PulsateMinGlow;
        var maxColor = TintColor * PulsateMaxGlow;

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