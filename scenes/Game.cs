// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;
using Chromonia.Scripts;

namespace Chromonia.Scenes;

public partial class Game : Node2D
{
    //////////////////////////////////////////////////////////////////////
    /// Nodes
    private SubViewport _maskViewport = null!;

    private PaintingLibrary _library = null!;
    private Node2D _maskRoot = null!;

    private Sprite2D _painting = null!;
    private Label _title = null!;
    private Label _artist = null!;

    private Sprite2D _arrow = null!;

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

    //////////////////////////////////////////////////////////////////////
    /// Overrides
    ///
    public override void _Ready()
    {
        if (SetupNodes() && LoadCurrentPainting())
        {
            SetupArrow();
            return;
        }

        GD.PrintErr("Game: initialization failed, quitting");
        OS.Alert("Something went wrong loading the game.", "Chromonia - Error");
        GetTree().Quit();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        MoveArrow(delta);
        Input.IsActionJustPressed("ui_accept");
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
    private bool SetupNodes()
    {
        _painting = GetNodeOrNull<Sprite2D>("Painting");
        if (_painting is null)
        {
            GD.PrintErr("Game: Painting node not found in scene tree");
            return false;
        }

        _maskViewport = GetNodeOrNull<SubViewport>("SubViewport");
        if (_maskViewport is null)
        {
            GD.PrintErr("Game: MaskViewport not found");
            return false;
        }

        _maskRoot = _maskViewport.GetNodeOrNull<Node2D>("MaskRoot");
        if (_maskRoot is null)
        {
            GD.PrintErr("Game: MaskRoot not found");
            return false;
        }

        _title = GetNodeOrNull<Label>("Painting/Title");
        if (_title is null)
        {
            GD.PrintErr("Game: Title label not found in scene tree");
            return false;
        }

        _artist = GetNodeOrNull<Label>("Painting/Artist");
        if (_artist is null)
        {
            GD.PrintErr("Game: Artist label not found in scene tree");
            return false;
        }

        _library = GetNodeOrNull<PaintingLibrary>("/root/PaintingLibrary");
        if (_library is null)
        {
            GD.PrintErr("Game: PaintingLibrary autoload not found");
            return false;
        }

        _arrow = GetNodeOrNull<Sprite2D>("Painting/Arrow");

        if (_arrow is not null) return true;
        GD.PrintErr("Game: Arrow node not found in scene tree");
        return false;
    }

    private bool LoadPainting(PaintingEntry painting)
    {
        var texture = PaintingLibrary.LoadTexture(painting);
        if (texture is null)
        {
            GD.PrintErr("Game: LoadTexture failed");
            return false;
        }

        _paintingWidth = texture.GetWidth();
        _paintingHeight = texture.GetHeight();

        if (_paintingWidth <= 0 || _paintingHeight <= 0)
        {
            GD.PrintErr($"Game: invalid painting dimensions: {_paintingWidth}x{_paintingHeight}");
            return false;
        }

        _painting.Texture = texture;

        float scale = Math.Min((float)ViewportWidth / _paintingWidth, (float)ViewportHeight / _paintingHeight);
        _painting.Scale = new Vector2(scale, scale);
        _painting.Position = Vector2.Zero;

        // position labels in the top-left corner of the image in Sprite2D local space
        var topLeft = new Vector2(-_paintingWidth / 2f + LabelPadding, -_paintingHeight / 2f + LabelPadding);
        _title.Position = topLeft;
        _title.Text = $"{painting.Title} ({painting.Years})";

        _artist.Position = topLeft + new Vector2(0, _title.Size.Y > 0 ? _title.Size.Y : 24f);
        _artist.Text = $"{painting.Artist} ({painting.Nationality})";

        // at the end of LoadPainting, after setting scale/position:
        _maskViewport.Size = new Vector2I(_paintingWidth, _paintingHeight);

        var material = (ShaderMaterial)_painting.Material;
        material.SetShaderParameter("mask_texture", _maskViewport.GetTexture());

        // create a border with a giving color and thickness
        CreateBorder(_paintingWidth, _paintingHeight, BorderColor, BorderThickness);

        return true;
    }

    private void CreateBorder(float width, float height, Color color, float size)
    {
        var verticalGap = new Vector2(0, size);
        var horizontalGap = new Vector2(size, 0);

        var topLeft = new Vector2(0, 0);
        var topRight = new Vector2(width, 0);
        var bottomLeft = new Vector2(0, height);
        var bottomRight = new Vector2(width, height);

        // top, bottom, left, right strips
        _maskRoot.AddChild(new Polygon2D
            { Polygon = [topLeft, topRight, topRight + verticalGap, topLeft + verticalGap], Color = color });
        _maskRoot.AddChild(
            new Polygon2D
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

    private bool LoadCurrentPainting()
    {
        var painting = _library.Current();
        if (painting is not null) return LoadPainting(painting);
        GD.PrintErr("Game: could not get current painting from library");
        return false;
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
        var direction = new Vector2(vx, vy);
        var velocity = direction * speed;

        _arrow.SetPosition(_arrow.GetPosition() + velocity);
    }
}