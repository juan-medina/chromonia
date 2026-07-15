// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.ToastNotification;

public partial class ToastNotification : CanvasLayer
{
    [Export] private Label _titleLabel = null!;
    [Export] private Label _subtitleLabel = null!;
    [Export] private MarginContainer _marginContainer = null!;

    private const float FadeDuration = 0.5f;

    public override void _Ready()
    {
        _marginContainer.Modulate = new Color(1, 1, 1, 0); // Hide initially
    }

    public void ShowToast(string title, string subtitle, float duration = 5.0f)
    {
        _titleLabel.Text = title;
        _subtitleLabel.Text = subtitle;

        var tween = CreateTween();
        tween.TweenProperty(_marginContainer, "modulate:a", 1.0f, FadeDuration);
        tween.TweenInterval(duration);
        tween.TweenProperty(_marginContainer, "modulate:a", 0.0f, FadeDuration);
    }
}
