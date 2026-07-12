// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Chromonia.Core;

namespace Chromonia.UI;

public partial class PauseMenu : CanvasLayer
{
    [Export] private Button _resumeButton = null!;
    [Export] private Button _restartButton = null!;
    [Export] private CheckButton _fullscreenToggle = null!;
    [Export] private HSlider _masterSlider = null!;
    [Export] private HSlider _musicSlider = null!;
    [Export] private HSlider _sfxSlider = null!;
    [Export] private Button _quitButton = null!;

    [Export] private Label _masterLabel = null!;
    [Export] private Label _musicLabel = null!;
    [Export] private Label _sfxLabel = null!;

    [Export] private AudioStreamPlayer _hoverSfx = null!;
    [Export] private AudioStreamPlayer _clickSfx = null!;

    private SettingsManager _settingsManager = null!;
    private Transition.TransitionManager _transitionManager = null!;

    public override void _Ready()
    {
        _settingsManager = GetNode<SettingsManager>("/root/SettingsManager");
        _transitionManager = GetNode<Transition.TransitionManager>("/root/TransitionManager");

        // Connect UI elements
        _resumeButton.Pressed += CloseMenu;
        _restartButton.Pressed += RestartGame;
        _quitButton.Pressed += QuitGame;

        _fullscreenToggle.Toggled += OnFullscreenToggled;

        _masterSlider.ValueChanged += OnMasterVolumeChanged;
        _musicSlider.ValueChanged += OnMusicVolumeChanged;
        _sfxSlider.ValueChanged += OnSfxVolumeChanged;

        // Setup sound feedback for all interactive controls
        ConnectSounds(_resumeButton);
        ConnectSounds(_restartButton);
        ConnectSounds(_fullscreenToggle);
        ConnectSounds(_masterSlider);
        ConnectSounds(_musicSlider);
        ConnectSounds(_sfxSlider);
        ConnectSounds(_quitButton);
    }

    private void ConnectSounds(Control control)
    {
        control.FocusEntered += PlayHoverSound;
        control.MouseEntered += PlayHoverSound;

        if (control is BaseButton button) button.Pressed += PlayClickSound;
    }

    private void PlayHoverSound() => _hoverSfx.Play();
    private void PlayClickSound() => _clickSfx.Play();
    private void QuitGame() => GetTree().Quit();
    private void OnFullscreenToggled(bool toggled) => _settingsManager.SetFullscreen(toggled);

    private void OnMasterVolumeChanged(double val) => ChangeVolume("Master", (float)val);
    private void OnMusicVolumeChanged(double val) => ChangeVolume("Music", (float)val);
    private void OnSfxVolumeChanged(double val) => ChangeVolume("Sfx", (float)val);

    private void ChangeVolume(string bus, float val)
    {
        _settingsManager.SetVolume(bus, val);
        UpdateLabels();
        _clickSfx.Play();
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
        Visible = true;
        GetTree().Paused = true;

        // Sync UI with current settings
        _fullscreenToggle.SetPressedNoSignal(_settingsManager.Fullscreen);
        _masterSlider.SetValueNoSignal(_settingsManager.MasterVolume);
        _musicSlider.SetValueNoSignal(_settingsManager.MusicVolume);
        _sfxSlider.SetValueNoSignal(_settingsManager.SfxVolume);

        UpdateLabels();

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
        Visible = false;
        GetTree().Paused = false;
    }

    private void RestartGame()
    {
        GetTree().Paused = false;
        Visible = false;

        _transitionManager.ReloadCurrentScene();
    }

    private void UpdateLabels()
    {
        _masterLabel.Text = $"Master Volume: {Mathf.RoundToInt(_masterSlider.Value * 100)}%";
        _musicLabel.Text = $"Music Volume: {Mathf.RoundToInt(_musicSlider.Value * 100)}%";
        _sfxLabel.Text = $"SFX Volume: {Mathf.RoundToInt(_sfxSlider.Value * 100)}%";
    }
}