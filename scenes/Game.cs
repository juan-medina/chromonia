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


    //////////////////////////////////////////////////////////////////////
    /// Helpers
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
        // our speed scale with passed time
        var speed = ArrowSpeed * (float)delta;

        // get axis input this is -1..1
        var vx = Input.GetAxis("ui_left", "ui_right");
        var vy = Input.GetAxis("ui_up", "ui_down");

        // if we are not moving return
        if (vx == 0 && vy == 0) return;

        // We can move only horizontal or vertically but not both
        // dominant axis wins; the other is discarded entirely
        if (Math.Abs(vx) >= Math.Abs(vy)) vy = 0;
        else vx = 0;

        // The user's input vector. We preserve its analog magnitude along the dominant axis.
        var velocity = new Vector2(vx, vy) * speed;
        var inputDir = new Vector2(vx, vy);
        Vector2 current = _arrow.Position;

        if (_playerState == PlayerState.OnPerimeter)
        {
            UpdateSegmentsOnPoint(current);
            bool movedOnPerimeter = false;

            foreach (var idx in _segmentsOnPoint)
            {
                Vector2 a = _perimeter[idx];
                Vector2 b = _perimeter[idx + 1];
                Vector2 dir = (b - a).Normalized();

                bool isWallHorizontal = dir.Y == 0;
                bool isInputHorizontal = vy == 0;

                // If input is not in the direction of the wall we slide
                if (isWallHorizontal != isInputHorizontal) continue;

                // clamp to the segment
                Vector2 target = ClampPointToSegment(current + velocity, a, b);

                // if the target is where we were, do nothing
                if (target.IsEqualApprox(current)) continue;

                _arrow.Position = target;
                movedOnPerimeter = true;
                break;
            }

            // If we didn't slide, the user is pushing AWAY from the wall.
            // Check if they are pushing INSIDE the safe area (unclaimed space).
            if (!movedOnPerimeter)
            {
                Vector2 testPoint = current + inputDir * 1.0f;
                if (Geometry2D.IsPointInPolygon(testPoint, _perimeter))
                {
                    // Break away!
                    _playerState = PlayerState.Drawing;
                    _lastDrawDirection = inputDir;
                    _activeLine.Clear();
                    _activeLine.Add(current); // Anchor point on the perimeter
                    _activeLine.Add(current); // The moving head of the line
                }
            }
        }

        // if we are not drawing a line we return

        if (_playerState != PlayerState.Drawing) return;

        // Did they turn?
        if (inputDir != _lastDrawDirection)
        {
            // Drop a new waypoint where they turned
            _activeLine.Add(_arrow.Position);
            _lastDrawDirection = inputDir;
        }

        // Move freely
        _arrow.Position += velocity;

        // Update the tip of the line to follow the arrow
        _activeLine[^1] = _arrow.Position;

        // Render the line
        _debugActiveLine.Points = _activeLine.ToArray();
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