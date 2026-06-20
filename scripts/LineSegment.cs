// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;

namespace Chromonia.Scripts;

public readonly struct LineSegment(Vector2 a, Vector2 b, float tolerance)
{
    public float Tolerance { get; } = tolerance;
    public Vector2 GetClosestPoint(Vector2 p)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;
        float abLenSq = ab.LengthSquared();
        if (abLenSq == 0f) return a;

        float t = Math.Clamp(ap.Dot(ab) / abLenSq, 0f, 1f);
        return a + t * ab;
    }
}