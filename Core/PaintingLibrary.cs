// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Chromonia.Core;

public record PaintingEntry(
    string File,
    string Title,
    string Years,
    string Artist,
    string Nationality);


public partial class PaintingLibrary : Node
{
    private const string JsonPath = "res://paitings/paintings.json";
    private const string FolderPath = "res://paitings/";

    private readonly List<PaintingEntry> _entries = [];
    private int _index;

    public override void _Ready()
    {
        var (success, error) = TryLoadEntries();
        if (!success)
        {
            GD.PrintErr($"PaintingLibrary Critical Initialization Failure: {error}");
            return;
        }

        Shuffle();
    }

    public (PaintingEntry? Entry, AppError Err) Current()
    {
        return _entries.Count != 0
            ? (_entries[_index], AppError.Ok())
            : (null, AppError.Fail("PaintingLibrary has no paintings loaded."));
    }

    public void MoveNext()
    {
        if (_entries.Count == 0)
        {
            GD.PrintErr("PaintingLibrary: cannot move next, no paintings");
            return;
        }

        if (_index + 1 < _entries.Count)
        {
            _index++;
            return;
        }

        Shuffle();
        _index = 0;
    }

    public static (Texture2D? Texture, AppError Err) LoadTexture(PaintingEntry entry)
    {
        var path = FolderPath + entry.File;
        var texture = ResourceLoader.Load<Texture2D>(path);
        return texture is not null
            ? (texture, AppError.Ok())
            : (null, AppError.Fail($"Could not load image: {path}"));
    }

    private (bool Success, string Error) TryLoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            return (false, $"Could not open {JsonPath}: {FileAccess.GetOpenError()}");
        }

        try
        {
            using var doc = JsonDocument.Parse(file.GetAsText());
            foreach (var element in doc.RootElement.GetProperty("paintings").EnumerateArray())
            {
                _entries.Add(new PaintingEntry(
                    element.GetProperty("file").GetString()!,
                    element.GetProperty("title").GetString()!,
                    element.GetProperty("years").GetString()!,
                    element.GetProperty("artist").GetString()!,
                    element.GetProperty("nationality").GetString()!
                ));
            }
        }
        catch (JsonException ex)
        {
            return (false, $"Failed to parse JSON configuration: {ex.Message}");
        }

        return _entries.Count == 0
            ? (false, $"No paintings found in the parsed configuration file {JsonPath}")
            : (true, string.Empty);
    }

    private void Shuffle()
    {
        if (_entries.Count <= 1) return;

        var at = _entries[_index];

        for (int i = _entries.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (_entries[i], _entries[j]) = (_entries[j], _entries[i]);
        }

        // Make sure we don't end with the current painting in the first position
        if (at.File == _entries[0].File)
            (_entries[0], _entries[1]) = (_entries[1], _entries[0]);
    }
}
