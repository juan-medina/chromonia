// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Chromonia.UI.Components;

namespace Chromonia.UI;

public partial class PauseMenu : CanvasLayer
{
    [Export] private Button _resumeButton = null!;
    [Export] private Button _restartButton = null!;
    [Export] private SettingsPanel _settingsPanel = null!;
    [Export] private Button _quitButton = null!;

    private Transition.TransitionManager _transitionManager = null!;
    private UiAudioManager _uiAudioManager = null!;

    public override void _Ready()
    {
        _transitionManager = GetNode<Transition.TransitionManager>("/root/TransitionManager");
        _uiAudioManager = GetNode<UiAudioManager>("/root/UiAudioManager");

        // Connect UI elements
        _resumeButton.Pressed += CloseMenu;
        _restartButton.Pressed += RestartGame;
        _quitButton.Pressed += QuitGame;

        _uiAudioManager.ConnectMenuSounds(this);
    }

    private void QuitGame()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        GetTree().Paused = false;
        _transitionManager.TransitionToMenu();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause_game"))
        {
            if (Visible)
                CloseMenu();
            else
                OpenMenu();

            GetViewport().SetInputAsHandled();
        }
        else if (Visible && @event.IsActionPressed("ui_cancel"))
        {
            CloseMenu();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OpenMenu()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;
        Visible = true;
        GetTree().Paused = true;

        // Sync UI with current settings
        _settingsPanel.Refresh();

        // Grab focus so controller works immediately
        _resumeButton.GrabFocus();

        // Fade in
        var colorRect = GetNode<Control>("ColorRect");
        colorRect.Modulate = new Color(1, 1, 1, 0);

        CreateTween().SetPauseMode(Tween.TweenPauseMode.Process)
            .TweenProperty(colorRect, "modulate", new Color(1, 1, 1), 0.15f);
    }

    private void CloseMenu()
    {
        var colorRect = GetNode<Control>("ColorRect");

        var tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(colorRect, "modulate", new Color(1, 1, 1, 0), 0.15f);
        tween.TweenCallback(new Callable(this, MethodName.FinishClose));
    }

    private void FinishClose()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        Visible = false;
        GetTree().Paused = false;
    }

    private void RestartGame()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        GetTree().Paused = false;
        _transitionManager.ReloadCurrentScene();
    }
}