// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Scripts;

public partial class LineSegment : Polygon2D
{
    public LineSegment()
    {
    }

    public LineSegment(Vector2 from, Vector2 to, float thickness, Color color, float outlineThickness = 1.5f, Color? outlineColor = null)
    {
        var direction = to - from;
        var length = direction.Length();
        if (length < 0.0001f) return;

        var dir = direction / length;
        var ortho = dir.Orthogonal();
        var half = ortho * (thickness / 2f);

        Polygon = [from - half, to - half, to + half, from + half];
        Color = color;

        if (outlineThickness > 0f)
        {
            var halfOutline = ortho * (thickness / 2f + outlineThickness);
            var fromOutline = from - dir * outlineThickness;
            var toOutline = to + dir * outlineThickness;

            var outline = new Polygon2D
            {
                Polygon = [fromOutline - halfOutline, toOutline - halfOutline, toOutline + halfOutline, fromOutline + halfOutline],
                Color = outlineColor ?? Colors.Black,
                ZIndex = -1,
                ZAsRelative = true
            };
            AddChild(outline);
        }
    }
}