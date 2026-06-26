// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Godot;
using Chromonia.Scripts;

namespace Chromonia.Scenes;

public enum PlayerState
{
    OnPerimeter,
    Drawing,
    Won
}

public partial class Game : Node2D
{
    [Export] private SubViewport _maskViewport = null!;
    [Export] private Node2D _maskRoot = null!;
    [Export] private Sprite2D _painting = null!;
    [Export] private Label _title = null!;
    [Export] private Label _artist = null!;
    [Export] private Arrow _arrow = null!;
    [Export] private SharedProgressBar _progressBar = null!;
    private PaintingLibrary _library = null!;

    private const int ViewportWidth = 1920;
    private const int ViewportHeight = 1080;
    private const float LabelPadding = 10f;
    private static readonly Color BorderColor = new(0.75f, 2.25f, 0.75f);
    private const float BorderThickness = 5f;
    private const float ArrowSpeed = 300f;
    private const float TopMargin = 120f;
    private const float BottomMargin = 35f;
    private const float SideMargin = 25f;
    private const float AvailableWidth = ViewportWidth - (SideMargin * 2);
    private const float AvailableHeight = ViewportHeight - (TopMargin + BottomMargin);
    private const float RevealTime = 1.0F;

    private int _paintingWidth = ViewportWidth;
    private int _paintingHeight = ViewportHeight;
    private Vector2[] _perimeter = []; // represent all the points that create the safe perimeter of the safe area
    public Vector2[] Perimeter => _perimeter;
    private readonly List<int> _segmentsOnPoint = new(4); // Pre-allocated list to prevent GC pressure
    private Line2D _perimeterLine = null!;
    private PlayerState _playerState = PlayerState.OnPerimeter;
    private readonly List<Vector2> _activeLine = [];
    private Vector2 _lastDrawDirection = Vector2.Zero;
    private int _startSegmentIndex = -1;
    private Line2D _drawingLine = null!;
    private float _totalClaimedArea;
    private float _claimedAreaA;
    private float _claimedAreaB;
    private float _totalArea;

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

        if (_playerState == PlayerState.Won) GetTree().ReloadCurrentScene();

        _arrow.Cycle();

        if (_playerState == PlayerState.Drawing)
        {
            _drawingLine.DefaultColor = _arrow.CurrentEnergy.Line;
        }
    }


    private void Reveal()
    {
        _playerState = PlayerState.Won;

        // Kill all remaining blobs
        foreach (var child in _painting.GetChildren())
        {
            if (child is BlobEnemy blob) blob.QueueFree();
        }

        // Run all these animations simultaneously
        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_painting.Material, "shader_parameter/reveal_progress", 1.0f, RevealTime);
        tween.TweenProperty(_progressBar, "modulate:a", 0.0f, RevealTime / 3);

        // Calculate new scale and position as if TopMargin was 35f
        const float newAvailableHeight = ViewportHeight - (35f + BottomMargin);
        float newScale = Math.Min(AvailableWidth / _paintingWidth, newAvailableHeight / _paintingHeight);

        tween.TweenProperty(_painting, "scale", new Vector2(newScale, newScale), RevealTime);
        tween.TweenProperty(_painting, "position", Vector2.Zero, RevealTime); // (35 - 35) / 2 = 0

        // Wait for animations to finish before removing material
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => _painting.Material = null));

        _title.Visible = true;
        _artist.Visible = true;
        _arrow.Visible = false;
        _perimeterLine.Visible = false;
        _drawingLine.Visible = false;
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
        _totalArea = _paintingWidth * _paintingHeight;

        if (_paintingWidth <= 0 || _paintingHeight <= 0)
        {
            return (false, $"Invalid painting dimensions: {_paintingWidth}x{_paintingHeight}");
        }

        _painting.Texture = texture;

        float scale = Math.Min(AvailableWidth / _paintingWidth, AvailableHeight / _paintingHeight);
        _painting.Scale = new Vector2(scale, scale);

        float offsetY = (TopMargin - BottomMargin) / 2f;
        _painting.Position = new Vector2(0, offsetY);

        // Create a drop shadow using Godot's highly optimized StyleBoxFlat
        var shadowPanel = _painting.GetNodeOrNull<Panel>("DropShadow");
        if (shadowPanel == null)
        {
            shadowPanel = new Panel { Name = "DropShadow", ShowBehindParent = true };
            var styleBox = new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                ShadowColor = new Color(0, 0, 0, 0.7f),
                ShadowSize = 60,
                ShadowOffset = new Vector2(0, 30)
            };
            shadowPanel.AddThemeStyleboxOverride("panel", styleBox);
            _painting.AddChild(shadowPanel);
        }

        shadowPanel.Size = new Vector2(_paintingWidth, _paintingHeight);
        shadowPanel.Position = new Vector2(-_paintingWidth / 2f, -_paintingHeight / 2f);

        // Position labels in the top-left corner of the image in Sprite2D local space
        var topLeft = new Vector2(-_paintingWidth / 2f + LabelPadding, -_paintingHeight / 2f + LabelPadding);
        _title.Position = topLeft;
        _title.Text = $"{painting.Title} ({painting.Years})";
        _title.AddThemeColorOverride("font_color", new Color(1.5F, 1.5F, 1.5F));

        _artist.Position = topLeft + new Vector2(0, _title.Size.Y > 0 ? _title.Size.Y : 24f);
        _artist.Text = $"{painting.Artist} ({painting.Nationality})";
        _artist.AddThemeColorOverride("font_color", new Color(1.5F, 1.5F, 1.5F));

        // Size the viewport after setting scale/position
        _maskViewport.Size = new Vector2I(_paintingWidth, _paintingHeight);

        if (_painting.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("mask_texture", _maskViewport.GetTexture());
            material.SetShaderParameter("reveal_progress", 0.0f);
        }
        else
            HandleFatalError(
                "Painting material is not a ShaderMaterial. Please ensure the painting uses the correct shader.");

        // Create a border with a given color and thickness
        CreateBorder(_paintingWidth, _paintingHeight, BorderColor, BorderThickness);

        SpawnEnemies(_paintingWidth, _paintingHeight);

        return (true, string.Empty);
    }

    private void SpawnEnemies(float width, float height)
    {
        var bounds = new Rect2(-width / 2f, -height / 2f, width, height);

        for (int i = 0; i < 6; i++)
        {
            var tint = (i % 2 == 0) ? Energy.Tint.A : Energy.Tint.B;
            var blob = new BlobEnemy(tint, 250f);

            // Random start position within bounds (leaving some margin for the radius)
            float px = (float)GD.RandRange(bounds.Position.X + 70f, bounds.End.X - 70f);
            float py = (float)GD.RandRange(bounds.Position.Y + 70f, bounds.End.Y - 70f);
            blob.Position = new Vector2(px, py);
            blob.ZIndex = 2;

            _painting.AddChild(blob);
        }
    }

    private void CreateBorder(float width, float height, Color color, float thickness)
    {
        var halfWidth = width / 2f;
        var halfHeight = height / 2f;

        var topLeft = new Vector2(-halfWidth, -halfHeight);
        var topRight = new Vector2(halfWidth, -halfHeight);
        var bottomLeft = new Vector2(-halfWidth, halfHeight);
        var bottomRight = new Vector2(halfWidth, halfHeight);

        _perimeter = [topLeft, topRight, bottomRight, bottomLeft, topLeft];

        _perimeterLine = new Line2D
        {
            Points = [topLeft, topRight, bottomRight, bottomLeft],
            DefaultColor = color,
            Width = thickness,
            Closed = true,
            ZIndex = 1
        };
        _painting.AddChild(_perimeterLine);

        _drawingLine = new Line2D
        {
            DefaultColor = Colors.HotPink,
            Width = thickness,
            Closed = false,
            ZIndex = 1
        };
        _painting.AddChild(_drawingLine);

        var borderPhysics = new StaticBody2D { Name = "BorderPhysics" };
        for (int i = 0; i < _perimeter.Length - 1; i++)
        {
            var shape = new CollisionShape2D
            {
                Shape = new SegmentShape2D { A = _perimeter[i], B = _perimeter[i + 1] }
            };
            borderPhysics.AddChild(shape);
        }

        _painting.AddChild(borderPhysics);
    }

    private void SetupArrow()
    {
        // Start exactly on the bottom segment at x=0, since we have a center camera this is screen center
        _arrow.SetPosition(new Vector2(0, _paintingHeight / 2f));
        _arrow.ZIndex = 2;
    }

    private void MoveArrow(double delta)
    {
        if (_playerState == PlayerState.Won) return;

        var speed = ArrowSpeed * (float)delta;
        var vx = Input.GetAxis("ui_left", "ui_right");
        var vy = Input.GetAxis("ui_up", "ui_down");

        if (vx == 0 && vy == 0) return;

        if (Math.Abs(vx) >= Math.Abs(vy)) vy = 0;
        else vx = 0;

        var velocity = new Vector2(vx, vy) * speed;
        var inputDir = new Vector2(vx, vy);

        if (_playerState == PlayerState.Drawing)
            if (inputDir != -_lastDrawDirection && IsOnSelfLine())
                return;

        _arrow.SetDirection(inputDir);

        switch (_playerState)
        {
            case PlayerState.OnPerimeter:
                ProcessOnPerimeter(velocity, inputDir, vy);
                break;
            case PlayerState.Drawing:
                ProcessDrawing(velocity, inputDir);
                break;
            case PlayerState.Won:
                // Do nothing, there is no visible arrow
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool IsOnSelfLine()
    {
        if (_activeLine.Count < 4) return false;
        for (int i = 0; i < _activeLine.Count - 3; i++)
        {
            if (GeometryUtils.DistanceToSegment(_arrow.Position, _activeLine[i], _activeLine[i + 1]) < 0.05f)
            {
                return true;
            }
        }

        return false;
    }

    private void ProcessOnPerimeter(Vector2 velocity, Vector2 inputDir, float vy)
    {
        Vector2 current = _arrow.Position;
        UpdateSegmentsOnPoint(current);
        bool movedOnPerimeter = false;

        for (int i = 0; i < _segmentsOnPoint.Count; i++)
        {
            int idx = _segmentsOnPoint[i];
            Vector2 a = _perimeter[idx];
            Vector2 b = _perimeter[idx + 1];
            Vector2 dir = (b - a).Normalized();

            bool isWallHorizontal = dir.Y == 0;
            bool isInputHorizontal = vy == 0;

            if (isWallHorizontal != isInputHorizontal) continue;

            Vector2 target = GeometryUtils.ClampPointToSegment(current + velocity, a, b);

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

        _drawingLine.DefaultColor = _arrow.CurrentEnergy.Line;

        _activeLine.Clear();
        _activeLine.Add(current);
        _activeLine.Add(current);

        _drawingLine.ClearPoints();
        _drawingLine.AddPoint(current);
        _drawingLine.AddPoint(current);
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
        _drawingLine.SetPointPosition(_drawingLine.GetPointCount() - 1, _arrow.Position);
    }

    private void HandleBacktracking(Vector2 velocity)
    {
        _arrow.Position += velocity;
        Vector2 corner = _activeLine[^2];

        if (!((_arrow.Position - corner).Dot(_lastDrawDirection) <= 0)) return;

        _arrow.Position = corner;
        _activeLine.RemoveAt(_activeLine.Count - 1);
        _drawingLine.RemovePoint(_drawingLine.GetPointCount() - 1);

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
            ProcessClaim(hitSegmentIndex);
        }
        else
        {
            if (inputDir != _lastDrawDirection)
            {
                _activeLine.Insert(_activeLine.Count - 1, _arrow.Position);
                _drawingLine.AddPoint(_arrow.Position, _drawingLine.GetPointCount() - 1);
                _lastDrawDirection = inputDir;
            }

            _arrow.Position += velocity;
        }
    }

    private void ProcessClaim(int hitSegmentIndex)
    {
        var activeArray = _activeLine.ToArray();

        var (claimedPoly, newPerimeter, claimedArea) = DetermineClaimedPolygon(hitSegmentIndex, activeArray);

        // Pass the mathematical polygon through Godot's Clipper library (OffsetPolygon with delta 0).
        // This is the standard "Godot way" to instantly eliminate self-intersections and duplicate
        // points that cause "Convex decomposing failed".
        var cleanPolys = Geometry2D.OffsetPolygon(claimedPoly, 0);
        if (cleanPolys.Count > 0) claimedPoly = cleanPolys[0];

        if (!DestroyTrappedBlobs(claimedPoly))
        {
            // Player "died" due to trapping wrong color. Destroy line and cancel.
            KillPlayer();
            return;
        }

        ApplyNewPerimeter(newPerimeter);
        CreateClaimVisuals(claimedPoly);
        CreateClaimPhysics(claimedPoly);

        CancelDrawing();
        UpdateProgressAndCheckWin(claimedArea);
    }

    private (Vector2[] ClaimedPoly, Vector2[] NewPerimeter, float ClaimedArea) DetermineClaimedPolygon(
        int hitSegmentIndex, Vector2[] activeArray)
    {
        var poly1 = GeometryUtils.BuildPolygon(_perimeter, _startSegmentIndex, hitSegmentIndex, activeArray,
            forward: true);
        var poly2 = GeometryUtils.BuildPolygon(_perimeter, _startSegmentIndex, hitSegmentIndex, activeArray,
            forward: false);

        float area1 = GeometryUtils.GetPolygonArea(poly1);
        float area2 = GeometryUtils.GetPolygonArea(poly2);

        // The smaller area is claimed, the larger area becomes the new safe perimeter
        return area1 < area2 ? (poly1, poly2, area1) : (poly2, poly1, area2);
    }

    private void ApplyNewPerimeter(Vector2[] newPerimeter)
    {
        var newPerimeterList = new List<Vector2>(newPerimeter) { newPerimeter[0] };
        _perimeter = newPerimeterList.ToArray();
        _perimeterLine.Points = newPerimeter;
    }

    private bool DestroyTrappedBlobs(Vector2[] claimedPoly)
    {
        bool lethalTrapFound = false;
        var trappedBlobs = new List<BlobEnemy>();

        foreach (var child in _painting.GetChildren())
        {
            if (child is BlobEnemy blob && Geometry2D.IsPointInPolygon(blob.Position, claimedPoly))
            {
                // Polarity rules: Trapping the SAME color is lethal. You can only destroy OPPOSITE colors.
                if (blob.BlobEnergy.CurrentTint == _arrow.CurrentEnergy.CurrentTint)
                {
                    lethalTrapFound = true;
                    break;
                }

                trappedBlobs.Add(blob);
            }
        }

        if (lethalTrapFound)
        {
            return false;
        }

        foreach (var blob in trappedBlobs)
        {
            blob.QueueFree();
        }

        return true;
    }

    private void CreateClaimVisuals(Vector2[] claimedPoly)
    {
        Polygon2D claimNode = new Polygon2D
        {
            Polygon = ConvertToMaskCoordinates(claimedPoly),
            Color = _arrow.CurrentEnergy.Fill with { A = 0.0f }
        };
        _maskRoot.AddChild(claimNode);

        var tween = CreateTween();
        tween.TweenProperty(claimNode, "color:a", 1.0f, 0.5f);
    }

    private void CreateClaimPhysics(Vector2[] claimedPoly)
    {
        var claimPhysics = new StaticBody2D();
        var collisionPoly = new CollisionPolygon2D { Polygon = claimedPoly };
        claimPhysics.AddChild(collisionPoly);
        _painting.AddChild(claimPhysics);
    }

    private void UpdateProgressAndCheckWin(float claimedArea)
    {
        if (_arrow.CurrentEnergy.Primary)
            _claimedAreaA += claimedArea;
        else
            _claimedAreaB += claimedArea;

        _totalClaimedArea = _claimedAreaA + _claimedAreaB;

        _progressBar.UpdateProgress(_claimedAreaA / _totalArea, _claimedAreaB / _totalArea);

        // Win condition: both colors must reach the 35% goal
        if (_claimedAreaA / _totalArea >= 0.35f && _claimedAreaB / _totalArea >= 0.35f)
            Reveal();
    }

    private void CancelDrawing()
    {
        _playerState = PlayerState.OnPerimeter;
        _activeLine.Clear();
        _drawingLine.ClearPoints();
    }

    private void KillPlayer()
    {
        if (_activeLine.Count > 0)
        {
            _arrow.Position = _activeLine[0]; // Snap back to where drawing started
        }

        CancelDrawing();

        if (!_arrow.IsImmune)
        {
            _arrow.Die();
        }
    }


    private Vector2[] ConvertToMaskCoordinates(Vector2[] poly)
    {
        var shift = new Vector2(_paintingWidth / 2f, _paintingHeight / 2f);
        var shifted = new Vector2[poly.Length];
        for (int i = 0; i < poly.Length; i++)
            shifted[i] = poly[i] + shift;
        return shifted;
    }

    private void UpdateSegmentsOnPoint(Vector2 pt)
    {
        _segmentsOnPoint.Clear();
        for (int i = 0; i < _perimeter.Length - 1; i++)
            if (GeometryUtils.DistanceToSegment(pt, _perimeter[i], _perimeter[i + 1]) < 0.1f)
                _segmentsOnPoint.Add(i);
    }


    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Game Initialization Failed: {errorMessage}");
        OS.Alert("Something went wrong loading Chromonia.", "Initialization Error");
        GetTree().Quit();
    }
}