// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Chromonia.Transition;
using Chromonia.Core;

namespace Chromonia.UI;

public partial class MainMenu : Control
{
    [Export] private ModernMenuButton _playButton = null!;
    [Export] private ModernMenuButton _optionsButton = null!;
    [Export] private ModernMenuButton _aboutButton = null!;
    [Export] private ModernMenuButton _exitButton = null!;

    private TransitionManager _transitionManager = null!;
    private UiAudioManager _uiAudioManager = null!;

    public override void _Ready()
    {
        _transitionManager = GetNode<TransitionManager>("/root/TransitionManager");
        _uiAudioManager = GetNode<UiAudioManager>("/root/UiAudioManager");

        _transitionManager.OnTransitionFailed += OnFatalAppError;

        _playButton.Pressed += OnPlayPressed;
        _optionsButton.Pressed += OnOptionsPressed;
        _aboutButton.Pressed += OnAboutPressed;
        _exitButton.Pressed += OnExitPressed;

        _uiAudioManager.ConnectMenuSounds(this);

        _playButton.GrabFocus();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_transitionManager)) _transitionManager.OnTransitionFailed -= OnFatalAppError;
    }

    private void OnPlayPressed() => _transitionManager.TransitionToGame();
    private static void OnOptionsPressed() => GD.Print("Options Pressed");
    private static void OnAboutPressed() => GD.Print("About Pressed");
    private void OnExitPressed() => GetTree().Quit();

    private void OnFatalAppError(Result err) => HandleFatalError(err.Message);

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Transition Failed: {errorMessage}");
        OS.Alert("Something went wrong loading the game.", "Transition Error");
        GetTree().Quit();
    }
}