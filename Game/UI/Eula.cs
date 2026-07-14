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
    [Export] private Label _version = null!;

    private UiAudioManager _uiAudioManager = null!;
    private TransitionManager _transitionManager = null!;
    private SettingsManager _settingsManager = null!;
    private ErrorManager _errorManager = null!;

    public override void _Ready()
    {
        _errorManager = GetNode<ErrorManager>("/root/ErrorManager");
        _settingsManager = GetNode<SettingsManager>("/root/SettingsManager");
        _transitionManager = GetNode<TransitionManager>("/root/TransitionManager");
        _uiAudioManager = GetNode<UiAudioManager>("/root/UiAudioManager");

        _transitionManager.OnTransitionFailed += OnFatalAppError;

        var version = ProjectSettings.GetSetting("application/config/version").ToString();
        var minorVersion = GetMinorVersion(version);

        if (_settingsManager.IsEulaAccepted(minorVersion))
        {
            _transitionManager.SkipToMenu();
            return;
        }

        Visible = true;

        if (!SetupText()) return;

        _version.Text = $"v{version}";
        _acceptButton.Pressed += OnAcceptPressed;
        _declineButton.Pressed += OnDeclinePressed;
        _acceptButton.GrabFocus();
        _uiAudioManager.ConnectMenuSounds(this);
    }

    private static string GetMinorVersion(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version;
    }

    private bool SetupText()
    {
        if (!IsInstanceValid(_bigTextPanel))
        {
            _errorManager.NotifyFatalError("AboutPanel is missing in the scene tree.");
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
        var version = ProjectSettings.GetSetting("application/config/version").ToString();
        _settingsManager.SetEulaAccepted(GetMinorVersion(version));
        _transitionManager.TransitionToMenu();
    }

    private void OnFatalAppError(Result err) => _errorManager.NotifyFatalError(err);
}