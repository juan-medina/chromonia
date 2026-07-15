// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Theme;

public partial class ModernFocusAnimator : ColorRect
{
    private const float AnimDuration = 0.2f;

    public override void _Ready()
    {
        var parent = GetParent<Control>();
        parent.MouseEntered += parent.GrabFocus;

        parent.FocusEntered += Focus;
        parent.FocusExited += Unfocus;

        Modulate = new Color(1, 1, 1);
        Scale = new Vector2(0.0f, 1.0f);
    }

    public override void _ExitTree()
    {
        var parent = GetParent<Control>();
        parent.MouseEntered -= parent.GrabFocus;

        parent.FocusEntered -= Focus;
        parent.FocusExited -= Unfocus;

        base._ExitTree();
    }

    private void Focus() => SetState(true);
    private void Unfocus() => SetState(false);

    private void SetState(bool hovered)
    {
        float targetScaleX = hovered ? 1.0f : 0.0f;

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(this, "scale:x", targetScaleX, AnimDuration);
    }
}