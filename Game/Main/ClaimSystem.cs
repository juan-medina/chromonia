// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Godot;

namespace Chromonia.Main;

public class ClaimSystem(Node2D playfield, Node2D maskRoot, Line2D perimeterLine, Arrow.Arrow arrow)
{
    private float _scaledWidth;
    private float _scaledHeight;

    public void UpdateDimensions(float scaledWidth, float scaledHeight)
    {
        _scaledWidth = scaledWidth;
        _scaledHeight = scaledHeight;
    }

    public (Vector2[] ClaimedPoly, Vector2[] NewPerimeter, float ClaimedArea) DetermineClaimedPolygon(
        Vector2[] perimeter, int startSeg, int hitSegmentIndex, Vector2[] activeArray)
    {
        var poly1 = GeometryUtils.BuildPolygon(perimeter, startSeg, hitSegmentIndex, activeArray, forward: true);
        var poly2 = GeometryUtils.BuildPolygon(perimeter, startSeg, hitSegmentIndex, activeArray, forward: false);

        float area1 = GeometryUtils.GetPolygonArea(poly1);
        float area2 = GeometryUtils.GetPolygonArea(poly2);

        return area1 < area2 ? (poly1, poly2, area1) : (poly2, poly1, area2);
    }

    public void ApplyNewPerimeter(Vector2[] newPerimeter, out Vector2[] updatedPerimeterArray)
    {
        var newPerimeterList = new List<Vector2>(newPerimeter) { newPerimeter[0] };
        updatedPerimeterArray = newPerimeterList.ToArray();
        perimeterLine.Points = newPerimeter;
    }

    public void CreateClaimVisuals(Vector2[] claimedPoly, SceneTree tree)
    {
        Polygon2D claimNode = new Polygon2D
        {
            Polygon = ConvertToMaskCoordinates(claimedPoly),
            Color = arrow.CurrentEnergy.Fill() with { A = 0.0f }
        };
        maskRoot.AddChild(claimNode);

        var tween = tree.CreateTween();
        tween.TweenProperty(claimNode, "color:a", 1.0f, 0.5f);
    }

    public void CreateClaimPhysics(Vector2[] claimedPoly)
    {
        var claimPhysics = new StaticBody2D();

        for (int i = 0; i < claimedPoly.Length; i++)
        {
            var p1 = claimedPoly[i];
            var p2 = claimedPoly[(i + 1) % claimedPoly.Length];

            var segmentShape = new SegmentShape2D { A = p1, B = p2 };
            var collisionShape = new CollisionShape2D { Shape = segmentShape };
            claimPhysics.AddChild(collisionShape);
        }

        playfield.AddChild(claimPhysics);
    }

    private Vector2[] ConvertToMaskCoordinates(Vector2[] poly)
    {
        var shift = new Vector2(_scaledWidth / 2f, _scaledHeight / 2f);
        var shifted = new Vector2[poly.Length];
        for (int i = 0; i < poly.Length; i++) shifted[i] = poly[i] + shift;

        return shifted;
    }
}