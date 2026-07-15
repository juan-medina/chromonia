// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.GalleryPlaque;

public partial class GalleryPlaque : Node2D
{
    [Export] private Label _titleLabel = null!;
    [Export] private Label _artistLabel = null!;
    [Export] private Control _plaqueContainer = null!;
    [Export] private Control _anchorPoint = null!;

    private const float FadeDuration = 1.0f;

    public override void _Ready()
    {
        Visible = false;
    }

    public void ShowPlaque(string title, string artist, float holdDuration)
    {
        _anchorPoint.Position = Vector2.Zero;
        _titleLabel.Text = title;
        _artistLabel.Text = artist;

        _plaqueContainer.Modulate = new Color(1, 1, 1, 0);
        Visible = true;

        Tween tween = CreateTween();

        // Fade in plaque
        tween.TweenProperty(_plaqueContainer, "modulate:a", 1.0f, FadeDuration);
    }
}
