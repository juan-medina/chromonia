// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Core;

public partial class MusicPlayer : ResourceLibrary<AudioStream>
{
    protected override string JsonPath => "res://music/music.json";
    protected override string FolderPath => "res://music/";

    private AudioStreamPlayer _player = null!;
    public event System.Action<AppError>? OnPlaybackFailed;

    protected override void OnLibraryReady()
    {
        _player = new AudioStreamPlayer();
        AddChild(_player);
        _player.Finished += OnFinished;
    }

    private bool IsPlaying() => _player.Playing;

    public void Stop() => _player.Stop();

    public AppError TryPlayMusic() => IsPlaying() ? AppError.Ok() : PlayCurrent();

    private void OnFinished()
    {
        MoveNext();
        var err = PlayCurrent();
        if (err.Success) return;

        GD.PrintErr(err.Message);
        OnPlaybackFailed?.Invoke(err);
    }

    private AppError PlayCurrent()
    {
        var (stream, err) = LoadCurrentResource();
        if (!err.Success) return err;

        _player.Stream = stream;
        _player.Play();
        return AppError.Ok();
    }
}