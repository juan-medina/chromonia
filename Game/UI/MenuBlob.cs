// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.UI;

public partial class MenuBlob : Node2D
{
    private const float GrowthRate = 90f; // pixels per second
    private const float MaxGrowDelay = 0.7f; // seconds

    private Color _displayColor;
    private float _currentRadius;
    private float _growDelay;

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
        _growDelay = (float)GD.RandRange(0f, MaxGrowDelay);
        var shader = ResourceLoader.Load<Shader>("res://Enemies/blob_soft.gdshader");
        Material = new ShaderMaterial { Shader = shader };
    }

    public override void _Process(double delta)
    {
        if (_growDelay > 0f)
        {
            _growDelay -= (float)delta;
            return;
        }

        if (!(_currentRadius < Radius)) return;
        _currentRadius = Mathf.Min(_currentRadius + GrowthRate * (float)delta, Radius);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float drawRadius = _currentRadius * 1.5f;
        var rect = new Rect2(-drawRadius, -drawRadius, drawRadius * 2, drawRadius * 2);
        DrawRect(rect, _displayColor);
    }
}