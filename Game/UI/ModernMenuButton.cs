// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.UI;

public partial class ModernMenuButton : Button
{
    [Export] private ColorRect _underline = null!;

    private const float AnimDuration = 0.2f;

    public override void _Ready()
    {
        MouseEntered += GrabFocus;

        FocusEntered += Focus;
        FocusExited += Unfocus;

        _underline.Modulate = new Color(1, 1, 1);
        _underline.Scale = new Vector2(0.0f, 1.0f);
    }

    private void Focus() => SetState(true);
    private void Unfocus() => SetState(false);

    private void SetState(bool hovered)
    {
        float targetScaleX = hovered ? 1.0f : 0.0f;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_underline, "scale:x", targetScaleX, AnimDuration);
    }
}