// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Chromonia.Energy;
using Godot;

namespace Chromonia.PlayerSystem;

public enum PlayerState
{
    OnPerimeter,
    Drawing,
    Won
}

public class PlayerSystem(Arrow.Arrow arrow, Line2D drawingLine)
{
    private readonly List<int> _segmentsOnPoint = new(4);
    private readonly List<Vector2> _activeLine = new();
    private Vector2 _lastDrawDirection = Vector2.Zero;
    private Vector2 _initialDrawDirection = Vector2.Zero;
    private int _startSegmentIndex = -1;

    public PlayerState State { get; set; } = PlayerState.OnPerimeter;
    public List<Vector2> ActiveLine => _activeLine;
    public int StartSegmentIndex => _startSegmentIndex;
    public Vector2 InitialDrawDirection => _initialDrawDirection;

    public event Action<int>? OnClaimTriggered;

    public void MoveArrow(double delta, float arrowSpeed, Vector2[] perimeter)
    {
        if (State == PlayerState.Won) return;
        if (arrow.State == Arrow.ArrowState.Stunned) return;

        var speed = arrowSpeed * (float)delta;
        var vx = Input.GetAxis("ui_left", "ui_right");
        var vy = Input.GetAxis("ui_up", "ui_down");

        if (vx == 0 && vy == 0) return;

        if (Math.Abs(vx) >= Math.Abs(vy)) vy = 0;
        else vx = 0;

        var velocity = new Vector2(vx, vy) * speed;
        var inputDir = new Vector2(vx, vy);
        var inputDirNorm = inputDir.Normalized();

        if (State == PlayerState.Drawing)
        {
            if (!inputDirNorm.IsEqualApprox(-_lastDrawDirection) && IsOnSelfLine())
            {
                return;
            }
        }

        arrow.SetDirection(inputDir);

        switch (State)
        {
            case PlayerState.OnPerimeter:
                ProcessOnPerimeter(velocity, inputDirNorm, vy, perimeter);
                break;
            case PlayerState.Drawing:
                ProcessDrawing(velocity, inputDirNorm, perimeter);
                break;
            case PlayerState.Won:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void CycleColor()
    {
        arrow.Cycle();
        if (State == PlayerState.Drawing) drawingLine.DefaultColor = arrow.CurrentEnergy.Line();
    }

    public void CancelDrawing()
    {
        State = PlayerState.OnPerimeter;
        _activeLine.Clear();
        drawingLine.ClearPoints();
    }

    public void ResetToStart()
    {
        if (_activeLine.Count > 0)
        {
            arrow.Position = _activeLine[0];
            if (_initialDrawDirection != Vector2.Zero) arrow.SetDirection(_initialDrawDirection);
        }

        CancelDrawing();
        arrow.Die();
    }

    private bool IsOnSelfLine()
    {
        if (_activeLine.Count < 4) return false;
        for (int i = 0; i < _activeLine.Count - 3; i++)
            if (GeometryUtils.GeometryUtils.DistanceToSegment(arrow.Position, _activeLine[i], _activeLine[i + 1]) < 0.05f)
                return true;

        return false;
    }

    private void ProcessOnPerimeter(Vector2 velocity, Vector2 inputDirNorm, float vy, Vector2[] perimeter)
    {
        Vector2 current = arrow.Position;
        UpdateSegmentsOnPoint(current, perimeter);
        bool movedOnPerimeter = false;

        for (int i = 0; i < _segmentsOnPoint.Count; i++)
        {
            int idx = _segmentsOnPoint[i];
            Vector2 a = perimeter[idx];
            Vector2 b = perimeter[idx + 1];
            Vector2 dir = (b - a).Normalized();

            bool isWallHorizontal = dir.Y == 0;
            bool isInputHorizontal = vy == 0;

            if (isWallHorizontal != isInputHorizontal) continue;

            Vector2 target = GeometryUtils.GeometryUtils.ClampPointToSegment(current + velocity, a, b);

            if (target.IsEqualApprox(current)) continue;

            arrow.Position = target;
            movedOnPerimeter = true;
            break;
        }

        if (movedOnPerimeter) return;

        Vector2 testPoint = current + inputDirNorm * 1.0f;
        if (!Geometry2D.IsPointInPolygon(testPoint, perimeter)) return;

        State = PlayerState.Drawing;
        _lastDrawDirection = inputDirNorm;
        _initialDrawDirection = inputDirNorm;
        _startSegmentIndex = _segmentsOnPoint[0];

        drawingLine.DefaultColor = arrow.CurrentEnergy.Line();

        _activeLine.Clear();
        _activeLine.Add(current);
        _activeLine.Add(current);

        drawingLine.ClearPoints();
        drawingLine.AddPoint(current);
        drawingLine.AddPoint(current);
    }

    private void ProcessDrawing(Vector2 velocity, Vector2 inputDirNorm, Vector2[] perimeter)
    {
        bool backtracking = inputDirNorm.IsEqualApprox(-_lastDrawDirection);

        if (backtracking)
            HandleBacktracking(velocity);
        else
            HandleAdvancing(velocity, inputDirNorm, perimeter);

        if (State != PlayerState.Drawing) return;

        _activeLine[^1] = arrow.Position;
        drawingLine.SetPointPosition(drawingLine.GetPointCount() - 1, arrow.Position);
    }

    private void HandleBacktracking(Vector2 velocity)
    {
        arrow.Position += velocity;
        Vector2 corner = _activeLine[^2];

        if (!((arrow.Position - corner).Dot(_lastDrawDirection) <= 0)) return;

        arrow.Position = corner;
        _activeLine.RemoveAt(_activeLine.Count - 1);
        drawingLine.RemovePoint(drawingLine.GetPointCount() - 1);

        if (_activeLine.Count == 1)
            CancelDrawing();
        else
        {
            Vector2 prevCorner = _activeLine[^2];
            _lastDrawDirection = (corner - prevCorner).Normalized();
        }
    }

    private void HandleAdvancing(Vector2 velocity, Vector2 inputDirNorm, Vector2[] perimeter)
    {
        Vector2 moveA = arrow.Position;
        Vector2 moveB = arrow.Position + velocity;
        Vector2? hitPoint = null;
        bool hitPerimeter = false;
        int hitSegmentIndex = -1;

        for (int i = 0; i < perimeter.Length - 1; i++)
        {
            var inter = GeometryUtils.GeometryUtils.SegmentIntersectsSegment(moveA, moveB, perimeter[i], perimeter[i + 1]);
            if (!inter.HasValue) continue;
            Vector2 pt = inter.Value;

            if (pt.DistanceTo(_activeLine[0]) <= 1.0f) continue;

            hitPoint = pt;
            hitPerimeter = true;
            hitSegmentIndex = i;
            break;
        }

        if (!hitPoint.HasValue && _activeLine.Count >= 4)
        {
            for (int i = 0; i < _activeLine.Count - 3; i++)
            {
                var inter = GeometryUtils.GeometryUtils.SegmentIntersectsSegment(moveA, moveB, _activeLine[i], _activeLine[i + 1]);
                if (!inter.HasValue) continue;
                hitPoint = inter.Value;
                break;
            }
        }

        if (hitPoint.HasValue)
        {
            arrow.Position = hitPoint.Value;

            if (!hitPerimeter) return;

            _activeLine[^1] = hitPoint.Value;
            OnClaimTriggered?.Invoke(hitSegmentIndex);
        }
        else
        {
            if (!inputDirNorm.IsEqualApprox(_lastDrawDirection))
            {
                _activeLine.Insert(_activeLine.Count - 1, arrow.Position);
                drawingLine.AddPoint(arrow.Position, drawingLine.GetPointCount() - 1);
                _lastDrawDirection = inputDirNorm;
            }

            arrow.Position += velocity;
        }
    }

    private void UpdateSegmentsOnPoint(Vector2 pt, Vector2[] perimeter)
    {
        _segmentsOnPoint.Clear();
        for (int i = 0; i < perimeter.Length - 1; i++)
            if (GeometryUtils.GeometryUtils.DistanceToSegment(pt, perimeter[i], perimeter[i + 1]) < 0.1f)
                _segmentsOnPoint.Add(i);
    }
}