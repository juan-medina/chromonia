// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Godot;

namespace Chromonia.Music;

public partial class MusicPlayer : AudioStreamPlayer
{
    private Library.MusicLibrary _library = null!;
    public event System.Action<Result>? OnPlaybackFailed;
    public event System.Action<Result<Library.ResourceEntry>>? OnPlaybackStarted;

    private bool _ready;

    public override void _Ready()
    {
        _library = GetNodeOrNull<Library.MusicLibrary>("/root/MusicLibrary");
        _ready = _library is not null;

        ProcessMode = ProcessModeEnum.Always;
        Finished += OnFinished;
        Bus = "Music";
    }

    public Result TryPlayMusic() => IsPlaying() ? Result.Ok() : PlayCurrent();

    private void OnFinished()
    {
        if (!_ready) return;

        _library.MoveNext();
        var result = PlayCurrent();
        if (result) return;

        GD.PrintErr(result.Message);
        OnPlaybackFailed?.Invoke(result);
    }

    private Result PlayCurrent()
    {
        if (!_ready)
        {
            var err = Result.Fail("MusicLibrary is not ready on PlayCurrent");
            OnPlaybackFailed?.Invoke(err);
            GD.PrintErr(err.Message);
            return err;
        }

        var result = _library.LoadCurrentResource();
        if (!result) return Result.Fail(result.ErrorMessage);

        Stream = result.Value;
        Play();
        OnPlaybackStarted?.Invoke(_library.Current());
        return Result.Ok();
    }
}