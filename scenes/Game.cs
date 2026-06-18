// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Chromonia.Scripts;

namespace Chromonia.Scenes;

public partial class Game : Node2D
{
    private const float ViewportWidth = 1920f;
    private const float ViewportHeight = 1080f;

    private const float LabelPadding = 10f;

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
            GD.PrintErr("Game: PaintingLibrary autoload not found — is it registered in Project Settings?");
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

        _painting.Texture = texture;

        float scale = Math.Min(ViewportWidth / painting.Width, ViewportHeight / painting.Height);
        _painting.Scale = new Vector2(scale, scale);
        _painting.Position = Vector2.Zero;

        // position labels at the top-left corner of the image in Sprite2D local space
        var topLeft = new Vector2(-painting.Width / 2f + LabelPadding, -painting.Height / 2f + LabelPadding);
        _title.Position = topLeft;
        _title.Text = $"{painting.Title} ({painting.Years})";

        _artist.Position = topLeft + new Vector2(0, _title.Size.Y > 0 ? _title.Size.Y : 24f);
        _artist.Text = $"{painting.Artist} ({painting.Nationality})";

        // at the end of LoadPainting, after setting scale/position:
        _maskViewport.Size = new Vector2I((int)painting.Width, (int)painting.Height);

        var material = (ShaderMaterial)_painting.Material;
        material.SetShaderParameter("mask_texture", _maskViewport.GetTexture());

        DrawMaskBorder(painting.Width, painting.Height);
    }

    private const float BorderThickness = 9f;
    private static readonly Color BorderColor = new(1, 0, 1);

    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
    private void DrawMaskBorder(float width, float height)
    {
        if (_maskRoot is null)
            return;


        // top, bottom, left, right strips
        (Vector2[] quad, Color color)[] strips =
        [
            ([new(0, 0), new(width, 0), new(width, BorderThickness), new(0, BorderThickness)], BorderColor),
            ([new(0, height - BorderThickness), new(width, height - BorderThickness), new(width, height), new(0, height)],
                BorderColor),
            ([new(0, 0), new(BorderThickness, 0), new(BorderThickness, height), new(0, height)], BorderColor),
            ([new(width - BorderThickness, 0), new(width, 0), new(width, height), new(width - BorderThickness, height)],
                BorderColor),
        ];

        foreach (var (quad, color) in strips)
        {
            var poly = new Polygon2D { Color = color, Polygon = quad };
            _maskRoot.AddChild(poly);
        }
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