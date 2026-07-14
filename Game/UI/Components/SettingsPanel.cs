// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Chromonia.Core;

namespace Chromonia.UI.Components;

public partial class SettingsPanel : VBoxContainer
{
    [Export] private CheckButton _fullscreenToggle = null!;
    [Export] private HSlider _masterSlider = null!;
    [Export] private HSlider _musicSlider = null!;
    [Export] private HSlider _sfxSlider = null!;

    [Export] private Label _masterLabel = null!;
    [Export] private Label _musicLabel = null!;
    [Export] private Label _sfxLabel = null!;

    private SettingsManager _settingsManager = null!;

    public override void _Ready()
    {
        _settingsManager = GetNode<SettingsManager>("/root/SettingsManager");

        _fullscreenToggle.Toggled += OnFullscreenToggled;
        _masterSlider.ValueChanged += OnMasterVolumeChanged;
        _musicSlider.ValueChanged += OnMusicVolumeChanged;
        _sfxSlider.ValueChanged += OnSfxVolumeChanged;
    }

    private void OnFullscreenToggled(bool toggled) => _settingsManager.SetFullscreen(toggled);

    private void OnMasterVolumeChanged(double val) => ChangeVolume("Master", (float)val);
    private void OnMusicVolumeChanged(double val) => ChangeVolume("Music", (float)val);
    private void OnSfxVolumeChanged(double val) => ChangeVolume("Sfx", (float)val);

    private void ChangeVolume(string bus, float val)
    {
        _settingsManager.SetVolume(bus, val);
        UpdateLabels();
    }

    public void Refresh()
    {
        // Sync UI with current settings without triggering signals to avoid loop
        _fullscreenToggle.SetPressedNoSignal(_settingsManager.Fullscreen);
        _masterSlider.SetValueNoSignal(_settingsManager.MasterVolume);
        _musicSlider.SetValueNoSignal(_settingsManager.MusicVolume);
        _sfxSlider.SetValueNoSignal(_settingsManager.SfxVolume);

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _masterLabel.Text = $"Master Volume: {Mathf.RoundToInt(_masterSlider.Value * 100)}%";
        _musicLabel.Text = $"Music Volume: {Mathf.RoundToInt(_musicSlider.Value * 100)}%";
        _sfxLabel.Text = $"SFX Volume: {Mathf.RoundToInt(_sfxSlider.Value * 100)}%";
    }

    public Control GetFirstFocusableControl() => _fullscreenToggle;
}