// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Chromonia.Transition;

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

        _playButton.Pressed += OnPlayPressed;
        _optionsButton.Pressed += OnOptionsPressed;
        _aboutButton.Pressed += OnAboutPressed;
        _exitButton.Pressed += OnExitPressed;

        _uiAudioManager.ConnectMenuSounds(this);

        _playButton.GrabFocus();
    }

    private void OnPlayPressed() => _transitionManager.TransitionToGame();
    private static void OnOptionsPressed() => GD.Print("Options Pressed");
    private static void OnAboutPressed() => GD.Print("About Pressed");
    private void OnExitPressed() => GetTree().Quit();
}