// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Text.Json;
using Chromonia.Result;
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

    public event Action<Result.Result>? OnLoadFailed;

    protected virtual Result.Result ValidateEntry(ResourceEntry entry) => Result.Result.Ok();

    private readonly List<ResourceEntry> _entries = [];
    private int _index;

    public override void _Ready()
    {
        var result = TryLoadEntries();
        if (!result)
        {
            GetNode<ErrorManager.ErrorManager>("/root/ErrorManager")
                .NotifyFatalError($"{GetType().Name} failed to load: {result.Message}");
            return;
        }

        Shuffle();
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

        // do not modify index if we need to shuffle
        if (_index + 1 >= _entries.Count)
        {
            Shuffle();
            return;
        }

        _index++;
    }

    public Result<T> LoadCurrentResource()
    {
        var entryResult = Current();
        if (!entryResult) return Result<T>.Fail(entryResult.ErrorMessage);

        var path = FolderPath + entryResult.Value.File;
        var resource = ResourceLoader.Load<T>(path);
        if (resource is not null) return Result<T>.Ok(resource);

        var error = Result.Result.Fail($"Could not load resource: {path}");
        OnLoadFailed?.Invoke(error);
        return Result<T>.Fail(error.Message);
    }

    private Result.Result TryLoadEntries()
    {
        using var file = FileAccess.Open(JsonPath, FileAccess.ModeFlags.Read);
        if (file is null) return Result.Result.Fail($"Could not open {JsonPath}");

        try
        {
            using var doc = JsonDocument.Parse(file.GetAsText());
            foreach (var element in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                var fileProp = element.GetProperty("file").GetString();
                if (string.IsNullOrWhiteSpace(fileProp)) continue;

                var metadata = new Dictionary<string, string>();
                if (element.TryGetProperty("metadata", out var metaElement))
                    foreach (var prop in metaElement.EnumerateObject())
                        metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;

                var entry = new ResourceEntry(
                    fileProp,
                    element.GetProperty("name").GetString() ?? "Unknown",
                    element.GetProperty("author").GetString() ?? "Unknown",
                    metadata
                );

                var validation = ValidateEntry(entry);
                if (!validation) return Result.Result.Fail($"Invalid entry '{entry.Name}': {validation.Message}");

                _entries.Add(entry);
            }
        }
        catch (JsonException ex)
        {
            return Result.Result.Fail($"Failed to parse JSON: {ex.Message}");
        }

        return _entries.Count == 0 ? Result.Result.Fail("No items found.") : Result.Result.Ok();
    }

    public void Shuffle()
    {
        if (_entries.Count <= 1) return;

        var at = _entries.Count > 0 && _index < _entries.Count ? _entries[_index] : null;

        // In-place Fisher-Yates shuffle, 0 allocations. Iterating back to front ensures each
        // randomly swapped item is locked into place, guaranteeing an unbiased distribution.
        for (int i = _entries.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (_entries[i], _entries[j]) = (_entries[j], _entries[i]);
        }

        // if we have the same entry AT the current index, swap it with the next entry
        if (at is not null && at.File == _entries[0].File)
            (_entries[0], _entries[1]) = (_entries[1], _entries[0]);

        _index = 0;
    }
}