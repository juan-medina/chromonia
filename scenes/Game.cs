// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;
using Chromonia.Scripts;

namespace Chromonia.Scenes;

public partial class Game : Node2D
{
    private const float ViewportWidth = 1920f;
    private const float ViewportHeight = 1080f;

    private const float LabelPadding = 10f;

    private static readonly Color BorderColor = new(1, 0, 1);
    private const float BorderThickness = 9f;

    private Sprite2D? _painting;
    private SubViewport? _maskViewport;
    private Node2D? _maskRoot;
    private PaintingLibrary? _library;
    private Label? _title;
    private Label? _artist;
    private bool _revealed;

    public override void _Ready()
    {
        _painting = GetNodeOrNull<Sprite2D>("Painting");
        if (_painting is null)
        {
            GD.PrintErr("Game: Painting node not found in scene tree");
            return;
        }

        _maskViewport = GetNodeOrNull<SubViewport>("SubViewport");
        if (_maskViewport is null)
        {
            GD.PrintErr("Game: MaskViewport not found");
            return;
        }

        _maskRoot = _maskViewport.GetNodeOrNull<Node2D>("MaskRoot");
        if (_maskRoot is null)
        {
            GD.PrintErr("Game: MaskRoot not found");
            return;
        }

        _title = GetNodeOrNull<Label>("Painting/Title");
        if (_title is null)
        {
            GD.PrintErr("Game: Title label not found in scene tree");
            return;
        }

        _artist = GetNodeOrNull<Label>("Painting/Artist");
        if (_artist is null)
        {
            GD.PrintErr("Game: Artist label not found in scene tree");
            return;
        }

        _library = GetNodeOrNull<PaintingLibrary>("/root/PaintingLibrary");
        if (_library is null)
        {
            GD.PrintErr("Game: PaintingLibrary autoload not found");
            return;
        }

        LoadCurrentPainting();
    }


    private void LoadPainting(PaintingEntry painting)
    {
        if (_painting is null || _title is null || _artist is null)
        {
            GD.PrintErr("Game: scene nodes or PaintingLibrary not ready");
            return;
        }

        if (_maskViewport is null)
        {
            GD.PrintErr("Game: MaskViewport not found");
            return;
        }

        var texture = PaintingLibrary.LoadTexture(painting);
        if (texture is null)
        {
            GD.PrintErr("Game: LoadTexture failed");
            return;
        }

        var paintingWidth = texture.GetWidth();
        var paintingHeight = texture.GetHeight();
        if (paintingWidth <= 0 || paintingHeight <= 0)
        {
            GD.PrintErr($"Game: invalid painting dimensions: {paintingWidth}x{paintingHeight}");
            return;
        }

        _painting.Texture = texture;

        float scale = Math.Min(ViewportWidth / paintingWidth, ViewportHeight / paintingHeight);
        _painting.Scale = new Vector2(scale, scale);
        _painting.Position = Vector2.Zero;

        // position labels in the top-left corner of the image in Sprite2D local space
        var topLeft = new Vector2(-paintingWidth / 2f + LabelPadding, -paintingHeight / 2f + LabelPadding);
        _title.Position = topLeft;
        _title.Text = $"{painting.Title} ({painting.Years})";

        _artist.Position = topLeft + new Vector2(0, _title.Size.Y > 0 ? _title.Size.Y : 24f);
        _artist.Text = $"{painting.Artist} ({painting.Nationality})";

        // at the end of LoadPainting, after setting scale/position:
        _maskViewport.Size = new Vector2I(paintingWidth, paintingHeight);

        var material = (ShaderMaterial)_painting.Material;
        material.SetShaderParameter("mask_texture", _maskViewport.GetTexture());

        // create a border with a giving color and thickness
        CreateBorder(paintingWidth, paintingHeight, BorderColor, BorderThickness);
    }

    private void CreateBorder(float width, float height, Color color, float size)
    {
        if (_maskRoot is null)
            return;

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

    private void LoadCurrentPainting()
    {
        if (_library is null)
        {
            GD.PrintErr("Game: PaintingLibrary not ready");
            return;
        }

        var painting = _library.Current();
        if (painting is null)
        {
            GD.PrintErr("Game: could not get current painting from library");
            return;
        }

        LoadPainting(painting);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_accept"))
            return;

        if (!_revealed)
        {
            if (_painting is null || _title is null || _artist is null)
                return;

            _painting.Material = null;
            _title.Visible = true;
            _artist.Visible = true;
            _revealed = true;
        }
        else
        {
            if (_library is null)
            {
                GD.PrintErr("Game: PaintingLibrary not ready");
                return;
            }

            _library.MoveNext();
            GetTree().ReloadCurrentScene();
        }
    }
}