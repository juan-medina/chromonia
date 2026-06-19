// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Chromonia.Scripts;

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
        LoadEntries();
        Shuffle();
    }

    public PaintingEntry? Current()
    {
        if (_entries.Count != 0) return _entries[_index];
        GD.PrintErr("PaintingLibrary: no paintings");
        return null;
    }

    public void MoveNext()
    {
        if (_entries.Count == 0)
        {
            GD.PrintErr("PaintingLibrary: no paintings");
            return;
        }


        // if we don't reach the end just advance
        if (_index + 1 < _entries.Count)
        {
            _index++;
            return;
        }

        // shuffle and start over
        Shuffle();
        _index = 0;
    }

    public static ImageTexture? LoadTexture(PaintingEntry entry)
    {
        var path = FolderPath + entry.File;
        var image = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
        if (image is not null) return ImageTexture.CreateFromImage(image);

        GD.PrintErr($"Could not load image: {path}");
        return null;
    }

    private void LoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            GD.PrintErr($"PaintingLibrary: could not open {JsonPath}: {FileAccess.GetOpenError()}");
            return;
        }

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

        if (_entries.Count == 0)
        {
            GD.PrintErr($"PaintingLibrary: no paintings found in {JsonPath}");
        }
    }

    private void Shuffle()
    {
        // we don't shuffle if there is only 1 entry
        if (_entries.Count <= 1) return;

        // where we are now
        var at = _entries[_index];

        // shuffle the entries
        for (int i = _entries.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (_entries[i], _entries[j]) = (_entries[j], _entries[i]);
        }

        // make sure we don't end with the current painting to the first position
        if (at.File == _entries[0].File)
            (_entries[0], _entries[1]) = (_entries[1], _entries[0]);
    }
}