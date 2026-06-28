// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Chromonia.Core;

public record MusicEntry(
    string File,
    string Title,
    string Composer,
    string Performer,
    string PerformerUrl);


public partial class MusicPlayer : AudioStreamPlayer
{
    private const string JsonPath = "res://music/music.json";
    private const string FolderPath = "res://music/";

    private readonly List<MusicEntry> _entries = [];
    private int _index;

    public event System.Action<AppError>? OnPlaybackFailed;

    public override void _Ready()
    {
        var (success, error) = TryLoadEntries();
        if (!success)
        {
            GD.PrintErr($"MusicPlayer Critical Initialization Failure: {error}");
            return;
        }

        Shuffle();

        Finished += OnFinished;
    }

    private (MusicEntry? Entry, AppError Err) Current()
    {
        return _entries.Count != 0
            ? (_entries[_index], AppError.Ok())
            : (null, AppError.Fail("MusicPlayer has no music loaded."));
    }

    private static (AudioStream? Stream, AppError Err) LoadStream(MusicEntry entry)
    {
        var path = FolderPath + entry.File;
        var stream = ResourceLoader.Load<AudioStream>(path);
        return stream is not null
            ? (stream, AppError.Ok())
            : (null, AppError.Fail($"Could not load audio stream: {path}"));
    }

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
        var (entry, err) = Current();
        if (!err.Success) return err;

        var (stream, loadErr) = LoadStream(entry!);
        if (!loadErr.Success) return loadErr;

        Stream = stream;
        Play();
        return AppError.Ok();
    }

    private void MoveNext()
    {
        if (_entries.Count == 0) return;

        if (_index + 1 < _entries.Count)
        {
            _index++;
            return;
        }

        Shuffle();
        _index = 0;
    }

    private (bool Success, string Error) TryLoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null)
            return (false, $"Could not open {JsonPath}: {FileAccess.GetOpenError()}");

        try
        {
            using var doc = JsonDocument.Parse(file.GetAsText());
            foreach (var element in doc.RootElement.GetProperty("music").EnumerateArray())
            {
                var fileProp = element.GetProperty("file").GetString()!;
                if (string.IsNullOrWhiteSpace(fileProp))
                {
                    continue; // Skip tracks without an mp3 file
                }

                _entries.Add(new MusicEntry(
                    fileProp,
                    element.GetProperty("title").GetString()!,
                    element.GetProperty("composer").GetString()!,
                    element.GetProperty("performer").GetString()!,
                    element.GetProperty("performer_url").GetString()!
                ));
            }
        }
        catch (JsonException ex)
        {
            return (false, $"Failed to parse JSON configuration: {ex.Message}");
        }

        return _entries.Count == 0
            ? (false, $"No music tracks found in the parsed configuration file {JsonPath}")
            : (true, string.Empty);
    }

    private void Shuffle()
    {
        if (_entries.Count <= 1) return;

        var at = _entries.Count > 0 && _index < _entries.Count ? _entries[_index] : null;

        for (int i = _entries.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (_entries[i], _entries[j]) = (_entries[j], _entries[i]);
        }

        // Make sure we don't end with the current track in the first position
        if (at is not null && at.File == _entries[0].File)
            (_entries[0], _entries[1]) = (_entries[1], _entries[0]);
    }
}
