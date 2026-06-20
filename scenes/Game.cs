// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Godot;
using Chromonia.Scripts;

namespace Chromonia.Scenes;

public partial class Game : Node2D
{
    //////////////////////////////////////////////////////////////////////
    /// Nodes
    [Export] private SubViewport _maskViewport = null!;

    [Export] private Node2D _maskRoot = null!;
    [Export] private Sprite2D _painting = null!;
    [Export] private Label _title = null!;
    [Export] private Label _artist = null!;
    [Export] private Sprite2D _arrow = null!;

    //////////////////////////////////////////////////////////////////////
    /// Globals
    private PaintingLibrary _library = null!;

    //////////////////////////////////////////////////////////////////////
    /// Constants
    private const int ViewportWidth = 1920;

    private const int ViewportHeight = 1080;
    private const float LabelPadding = 10f;
    private static readonly Color BorderColor = new(1, 0, 1);
    private const float BorderThickness = 9f;
    private const float ArrowSpeed = 200f;

    //////////////////////////////////////////////////////////////////////
    /// State
    private int _paintingWidth = ViewportWidth;

    private int _paintingHeight = ViewportHeight;
    private bool _revealed;
    private readonly List<LineSegment> _safeSegments = [];

    //////////////////////////////////////////////////////////////////////
    /// Overrides
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

        if (!_revealed)
        {
            _painting.Material = null;
            _title.Visible = true;
            _artist.Visible = true;
            _revealed = true;
        }
        else
        {
            _library.MoveNext();
            GetTree().ReloadCurrentScene();
        }
    }

    //////////////////////////////////////////////////////////////////////
    /// Helpers
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

        float scale = Math.Min((float)ViewportWidth / _paintingWidth, (float)ViewportHeight / _paintingHeight);
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

        UpdateSafeSegments();

        return (true, string.Empty);
    }

    private void CreateBorder(float width, float height, Color color, float size)
    {
        var verticalGap = new Vector2(0, size);
        var horizontalGap = new Vector2(size, 0);

        var topLeft = new Vector2(0, 0);
        var topRight = new Vector2(width, 0);
        var bottomLeft = new Vector2(0, height);
        var bottomRight = new Vector2(width, height);

        // Top, bottom, left, right strips
        _maskRoot.AddChild(new Polygon2D
            { Polygon = [topLeft, topRight, topRight + verticalGap, topLeft + verticalGap], Color = color });
        _maskRoot.AddChild(new Polygon2D
        {
            Polygon = [bottomLeft, bottomRight, bottomRight - verticalGap, bottomLeft - verticalGap], Color = color
        });
        _maskRoot.AddChild(new Polygon2D
            { Polygon = [topLeft, topLeft + horizontalGap, bottomLeft + horizontalGap, bottomLeft], Color = color });
        _maskRoot.AddChild(new Polygon2D
        {
            Polygon = [topRight, topRight - horizontalGap, bottomRight - horizontalGap, bottomRight], Color = color
        });
    }

    private void SetupArrow()
    {
        _arrow.SetPosition(new Vector2(0, _paintingHeight / 2f - BorderThickness));
    }

    private void MoveArrow(double delta)
    {
        var speed = ArrowSpeed * (float)delta;

        var vx = Input.GetAxis("ui_left", "ui_right");
        var vy = Input.GetAxis("ui_up", "ui_down");

        if (vx == 0 && vy == 0) return;

        // we can move only horizontal or vertically but not both
        // dominant axis wins; the other is discarded entirely
        if (Math.Abs(vx) >= Math.Abs(vy)) vy = 0;
        else vx = 0;

        var direction = new Vector2(vx, vy);
        var velocity = direction * speed;

        var pos = _arrow.GetPosition();
        
        _arrow.SetPosition(GetSnappedPosition(pos, velocity));
    }

    private void UpdateSafeSegments()
    {
        _safeSegments.Clear();
        float halfW = _paintingWidth / 2f;
        float halfH = _paintingHeight / 2f;

        float left = -halfW + BorderThickness;
        float right = halfW - BorderThickness;
        float top = -halfH + BorderThickness;
        float bottom = halfH - BorderThickness;

        _safeSegments.Add(new LineSegment(new Vector2(left, top), new Vector2(right, top), BorderThickness));
        _safeSegments.Add(new LineSegment(new Vector2(left, bottom), new Vector2(right, bottom), BorderThickness));
        _safeSegments.Add(new LineSegment(new Vector2(left, top), new Vector2(left, bottom), BorderThickness));
        _safeSegments.Add(new LineSegment(new Vector2(right, top), new Vector2(right, bottom), BorderThickness));
    }

    private Vector2 GetSnappedPosition(Vector2 current, Vector2 move)
    {
        float min = float.MaxValue;
        Vector2 possible = current + move;
        Vector2 best = possible;
        float tolerance = 0;

        foreach (var segment in _safeSegments)
        {
            Vector2 closest = segment.GetClosestPoint(possible);
            float dist = possible.DistanceTo(closest);
            if (!(dist < min)) continue;
            min = dist;
            best = closest;
            tolerance = segment.Tolerance;
        }

        return !(min <= tolerance) ? current : best;
    }

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Game Initialization Failed: {errorMessage}");
        OS.Alert("Something went wrong loading Chromonia.", "Initialization Error");
        GetTree().Quit();
    }
}