// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Library;

public partial class MusicLibrary : ResourceLibrary<AudioStream>
{
    protected override string JsonPath => "res://music/music.json";
    protected override string FolderPath => "res://music/";
}