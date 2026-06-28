// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Chromonia.Core;

public record ResourceEntry(
    string File,
    string Name,
    string Author,
    Dictionary<string, string> Metadata
);

public abstract partial class ResourceLibrary<T> : Node where T : Resource
{
    protected abstract string JsonPath { get; }
    protected abstract string FolderPath { get; }

    private readonly List<ResourceEntry> _entries = [];
    private int _index;

    public override void _Ready()
    {
        var (success, error) = TryLoadEntries();
        if (!success)
        {
            GD.PrintErr($"{GetType().Name} Critical Initialization Failure: {error}");
            return;
        }

        Shuffle();
        OnLibraryReady();
    }

    protected virtual void OnLibraryReady()
    {
    }

    public (ResourceEntry? Entry, AppError Err) Current()
    {
        return _entries.Count != 0
            ? (_entries[_index], AppError.Ok())
            : (null, AppError.Fail($"{GetType().Name} has no entries loaded."));
    }

    public void MoveNext()
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

    public (T? Resource, AppError Err) LoadCurrentResource()
    {
        var (entry, err) = Current();
        if (!err.Success) return (null, err);

        var path = FolderPath + entry!.File;
        var resource = ResourceLoader.Load<T>(path);
        return resource is not null
            ? (resource, AppError.Ok())
            : (null, AppError.Fail($"Could not load resource: {path}"));
    }

    private (bool Success, string Error) TryLoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null) return (false, $"Could not open {JsonPath}");

        try
        {
            using var doc = JsonDocument.Parse(file.GetAsText());
            foreach (var element in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                var fileProp = element.GetProperty("file").GetString();
                if (string.IsNullOrWhiteSpace(fileProp)) continue;

                var metadata = new Dictionary<string, string>();
                if (element.TryGetProperty("metadata", out var metaElement))
                {
                    foreach (var prop in metaElement.EnumerateObject())
                    {
                        metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }

                _entries.Add(new ResourceEntry(
                    fileProp,
                    element.GetProperty("name").GetString() ?? "Unknown",
                    element.GetProperty("author").GetString() ?? "Unknown",
                    metadata
                ));
            }
        }
        catch (JsonException ex)
        {
            return (false, $"Failed to parse JSON: {ex.Message}");
        }

        return _entries.Count == 0 ? (false, "No items found.") : (true, string.Empty);
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

        if (at is not null && at.File == _entries[0].File)
            (_entries[0], _entries[1]) = (_entries[1], _entries[0]);
    }
}