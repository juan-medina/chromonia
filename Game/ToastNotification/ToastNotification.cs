// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Godot;

namespace Chromonia.ToastNotification;

public partial class ToastNotification : CanvasLayer
{
    [Export] private Label _titleLabel = null!;
    [Export] private Label _subtitleLabel = null!;
    [Export] private MarginContainer _container = null!;

    private const float FadeDuration = 0.5f;

    private readonly Queue<(string Title, string Subtitle, float Duration, Action? OnClick)> _queue = new();
    private bool _showing;
    private Action? _currentOnClick;
    private Tween? _currentTween;

    public override void _Ready()
    {
        _container.Modulate = new Color(1, 1, 1, 0);
        _container.GuiInput += OnGuiInput;
    }

    public override void _ExitTree()
    {
        _container.GuiInput -= OnGuiInput;
    }

    public void ShowToast(string title, string subtitle, float duration = 5.0f, Action? onClick = null)
    {
        _queue.Enqueue((title, subtitle, duration, onClick));
        if (!_showing) ShowNext();
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;

        _container.Visible = false;
        _currentOnClick?.Invoke();
        _currentOnClick = null;
    }

    private void ShowNext()
    {
        if (_queue.Count == 0)
        {
            _showing = false;
            return;
        }

        _showing = true;
        var (title, subtitle, duration, onClick) = _queue.Dequeue();

        _titleLabel.Text = title;
        _subtitleLabel.Text = subtitle;
        _currentOnClick = onClick;

        _container.Visible = true;

        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.TweenProperty(_container, "modulate:a", 1.0f, FadeDuration);
        _currentTween.TweenInterval(duration);
        _currentTween.TweenProperty(_container, "modulate:a", 0.0f, FadeDuration);
        _currentTween.TweenCallback(Callable.From(ShowNext));
    }
}