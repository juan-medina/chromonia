// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Text.Json;
using Chromonia.Core;
using Godot;

namespace Chromonia.Library;

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
        var result = TryLoadEntries();
        if (!result)
        {
            GD.PrintErr($"{GetType().Name} Critical Initialization Failure: {result.Message}");
            return;
        }

        Shuffle();
        OnLibraryReady();
    }

    protected virtual void OnLibraryReady()
    {
    }

    public Result<ResourceEntry> Current()
    {
        return _entries.Count != 0
            ? Result<ResourceEntry>.Ok(_entries[_index])
            : Result<ResourceEntry>.Fail($"{GetType().Name} has no entries loaded.");
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

    public Result<T> LoadCurrentResource()
    {
        var entryResult = Current();
        if (!entryResult) return Result<T>.Fail(entryResult.ErrorMessage);

        var path = FolderPath + entryResult.Value.File;
        var resource = ResourceLoader.Load<T>(path);
        return resource is not null
            ? Result<T>.Ok(resource)
            : Result<T>.Fail($"Could not load resource: {path}");
    }

    private Result TryLoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null) return Result.Fail($"Could not open {JsonPath}");

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
            return Result.Fail($"Failed to parse JSON: {ex.Message}");
        }

        return _entries.Count == 0 ? Result.Fail("No items found.") : Result.Ok();
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