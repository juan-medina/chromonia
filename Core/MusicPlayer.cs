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

    public override void _Ready()
    {
        var success = TryLoadEntries();
        if (!success)
        {
            GD.PrintErr("MusicPlayer Initialization Failure");
            return;
        }

        Shuffle();

        // Connect to the Finished signal to play the next song automatically
        Finished += OnFinished;

        PlayCurrent();
    }

    private void OnFinished()
    {
        MoveNext();
        PlayCurrent();
    }

    public void PlayMusic()
    {
        if (IsPlaying())
            return;

        PlayCurrent();
    }

    private void PlayCurrent()
    {
        if (_entries.Count == 0) return;

        var entry = _entries[_index];
        var path = FolderPath + entry.File;
        var stream = ResourceLoader.Load<AudioStream>(path);

        if (stream is not null)
        {
            Stream = stream;
            Play();
        }
        else
        {
            GD.PrintErr($"Could not load audio stream: {path}");
            // Skip to next if failed
            OnFinished();
        }
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

    private bool TryLoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PrintErr($"Could not open {JsonPath}: {FileAccess.GetOpenError()}");
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(file.GetAsText());
            foreach (var element in doc.RootElement.GetProperty("music").EnumerateArray())
            {
                var fileProp = element.GetProperty("file").GetString()!;
                if (string.IsNullOrWhiteSpace(fileProp))
                    continue; // Skip tracks without an mp3 file

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
            GD.PrintErr($"Failed to parse JSON configuration: {ex.Message}");
            return false;
        }

        return _entries.Count > 0;
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
