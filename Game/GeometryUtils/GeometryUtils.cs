// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Godot;

namespace Chromonia.GeometryUtils;

public static class GeometryUtils
{
    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq == 0) return (p - a).Length();
        float t = Math.Clamp((p - a).Dot(ab) / lengthSq, 0f, 1f);
        return (p - (a + t * ab)).Length();
    }

    public static Vector2? SegmentIntersectsSegment(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 r = b - a;
        Vector2 s = d - c;
        float rxs = r.Cross(s);
        float cmAxs = (c - a).Cross(s);

        // Lines are collinear or parallel. We will ignore overlapping collinear segments for our collision logic
        if (Mathf.IsZeroApprox(rxs))
            return null;

        float t = cmAxs / rxs;
        float u = (c - a).Cross(r) / rxs;

        if (t is >= 0f and <= 1f && u is >= 0f and <= 1f)
            return a + t * r;

        return null;
    }

    public static Vector2 ClampPointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq == 0) return a;
        float t = Math.Clamp((p - a).Dot(ab) / lengthSq, 0f, 1f);
        return a + t * ab;
    }

    public static float GetPolygonArea(Vector2[] polygon)
    {
        float area = 0;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            area += (polygon[j].X + polygon[i].X) * (polygon[j].Y - polygon[i].Y);
            j = i;
        }

        return Math.Abs(area / 2.0f);
    }

    public static Vector2[] BuildPolygon(Vector2[] perimeter, int startSeg, int endSeg, Vector2[] activeLine,
        bool forward)
    {
        int totalUnique = perimeter.Length - 1;
        var poly = new List<Vector2>(activeLine);

        bool startBeforeEnd = perimeter[startSeg].DistanceSquaredTo(activeLine[0]) <
                              perimeter[startSeg].DistanceSquaredTo(activeLine[^1]);

        if (startSeg == endSeg)
            if (forward && !startBeforeEnd || !forward && startBeforeEnd)
                return poly.ToArray();

        int curr = forward ? (endSeg + 1) % totalUnique : endSeg;
        int target = forward ? startSeg : (startSeg + 1) % totalUnique;
        int step = forward ? 1 : -1;

        while (true)
        {
            poly.Add(perimeter[curr]);
            if (curr == target) break;
            curr = (curr + step + totalUnique) % totalUnique;
        }

        return poly.ToArray();
    }
}