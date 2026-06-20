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
	[Export] private Arrow _arrow = null!;
	[Export] private Line2D _lineOutline = null!;
	[Export] private Line2D _lineColor = null!;

	//////////////////////////////////////////////////////////////////////
	/// Globals
	private PaintingLibrary _library = null!;

	//////////////////////////////////////////////////////////////////////
	/// Constants
	private const int ViewportWidth = 1920;

	private const int ViewportHeight = 1080;
	private const float LabelPadding = 10f;
	private static readonly Color BorderColor = new(0.25f, 0.55f, 0.3f);
	private const float BorderThickness = 9f;
	private const float ArrowSpeed = 200f;

	//////////////////////////////////////////////////////////////////////
	/// State
	private int _paintingWidth = ViewportWidth;

	private int _paintingHeight = ViewportHeight;
	private readonly List<LineSegment> _safeSegments = [];
	private float _playLeft;
	private float _playRight;
	private float _playTop;
	private float _playBottom;

	private bool _isDrawing;
	private int _drawStartSegmentIndex = -1;
	private Vector2 _lastDrawDirection;
	private readonly List<Vector2> _drawPoints = [];

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

		if (!_isDrawing)
		{
			var restingIndex = GetRestingSegmentIndex(pos);
			bool sliding = restingIndex >= 0 && IsAlongSegment(_safeSegments[restingIndex], direction);
			if (sliding)
			{
				_arrow.SetPosition(GetSnappedPosition(pos, velocity));
				return;
			}

			StartDrawing(pos, restingIndex);
		}

		var target = ClampToPlayfield(pos + velocity);

		if (_lastDrawDirection != Vector2.Zero && direction != _lastDrawDirection)
		{
			_drawPoints.Add(pos);
		}

		_lastDrawDirection = direction;

		if (TryGetDrawingHit(target, out var hitPoint))
		{
			UpdateDrawLines(hitPoint);
			_arrow.SetPosition(hitPoint);
			_isDrawing = false;
			return;
		}

		UpdateDrawLines(target);
		_arrow.SetPosition(target);
	}

	private void StartDrawing(Vector2 startPos, int segmentIndex)
	{
		_isDrawing = true;
		_drawStartSegmentIndex = segmentIndex;
		_lastDrawDirection = Vector2.Zero;
		_drawPoints.Clear();
		_drawPoints.Add(startPos);
		_lineColor.DefaultColor = _arrow.TintColor;
	}

	private void UpdateDrawLines(Vector2 head)
	{
		var points = new Vector2[_drawPoints.Count + 1];
		for (int i = 0; i < _drawPoints.Count; i++) points[i] = _drawPoints[i];
		points[^1] = head;

		_lineOutline.Points = points;
		_lineColor.Points = points;
	}

	private Vector2 ClampToPlayfield(Vector2 pos)
	{
		return new Vector2(Math.Clamp(pos.X, _playLeft, _playRight), Math.Clamp(pos.Y, _playTop, _playBottom));
	}

	private static bool IsAlongSegment(LineSegment segment, Vector2 direction)
	{
		return segment.IsHorizontal ? direction.X != 0 : direction.Y != 0;
	}

	private int GetRestingSegmentIndex(Vector2 pos)
	{
		for (int i = 0; i < _safeSegments.Count; i++)
		{
			var segment = _safeSegments[i];
			if (pos.DistanceTo(segment.GetClosestPoint(pos)) <= segment.Tolerance) return i;
		}

		return -1;
	}

	private bool TryGetDrawingHit(Vector2 pos, out Vector2 hitPoint)
	{
		hitPoint = pos;
		for (int i = 0; i < _safeSegments.Count; i++)
		{
			if (i == _drawStartSegmentIndex) continue;
			var segment = _safeSegments[i];
			var closest = segment.GetClosestPoint(pos);
			if (!(pos.DistanceTo(closest) <= segment.Tolerance)) continue;
			hitPoint = closest;
			return true;
		}

		return false;
	}

	private void UpdateSafeSegments()
	{
		_safeSegments.Clear();
		float halfW = _paintingWidth / 2f;
		float halfH = _paintingHeight / 2f;

		_playLeft = -halfW + BorderThickness;
		_playRight = halfW - BorderThickness;
		_playTop = -halfH + BorderThickness;
		_playBottom = halfH - BorderThickness;

		_safeSegments.Add(new LineSegment(new Vector2(_playLeft, _playTop), new Vector2(_playRight, _playTop),
			BorderThickness));
		_safeSegments.Add(new LineSegment(new Vector2(_playLeft, _playBottom), new Vector2(_playRight, _playBottom),
			BorderThickness));
		_safeSegments.Add(new LineSegment(new Vector2(_playLeft, _playTop), new Vector2(_playLeft, _playBottom),
			BorderThickness));
		_safeSegments.Add(new LineSegment(new Vector2(_playRight, _playTop), new Vector2(_playRight, _playBottom),
			BorderThickness));
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
