// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.UI;

public partial class MenuBlob : Node2D
{
    private Color _displayColor;
    public Color DisplayColor
    {
        set
        {
            if (_displayColor == value) return;
            _displayColor = value;
            QueueRedraw();
        }
    }

    public float Radius { get; set; }

    public override void _Ready()
    {
        var shader = ResourceLoader.Load<Shader>("res://Enemies/blob_soft.gdshader");
        Material = new ShaderMaterial { Shader = shader };
    }

    public override void _Draw()
    {
        float drawRadius = Radius * 1.5f;
        var rect = new Rect2(-drawRadius, -drawRadius, drawRadius * 2, drawRadius * 2);
        DrawRect(rect, _displayColor);
    }
}
