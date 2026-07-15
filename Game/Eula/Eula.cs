// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Theme;
using Chromonia.Transition;
using Godot;
using SettingsManager = Chromonia.Settings.SettingsManager;

namespace Chromonia.Eula;

public partial class Eula : Control
{
    [Export] private Button _acceptButton = null!;
    [Export] private Button _declineButton = null!;
    [Export] private BigTextPanel _bigTextPanel = null!;
    [Export] private Label _version = null!;

    private AudioTheme _audioTheme = null!;
    private TransitionManager _transitionManager = null!;
    private SettingsManager _settingsManager = null!;
    private ErrorManager.ErrorManager _errorManager = null!;

    public override void _Ready()
    {
        _errorManager = GetNode<ErrorManager.ErrorManager>("/root/ErrorManager");
        _settingsManager = GetNode<SettingsManager>("/root/SettingsManager");
        _transitionManager = GetNode<TransitionManager>("/root/TransitionManager");
        _audioTheme = GetNode<AudioTheme>("/root/AudioTheme");

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
        _audioTheme.ConnectMenuSounds(this);
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
        _bigTextPanel.Init("res://Eula/EULA.txt");
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

    private void OnFatalAppError(Result.Result err) => _errorManager.NotifyFatalError(err);
}