// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Core;

public partial class SettingsManager : Node
{
    private const string ConfigPath = "user://settings.cfg";
    private const string SectionDisplay = "display";
    private const string SectionAudio = "audio";

    public bool Fullscreen { get; private set; }
    public float MasterVolume { get; private set; } = 1.0f;
    public float MusicVolume { get; private set; } = 1.0f;
    public float SfxVolume { get; private set; } = 1.0f;

    private float _baseMasterDb;
    private float _baseMusicDb;
    private float _baseSfxDb;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // get base Db from the default buss setup
        _baseMasterDb = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"));
        _baseMusicDb = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"));
        _baseSfxDb = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Sfx"));

        LoadSettings();
        ApplyWindowMode();
        ApplyAudioVolumes();
    }

    private void LoadSettings()
    {
        using var config = new ConfigFile();
        var err = config.Load(ConfigPath);

        if (err == Error.Ok)
        {
            Fullscreen = (bool)config.GetValue(SectionDisplay, "fullscreen", false);
            MasterVolume = (float)config.GetValue(SectionAudio, "master_volume", 1.0f);
            MusicVolume = (float)config.GetValue(SectionAudio, "music_volume", 1.0f);
            SfxVolume = (float)config.GetValue(SectionAudio, "sfx_volume", 1.0f);
        }
        else
        {
            // First time launch or missing config - set defaults
            Fullscreen = false;
            MasterVolume = 1.0f;
            MusicVolume = 1.0f;
            SfxVolume = 1.0f;
        }
    }

    private void SaveSettings()
    {
        using var config = new ConfigFile();
        config.SetValue(SectionDisplay, "fullscreen", Fullscreen);
        config.SetValue(SectionAudio, "master_volume", MasterVolume);
        config.SetValue(SectionAudio, "music_volume", MusicVolume);
        config.SetValue(SectionAudio, "sfx_volume", SfxVolume);

        var err = config.Save(ConfigPath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"SettingsManager: Failed to save config to {ConfigPath}. Error: {err}");
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (Fullscreen == isFullscreen) return;

        Fullscreen = isFullscreen;
        ApplyWindowMode();
        SaveSettings();
    }

    public void SetVolume(string busName, float volumeLinear)
    {
        volumeLinear = Mathf.Clamp(volumeLinear, 0.0f, 1.0f);

        switch (busName.ToLower())
        {
            case "master":
                MasterVolume = volumeLinear;
                break;
            case "music":
                MusicVolume = volumeLinear;
                break;
            case "sfx":
                SfxVolume = volumeLinear;
                break;
            default:
                GD.PrintErr($"SettingsManager: Invalid bus name '{busName}'");
                return;
        }

        ApplyBusVolume(busName, volumeLinear);
        SaveSettings();
    }

    private void ApplyWindowMode()
    {
        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }

    private void ApplyAudioVolumes()
    {
        ApplyBusVolume("Master", MasterVolume);
        ApplyBusVolume("Music", MusicVolume);
        ApplyBusVolume("Sfx", SfxVolume);
    }

    private void ApplyBusVolume(string busName, float volumeLinear)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex >= 0)
        {
            float baseDb = busName switch
            {
                "Music" => _baseMusicDb,
                "Sfx" => _baseSfxDb,
                _ => _baseMasterDb
            };

            // Convert linear 0.0-1.0 to Db offset, and apply to the base Db
            float volumeDb = volumeLinear > 0.0f ? baseDb + Mathf.LinearToDb(volumeLinear) : -80.0f;
            AudioServer.SetBusVolumeDb(busIndex, volumeDb);
        }
        else
            GD.PrintErr($"SettingsManager: Audio Bus '{busName}' not found.");
    }
}
