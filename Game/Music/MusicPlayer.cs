// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Godot;

namespace Chromonia.Music;

public partial class MusicPlayer : AudioStreamPlayer
{
    private Library.MusicLibrary _library = null!;
    private UI.ToastNotification _toast = null!;
    public event System.Action<Result>? OnPlaybackFailed;

    private bool _ready;

    public override void _Ready()
    {
        _library = GetNodeOrNull<Library.MusicLibrary>("/root/MusicLibrary");
        _toast = GetNodeOrNull<UI.ToastNotification>("/root/ToastNotification");
        _ready = _library is not null && _toast is not null;

        ProcessMode = ProcessModeEnum.Always;
        Finished += OnFinished;
        Bus = "Music";
    }

    public void Play()
    {
        if (!IsPlaying()) PlayCurrent();
    }

    private void OnFinished()
    {
        if (!_ready) return;

        _library.MoveNext();
        PlayCurrent();
    }

    private void PlayCurrent()
    {
        if (!_ready)
        {
            var err = Result.Fail("MusicPlayer is not ready. Library or ToastNotification is missing.");
            OnPlaybackFailed?.Invoke(err);
            return;
        }

        var result = _library.LoadCurrentResource();
        if (!result)
        {
            OnPlaybackFailed?.Invoke(Result.Fail(result.ErrorMessage));
            return;
        }

        Stream = result.Value;
        base.Play();

        var currentResult = _library.Current();
        if (!currentResult)
        {
            OnPlaybackFailed?.Invoke(Result.Fail(currentResult.ErrorMessage));
            return;
        }

        var entry = currentResult.Value;
        string subtitle = $"{entry.Name} by {entry.Author}\nPerformed by: {entry.Metadata["performer"]}";
        _toast.ShowToast("Now Playing", subtitle);
    }
}