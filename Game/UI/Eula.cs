// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Chromonia.Transition;
using Godot;

namespace Chromonia.UI;

public partial class Eula : Control
{
    [Export] private Button _acceptButton = null!;
    [Export] private Button _declineButton = null!;
    [Export] private Components.BigTextPanel _bigTextPanel = null!;

    private UiAudioManager _uiAudioManager = null!;
    private TransitionManager _transitionManager = null!;

    public override void _Ready()
    {
        _uiAudioManager = GetNode<UiAudioManager>("/root/UiAudioManager");
        _transitionManager = GetNode<TransitionManager>("/root/TransitionManager");

        if (!SetupText()) return;

        _acceptButton.Pressed += OnAcceptPressed;
        _declineButton.Pressed += OnDeclinePressed;

        _acceptButton.GrabFocus();

        // setup sounds in the buttons
        _uiAudioManager.ConnectMenuSounds(this);
    }

    private bool SetupText()
    {
        if (!IsInstanceValid(_bigTextPanel))
        {
            HandleFatalError("AboutPanel is missing in the scene tree.");
            return false;
        }

        _bigTextPanel.OnLoadFailed += OnFatalAppError;
        _bigTextPanel.Init("res://UI/EULA.txt");
        return true;
    }


    private void OnDeclinePressed()
    {
        GetTree().Quit();
    }

    private void OnAcceptPressed()
    {
        _transitionManager.TransitionToMenu();
    }

    private void OnFatalAppError(Result err) => HandleFatalError(err.Message);

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Transition Failed: {errorMessage}");
        OS.Alert("Something went wrong loading the game.", "Transition Error");
        GetTree().Quit();
    }
}