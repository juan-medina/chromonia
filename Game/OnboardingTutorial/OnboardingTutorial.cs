// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.OnboardingTutorial;

public partial class OnboardingTutorial : CanvasLayer
{
    [Export] private CenterContainer _centerContainer = null!;

    private static bool _hasSeenThisSession;

    public override void _Ready()
    {
        if (_hasSeenThisSession) return;
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_up") && !@event.IsActionPressed("ui_down") &&
            !@event.IsActionPressed("ui_left") && !@event.IsActionPressed("ui_right") &&
            !@event.IsActionPressed("energy_cycle")) return;
        FadeOut();
        SetProcessUnhandledInput(false);
    }

    private void FadeOut()
    {
        _hasSeenThisSession = true;
        var tween = CreateTween();
        tween.TweenProperty(_centerContainer, "modulate:a", 0.0f, 1.0f);
        tween.TweenCallback(new Callable(this, MethodName.FinishFade));
    }

    private void FinishFade()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
    }
}