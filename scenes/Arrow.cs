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

    private static readonly Color AColor = new(0.8f, 0.25f, 0.2f);
    private static readonly Color BColor = new(0.2f, 0.35f, 0.75f);

    private const float PulsateScale = 1.25f;
    private const float PulsateDuration = 0.25f;
    private const float PulsateBrighten = 0.35f;

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
        Modulate = TintColor;
        var brightColor = TintColor.Lightened(PulsateBrighten);

        _pulsateTween = CreateTween().SetLoops().SetParallel();

        _pulsateTween.TweenProperty(this, "scale", _baseScale * PulsateScale, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _pulsateTween.TweenProperty(this, "modulate", brightColor, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        _pulsateTween.Chain().TweenProperty(this, "scale", _baseScale, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _pulsateTween.Chain().TweenProperty(this, "modulate", TintColor, PulsateDuration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}