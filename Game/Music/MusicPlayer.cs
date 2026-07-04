// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Godot;

namespace Chromonia.Music;

public partial class MusicPlayer : AudioStreamPlayer
{
    private Library.MusicLibrary? _library;
    public event System.Action<Result>? OnPlaybackFailed;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Finished += OnFinished;
        Bus = "Music";
    }

    public Result TryPlayMusic() => IsPlaying() ? Result.Ok() : PlayCurrent();

    private void OnFinished()
    {
        Result err;
        if (_library == null)
        {
            err = Result.Fail("MusicLibrary is null on music finished");
            OnPlaybackFailed?.Invoke(err);
            GD.PrintErr(err.Message);
            return;
        }

        _library.MoveNext();
        err = PlayCurrent();
        if (err) return;

        GD.PrintErr(err.Message);
        OnPlaybackFailed?.Invoke(err);
    }

    private Result PlayCurrent()
    {
        _library ??= GetNodeOrNull<Library.MusicLibrary>("/root/MusicLibrary");
        Result err;
        if (_library == null)
        {
            err = Result.Fail("MusicLibrary is null on PlayCurrent");
            OnPlaybackFailed?.Invoke(err);
            GD.PrintErr(err.Message);
            return err;
        }

        var result = _library.LoadCurrentResource();
        if (!result) return Result.Fail(result.ErrorMessage);

        Stream = result.Value;
        Play();
        return Result.Ok();
    }
}