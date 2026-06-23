// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Chromonia.Scripts;

namespace Chromonia.Scenes;

public enum PlayerState
{
    OnPerimeter,
    Drawing
}

public partial class Game : Node2D
{
    [Export] private SubViewport _maskViewport = null!;
    [Export] private Node2D _maskRoot = null!;
    [Export] private Sprite2D _painting = null!;
    [Export] private Label _title = null!;
    [Export] private Label _artist = null!;
    [Export] private Arrow _arrow = null!;
    private PaintingLibrary _library = null!;

    private const int ViewportWidth = 1920;
    private const int ViewportHeight = 1080;
    private const float LabelPadding = 10f;
    private static readonly Color BorderColor = new(0.25f, 0.55f, 0.3f);
    private const float BorderThickness = 5f;
    private const float ArrowSpeed = 300f;

    private int _paintingWidth = ViewportWidth;
    private int _paintingHeight = ViewportHeight;
    private Vector2[] _perimeter = []; // represent all the points that create the safe perimeter of the safe area
    private readonly List<int> _segmentsOnPoint = new(4); // Pre-allocated list to prevent GC pressure
    private Line2D _debugPerimeterLine = null!;
    private PlayerState _playerState = PlayerState.OnPerimeter;
    private readonly List<Vector2> _activeLine = new();
    private Vector2 _lastDrawDirection = Vector2.Zero;
    private int _startSegmentIndex = -1;
    private Line2D _debugActiveLine = null!;

    public override void _Ready()
    {
        // 1. Resolve Autoload explicitly
        _library = GetNodeOrNull<PaintingLibrary>("/root/PaintingLibrary");
        if (_library is null)
        {
            HandleFatalError("PaintingLibrary global autoload is missing.");
            return;
        }

        // 2. Load game data using explicit value checking
        var (success, error) = TryLoadCurrentPainting();
        if (!success)
        {
            HandleFatalError(error);
            return;
        }

        SetupArrow();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        MoveArrow(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel")) GetTree().Quit();
        if (!@event.IsActionPressed("ui_accept")) return;
        _arrow.Cycle();
    }


    private void Reveal()
    {
        _painting.Material = null;
        _title.Visible = true;
        _artist.Visible = true;
    }


    private (bool Success, string Error) TryLoadCurrentPainting()
    {
        var (painting, err) = _library.Current();
        return !err.Success ? (false, err.Message) : TryLoadPainting(painting!);
    }

    private (bool Success, string Error) TryLoadPainting(PaintingEntry painting)
    {
        var (texture, texErr) = PaintingLibrary.LoadTexture(painting);
        if (!texErr.Success)
        {
            return (false, texErr.Message);
        }

        _paintingWidth = texture!.GetWidth();
        _paintingHeight = texture.GetHeight();

        if (_paintingWidth <= 0 || _paintingHeight <= 0)
        {
            return (false, $"Invalid painting dimensions: {_paintingWidth}x{_paintingHeight}");
        }

        _painting.Texture = texture;

        float scale = Math.Min((float)(ViewportWidth - 20) / _paintingWidth,
            (float)(ViewportHeight - 20) / _paintingHeight);
        _painting.Scale = new Vector2(scale, scale);
        _painting.Position = Vector2.Zero;

        // Position labels in the top-left corner of the image in Sprite2D local space
        var topLeft = new Vector2(-_paintingWidth / 2f + LabelPadding, -_paintingHeight / 2f + LabelPadding);
        _title.Position = topLeft;
        _title.Text = $"{painting.Title} ({painting.Years})";

        _artist.Position = topLeft + new Vector2(0, _title.Size.Y > 0 ? _title.Size.Y : 24f);
        _artist.Text = $"{painting.Artist} ({painting.Nationality})";

        // Size the viewport after setting scale/position
        _maskViewport.Size = new Vector2I(_paintingWidth, _paintingHeight);

        var material = (ShaderMaterial)_painting.Material;
        material.SetShaderParameter("mask_texture", _maskViewport.GetTexture());

        // Create a border with a given color and thickness
        CreateBorder(_paintingWidth, _paintingHeight, BorderColor, BorderThickness);

        return (true, string.Empty);
    }

    private void CreateBorder(float width, float height, Color color, float thickness)
    {
        var halfWidth = width / 2f;
        var halfHeight = height / 2f;

        var topLeft = new Vector2(-halfWidth, -halfHeight);
        var topRight = new Vector2(halfWidth, -halfHeight);
        var bottomLeft = new Vector2(-halfWidth, halfHeight);
        var bottomRight = new Vector2(halfWidth, halfHeight);

        _perimeter =
        [
            topLeft, topRight, bottomRight, bottomLeft, topLeft
        ];

        // Create debug line to visualize math. We strip the 5th point and let Godot close the loop for perfect corners.
        _debugPerimeterLine = new Line2D
        {
            Points = _perimeter.Take(_perimeter.Length - 1).ToArray(),
            DefaultColor = color,
            Width = thickness,
            Closed = true,
            ZIndex = 1
        };
        _painting.AddChild(_debugPerimeterLine);

        _debugActiveLine = new Line2D
        {
            DefaultColor = Colors.HotPink,
            Width = thickness,
            Closed = false,
            ZIndex = 1
        };
        _painting.AddChild(_debugActiveLine);
    }

    private void SetupArrow()
    {
        // Start exactly on the bottom segment at x=0, since we have a center camera this is screen center
        _arrow.SetPosition(new Vector2(0, _paintingHeight / 2f));
        _arrow.ZIndex = 2;
    }

    private void MoveArrow(double delta)
    {
        var speed = ArrowSpeed * (float)delta;
        var vx = Input.GetAxis("ui_left", "ui_right");
        var vy = Input.GetAxis("ui_up", "ui_down");

        if (vx == 0 && vy == 0) return;

        if (Math.Abs(vx) >= Math.Abs(vy)) vy = 0;
        else vx = 0;

        var velocity = new Vector2(vx, vy) * speed;
        var inputDir = new Vector2(vx, vy);

        switch (_playerState)
        {
            case PlayerState.OnPerimeter:
                ProcessOnPerimeter(velocity, inputDir, vy);
                break;
            case PlayerState.Drawing:
                ProcessDrawing(velocity, inputDir);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ProcessOnPerimeter(Vector2 velocity, Vector2 inputDir, float vy)
    {
        Vector2 current = _arrow.Position;
        UpdateSegmentsOnPoint(current);
        bool movedOnPerimeter = false;

        foreach (var idx in _segmentsOnPoint)
        {
            Vector2 a = _perimeter[idx];
            Vector2 b = _perimeter[idx + 1];
            Vector2 dir = (b - a).Normalized();

            bool isWallHorizontal = dir.Y == 0;
            bool isInputHorizontal = vy == 0;

            if (isWallHorizontal != isInputHorizontal) continue;

            Vector2 target = ClampPointToSegment(current + velocity, a, b);

            if (target.IsEqualApprox(current)) continue;

            _arrow.Position = target;
            movedOnPerimeter = true;
            break;
        }

        if (movedOnPerimeter) return;

        Vector2 testPoint = current + inputDir * 1.0f;
        if (!Geometry2D.IsPointInPolygon(testPoint, _perimeter)) return;

        _playerState = PlayerState.Drawing;
        _lastDrawDirection = inputDir;
        _startSegmentIndex = _segmentsOnPoint[0];

        _activeLine.Clear();
        _activeLine.Add(current);
        _activeLine.Add(current);

        _debugActiveLine.ClearPoints();
        _debugActiveLine.AddPoint(current);
        _debugActiveLine.AddPoint(current);
    }

    private void ProcessDrawing(Vector2 velocity, Vector2 inputDir)
    {
        bool backtracking = inputDir == -_lastDrawDirection;

        if (backtracking)
            HandleBacktracking(velocity);
        else
            HandleAdvancing(velocity, inputDir);

        if (_playerState != PlayerState.Drawing) return;

        _activeLine[^1] = _arrow.Position;
        _debugActiveLine.SetPointPosition(_debugActiveLine.GetPointCount() - 1, _arrow.Position);
    }

    private void HandleBacktracking(Vector2 velocity)
    {
        _arrow.Position += velocity;
        Vector2 corner = _activeLine[^2];

        if (!((_arrow.Position - corner).Dot(_lastDrawDirection) <= 0)) return;

        _arrow.Position = corner;
        _activeLine.RemoveAt(_activeLine.Count - 1);
        _debugActiveLine.RemovePoint(_debugActiveLine.GetPointCount() - 1);

        if (_activeLine.Count == 1)
            CancelDrawing();
        else
        {
            Vector2 prevCorner = _activeLine[^2];
            _lastDrawDirection = (corner - prevCorner).Normalized();
        }
    }

    private void HandleAdvancing(Vector2 velocity, Vector2 inputDir)
    {
        Vector2 moveA = _arrow.Position;
        Vector2 moveB = _arrow.Position + velocity;
        Vector2? hitPoint = null;
        bool hitPerimeter = false;
        int hitSegmentIndex = -1;

        for (int i = 0; i < _perimeter.Length - 1; i++)
        {
            var inter = Geometry2D.SegmentIntersectsSegment(moveA, moveB, _perimeter[i], _perimeter[i + 1]);
            if (inter.VariantType == Variant.Type.Nil) continue;
            Vector2 pt = inter.AsVector2();
            
            // Ignore collision with our own starting anchor to prevent instant termination
            if (pt.DistanceTo(_activeLine[0]) <= 1.0f) continue;
            
            hitPoint = pt;
            hitPerimeter = true;
            hitSegmentIndex = i;
            break;
        }

        // Check self-collision against tail. Ignore the 3 most recent segments to prevent trivial vertex intersections.
        if (!hitPoint.HasValue && _activeLine.Count >= 4)
        {
            for (int i = 0; i < _activeLine.Count - 3; i++)
            {
                var inter = Geometry2D.SegmentIntersectsSegment(moveA, moveB, _activeLine[i], _activeLine[i + 1]);
                if (inter.VariantType == Variant.Type.Nil) continue;
                hitPoint = inter.AsVector2();
                break;
            }
        }

        if (hitPoint.HasValue)
        {
            _arrow.Position = hitPoint.Value;

            if (!hitPerimeter) return;
            
            _activeLine[^1] = hitPoint.Value;
            var activeArray = _activeLine.ToArray();
            
            var poly1 = BuildForwardPolygon(_startSegmentIndex, hitSegmentIndex, activeArray);
            var poly2 = BuildBackwardPolygon(_startSegmentIndex, hitSegmentIndex, activeArray);
            
            float area1 = GetPolygonArea(poly1);
            float area2 = GetPolygonArea(poly2);
            
            // The smaller area is claimed, the larger area becomes the new safe perimeter
            Vector2[] newPerimeter = area1 < area2 ? poly2 : poly1;
            
            var newPerimList = new List<Vector2>(newPerimeter) { newPerimeter[0] };
            _perimeter = newPerimList.ToArray();
            
            _debugPerimeterLine.Points = _perimeter.Take(_perimeter.Length - 1).ToArray();
            
            CancelDrawing();
        }
        else
        {
            if (inputDir != _lastDrawDirection)
            {
                _activeLine.Insert(_activeLine.Count - 1, _arrow.Position);
                _debugActiveLine.AddPoint(_arrow.Position, _debugActiveLine.GetPointCount() - 1);
                _lastDrawDirection = inputDir;
            }

            _arrow.Position += velocity;
        }
    }

    private void CancelDrawing()
    {
        _playerState = PlayerState.OnPerimeter;
        _activeLine.Clear();
        _debugActiveLine.ClearPoints();
    }

    private Vector2[] BuildForwardPolygon(int startSeg, int endSeg, Vector2[] activeLine)
    {
        int totalUnique = _perimeter.Length - 1;
        var poly = new List<Vector2>(activeLine);

        if (startSeg == endSeg && _perimeter[startSeg].DistanceSquaredTo(activeLine[0]) >= _perimeter[startSeg].DistanceSquaredTo(activeLine[^1]))
            return poly.ToArray();

        int curr = (endSeg + 1) % totalUnique;
        while (true)
        {
            poly.Add(_perimeter[curr]);
            if (curr == startSeg) break;
            curr = (curr + 1) % totalUnique;
        }

        return poly.ToArray();
    }

    private Vector2[] BuildBackwardPolygon(int startSeg, int endSeg, Vector2[] activeLine)
    {
        int totalUnique = _perimeter.Length - 1;
        var poly = new List<Vector2>(activeLine);

        if (startSeg == endSeg && _perimeter[startSeg].DistanceSquaredTo(activeLine[0]) < _perimeter[startSeg].DistanceSquaredTo(activeLine[^1]))
            return poly.ToArray();

        int curr = endSeg;
        while (true)
        {
            poly.Add(_perimeter[curr]);
            if (curr == (startSeg + 1) % totalUnique) break;
            curr = (curr - 1 + totalUnique) % totalUnique;
        }

        return poly.ToArray();
    }

    private static float GetPolygonArea(Vector2[] polygon)
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



    private void UpdateSegmentsOnPoint(Vector2 pt)
    {
        _segmentsOnPoint.Clear();
        for (int i = 0; i < _perimeter.Length - 1; i++)
            if (DistanceToSegment(pt, _perimeter[i], _perimeter[i + 1]) < 0.1f)
                _segmentsOnPoint.Add(i);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq == 0) return (p - a).Length();
        float t = Math.Clamp((p - a).Dot(ab) / lengthSq, 0f, 1f);
        return (p - (a + t * ab)).Length();
    }

    private static Vector2 ClampPointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq == 0) return a;
        float t = Math.Clamp((p - a).Dot(ab) / lengthSq, 0f, 1f);
        return a + t * ab;
    }

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Game Initialization Failed: {errorMessage}");
        OS.Alert("Something went wrong loading Chromonia.", "Initialization Error");
        GetTree().Quit();
    }
}