// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Core;

public partial class PaintingLibrary : ResourceLibrary<Texture2D>
{
    protected override string JsonPath => "res://paitings/paintings.json";
    protected override string FolderPath => "res://paitings/";
}