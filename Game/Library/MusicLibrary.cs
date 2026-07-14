// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Godot;

namespace Chromonia.Library;

public partial class MusicLibrary : ResourceLibrary<AudioStream>
{
    protected override string JsonPath => "res://Music/Songs/music.json";
    protected override string FolderPath => "res://Music/Songs/";

    protected override Result ValidateEntry(ResourceEntry entry)
    {
        return !entry.Metadata.ContainsKey("performer")
            ? Result.Fail("missing required metadata key 'performer'")
            : Result.Ok();
    }
}