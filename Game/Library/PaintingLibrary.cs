// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Library;

public partial class PaintingLibrary : ResourceLibrary<Texture2D>
{
    protected override string JsonPath => "res://Paitings/paintings.json";
    protected override string FolderPath => "res://Paitings/";
}