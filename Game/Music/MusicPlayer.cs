// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Music;

public partial class MusicPlayer : AudioStreamPlayer
{
    private Library.MusicLibrary _library = null!;
    private ToastNotification.ToastNotification _toast = null!;
    public event System.Action<Result.Result>? OnPlaybackFailed;

    public override void _Ready()
    {
        _library = GetNode<Library.MusicLibrary>("/root/MusicLibrary");
        _toast = GetNode<ToastNotification.ToastNotification>("/root/ToastNotification");

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
        _library.MoveNext();
        PlayCurrent();
    }

    private void PlayCurrent()
    {
        var result = _library.LoadCurrentResource();
        if (!result)
        {
            OnPlaybackFailed?.Invoke(Result.Result.Fail(result.ErrorMessage));
            return;
        }

        Stream = result.Value;
        base.Play();

        var currentResult = _library.Current();
        if (!currentResult)
        {
            OnPlaybackFailed?.Invoke(Result.Result.Fail(currentResult.ErrorMessage));
            return;
        }

        var entry = currentResult.Value;
        string subtitle = $"{entry.Name} by {entry.Author}\nPerformed by: {entry.Metadata["performer"]}";
        _toast.ShowToast("Now Playing", subtitle);
    }
}