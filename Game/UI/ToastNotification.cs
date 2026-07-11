// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.UI;

public partial class ToastNotification : MarginContainer
{
    [Export] private Label _titleLabel = null!;
    [Export] private Label _subtitleLabel = null!;

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0); // Hide initially
    }

    public void ShowToast(string title, string subtitle, float duration = 5.0f)
    {
        _titleLabel.Text = title;
        _subtitleLabel.Text = subtitle;

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, 0.5f);
        tween.TweenInterval(duration);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
    }
}
