// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Godot;

namespace Chromonia.Music;

public partial class MusicPlayer : AudioStreamPlayer
{
    private Library.MusicLibrary? _library;
    public event System.Action<AppError>? OnPlaybackFailed;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Finished += OnFinished;
        Bus = "Music";
    }

    public AppError TryPlayMusic() => IsPlaying() ? AppError.Ok() : PlayCurrent();

    private void OnFinished()
    {
        AppError err;
        if (_library == null)
        {
            err = new AppError(false, "MusicLibrary is null on music finished");
            OnPlaybackFailed?.Invoke(err);
            GD.PrintErr(err.Message);
            return;
        }

        _library.MoveNext();
        err = PlayCurrent();
        if (err.Success) return;

        GD.PrintErr(err.Message);
        OnPlaybackFailed?.Invoke(err);
    }

    private AppError PlayCurrent()
    {
        _library ??= GetNodeOrNull<Library.MusicLibrary>("/root/MusicLibrary");
        AppError err;
        if (_library == null)
        {
            err = new AppError(false, "MusicLibrary is null on PlayCurrent");
            OnPlaybackFailed?.Invoke(err);
            GD.PrintErr(err.Message);
            return err;
        }

        (Stream, err) = _library.LoadCurrentResource();
        if (!err.Success) return err;

        Play();
        return AppError.Ok();
    }
}