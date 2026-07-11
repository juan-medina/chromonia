// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Chromonia.Library;
using Godot;
using Chromonia.Core;
using BlobEnemy = Chromonia.Enemies.BlobEnemy;
using MusicPlayer = Chromonia.Music.MusicPlayer;
using PaintingLibrary = Chromonia.Library.PaintingLibrary;
using SharedProgressBar = Chromonia.UI.SharedProgressBar;
using TransitionManager = Chromonia.Transition.TransitionManager;

namespace Chromonia.Main;

public partial class Main : Node2D
{
    private PaintingLibrary _library = null!;
    private MusicPlayer _music = null!;
    private TransitionManager _transition = null!;

    [Export] private SubViewport _maskViewport = null!;
    [Export] private Node2D _maskRoot = null!;
    [Export] private Sprite2D _painting = null!;
    [Export] private Node2D _playfield = null!;
    [Export] private Label _title = null!;
    [Export] private Label _artist = null!;
    [Export] private Arrow.Arrow _arrow = null!;
    [Export] private SharedProgressBar _progressBar = null!;
    [Export] private CanvasGroup _blobsLayer = null!;
    [Export] private Line2D _perimeterLine = null!;
    [Export] private Line2D _drawingLine = null!;
    [Export] private Panel _dropShadow = null!;
    [Export] private StaticBody2D _borderPhysics = null!;
    [Export] private AudioStreamPlayer2D _sfxDrawLoop = null!;
    [Export] private AudioStreamPlayer2D _sfxSafeLoop = null!;
    [Export] private AudioStreamPlayer2D _sfxErase = null!;
    [Export] private AudioStreamPlayer2D _sfxSnap = null!;
    [Export] private AudioStreamPlayer2D _sfxWaterDrop = null!;
    [Export] private AudioStreamPlayer2D _sfxPaintStroke = null!;

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
    private const float MinBlobSpeed = 50f;
    private const float MaxBlobSpeed = 350f;
    private const int ClusterCount = 8;
    private const float StartImmunityDuration = 1.5f;
    private const float ToReveal = 0.35f;

    private int _paintingWidth = ViewportWidth;
    private int _paintingHeight = ViewportHeight;
    private float _scaledWidth;
    private float _scaledHeight;
    private Vector2[] Perimeter { get; set; } = [];

    private float _totalClaimedArea;
    private float _claimedAreaA;
    private float _claimedAreaB;
    private float _totalArea;

    private PlayerController _playerController = null!;
    private CollisionSystem _collisionSystem = null!;
    private ClaimSystem _claimSystem = null!;

    private readonly List<BlobEnemy> _trappedBlobsBuffer = new(32);

    public override void _Ready()
    {
        _library = GetNodeOrNull<PaintingLibrary>("/root/PaintingLibrary");
        if (_library is null)
        {
            HandleFatalError("PaintingLibrary global autoload is missing.");
            return;
        }

        if (!InitMusic()) return;

        _transition = GetNode<TransitionManager>("/root/TransitionManager");
        if (_transition is null)
        {
            HandleFatalError("TransitionManager global autoload is missing.");
            return;
        }

        _transition.OnTransitionFailed += OnFatalAppError;

        _playerController = new PlayerController(_arrow, _drawingLine);
        _playerController.OnClaimTriggered += HandleClaimTriggered;

        _collisionSystem = new CollisionSystem(_playfield, _arrow);
        _claimSystem = new ClaimSystem(_playfield, _maskRoot, _perimeterLine, _arrow);

        var result = TryLoadCurrentPainting();
        if (!result)
        {
            HandleFatalError(result.Message);
            return;
        }

        SetupArrow();
    }

    private bool InitMusic()
    {
        Result result;
        _music = GetNode<MusicPlayer>("/root/MusicPlayer");
        if (_music is null)
        {
            result = Result.Fail("MusicPlayer global autoload is missing.");
        }
        else
        {
            _music.OnPlaybackFailed += OnFatalAppError;
            _music.OnPlaybackStarted += OnMusicStarted;

            result = _music.TryPlayMusic();
        }


        if (!result) HandleFatalError(result.Message);
        return result;
    }

    private void OnMusicStarted(Result<ResourceEntry> result)
    {
        if (!result)
        {
            OnFatalAppError(Result.Fail(result.ErrorMessage));
            return;
        }

        GD.Print($"Now playing: {result.Value.Name} by {result.Value.Author}");
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_music))
            _music.OnPlaybackFailed -= OnFatalAppError;

        if (IsInstanceValid(_transition))
            _transition.OnTransitionFailed -= OnFatalAppError;

        base._ExitTree();
    }

    private void OnFatalAppError(Result err)
    {
        HandleFatalError(err.Message);
    }

    private void HandleClaimTriggered(int hitSegmentIndex)
    {
        var activeArray = _playerController.ActiveLine.ToArray();

        var (claimedPoly, newPerimeter, claimedArea) = _claimSystem.DetermineClaimedPolygon(
            Perimeter, _playerController.StartSegmentIndex, hitSegmentIndex, activeArray);

        _collisionSystem.GetTrappedBlobs(claimedPoly, _trappedBlobsBuffer);

        if (_collisionSystem.IsLethalTrap(_trappedBlobsBuffer))
        {
            KillPlayer();
            return;
        }

        DestroyBlobs(_trappedBlobsBuffer);
        if (_trappedBlobsBuffer.Count > 0)
        {
            _sfxWaterDrop.Play();
        }

        _sfxPaintStroke.Play();

        _claimSystem.ApplyNewPerimeter(newPerimeter, out var updatedPerimeter);
        Perimeter = updatedPerimeter;

        _claimSystem.CreateClaimVisuals(claimedPoly, GetTree());
        _claimSystem.CreateClaimPhysics(claimedPoly);

        _playerController.CancelDrawing();
        UpdateProgressAndCheckWin(claimedArea);
    }

    private static void DestroyBlobs(List<BlobEnemy> blobsToDestroy)
    {
        for (int i = 0; i < blobsToDestroy.Count; i++)
        {
            var blob = blobsToDestroy[i];
            if (blob.GetParent() is Enemies.BlobCluster cluster)
                cluster.Dissolve();
            else
                blob.Dissolve();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 posBefore = _arrow.Position;

        _playerController.MoveArrow(delta, ArrowSpeed, Perimeter);

        Vector2 posAfter = _arrow.Position;

        _collisionSystem.UpdateBlobMergeStates();

        if (_collisionSystem.CheckCollisions(_playerController.State, _playerController.ActiveLine)) KillPlayer();

        UpdateAudioState(posBefore != posAfter);
    }

    private void UpdateAudioState(bool actuallyMoved)
    {
        bool shouldPlayDraw = _playerController.State == PlayerState.Drawing && actuallyMoved;
        bool shouldPlaySafe = _playerController.State == PlayerState.OnPerimeter && actuallyMoved;

        if (shouldPlayDraw)
        {
            if (!_sfxDrawLoop.Playing) _sfxDrawLoop.Play();
            _sfxDrawLoop.StreamPaused = false;
        }
        else
            _sfxDrawLoop.StreamPaused = true;

        if (shouldPlaySafe)
        {
            if (!_sfxSafeLoop.Playing) _sfxSafeLoop.Play();
            _sfxSafeLoop.StreamPaused = false;
        }
        else
            _sfxSafeLoop.StreamPaused = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            _music.Stop();
            GetTree().Quit();
            return;
        }

        if (!@event.IsActionPressed("ui_accept")) return;

        if (_playerController.State == PlayerState.Won)
        {
            _library.MoveNext();
            _transition.ReloadCurrentScene();
            return;
        }

        _playerController.CycleColor();
    }

    private void Reveal()
    {
        _playerController.State = PlayerState.Won;
        _collisionSystem.DestroyAllBlobs();

        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_painting.Material, "shader_parameter/reveal_progress", 1.0f, RevealTime);
        tween.TweenProperty(_progressBar, "modulate:a", 0.0f, RevealTime / 3);

        const float newAvailableHeight = ViewportHeight - (35f + BottomMargin);
        float newScale = Math.Min(AvailableWidth / _paintingWidth, newAvailableHeight / _paintingHeight);

        tween.TweenProperty(_painting, "scale", new Vector2(newScale, newScale), RevealTime);
        tween.TweenProperty(_painting, "position", Vector2.Zero, RevealTime);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => _painting.Material = null));

        _title.Visible = true;
        _artist.Visible = true;
        _arrow.Visible = false;
        _perimeterLine.Visible = false;
        _drawingLine.Visible = false;
    }

    private Result TryLoadCurrentPainting()
    {
        var result = _library.Current();
        return !result ? Result.Fail(result.ErrorMessage) : TryLoadPainting(result.Value);
    }

    private Result TryLoadPainting(ResourceEntry painting)
    {
        var result = _library.LoadCurrentResource();
        if (!result) return Result.Fail(result.ErrorMessage);

        _paintingWidth = result.Value.GetWidth();
        _paintingHeight = result.Value.GetHeight();

        if (_paintingWidth <= 0 || _paintingHeight <= 0)
            return Result.Fail($"Invalid painting dimensions: {_paintingWidth}x{_paintingHeight}");

        _painting.Texture = result.Value;

        CalculateDimensionsAndScale();
        PositionElements();
        SetupLabels(painting);
        SetupShaderMask();

        CreateBorder(_scaledWidth, _scaledHeight, BorderColor, BorderThickness);
        SpawnEnemies(_scaledWidth, _scaledHeight);
        RegisterSpawnedBlobs();

        return Result.Ok();
    }

    private void CalculateDimensionsAndScale()
    {
        float scale = Math.Min(AvailableWidth / _paintingWidth, AvailableHeight / _paintingHeight);
        _scaledWidth = _paintingWidth * scale;
        _scaledHeight = _paintingHeight * scale;
        _totalArea = _scaledWidth * _scaledHeight;

        _claimSystem.UpdateDimensions(_scaledWidth, _scaledHeight);
        _painting.Scale = new Vector2(scale, scale);
    }

    private void PositionElements()
    {
        const float offsetY = (TopMargin - BottomMargin) / 2f;
        _painting.Position = new Vector2(0, offsetY);
        _playfield.Position = new Vector2(0, offsetY);

        _dropShadow.Size = new Vector2(_paintingWidth, _paintingHeight);
        _dropShadow.Position = new Vector2(-_paintingWidth / 2f, -_paintingHeight / 2f);
    }

    private void SetupLabels(ResourceEntry painting)
    {
        var topLeft = new Vector2(-_paintingWidth / 2f + LabelPadding, -_paintingHeight / 2f + LabelPadding);

        _title.Position = topLeft;
        _title.Text = $"{painting.Name} ({painting.Metadata.GetValueOrDefault("years", "")})";
        _title.AddThemeColorOverride("font_color", new Color(1.5F, 1.5F, 1.5F));

        _artist.Position = topLeft + new Vector2(0, _title.Size.Y > 0 ? _title.Size.Y : 24f);
        _artist.Text = $"{painting.Author} ({painting.Metadata.GetValueOrDefault("nationality", "")})";
        _artist.AddThemeColorOverride("font_color", new Color(1.5F, 1.5F, 1.5F));
    }

    private void SetupShaderMask()
    {
        _maskViewport.Size = new Vector2I((int)_scaledWidth, (int)_scaledHeight);

        if (_painting.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("mask_texture", _maskViewport.GetTexture());
            material.SetShaderParameter("reveal_progress", 0.0f);
        }
        else
            HandleFatalError(
                "Painting material is not a ShaderMaterial. Please ensure the painting uses the correct shader.");
    }

    private void RegisterSpawnedBlobs()
    {
        var nodes = GetTree().GetNodesInGroup(BlobEnemy.GroupName);
        var blobs = new List<BlobEnemy>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] is BlobEnemy blob)
            {
                blobs.Add(blob);
            }
        }

        _collisionSystem.AddBlobs(blobs);
    }

    private void SpawnEnemies(float width, float height)
    {
        var bounds = new Rect2(-width / 2f, -height / 2f, width, height);

        for (int i = 0; i < ClusterCount; i++)
        {
            var energy = (i % 2 == 0) ? Energy.A : Energy.B;
            float speed = (float)GD.RandRange(MinBlobSpeed, MaxBlobSpeed);
            var cluster = new Enemies.BlobCluster(energy, speed);

            float px = (float)GD.RandRange(bounds.Position.X + 70f, bounds.End.X - 70f);
            float py = (float)GD.RandRange(bounds.Position.Y + 70f, bounds.End.Y - 70f);
            cluster.Position = new Vector2(px, py);
            cluster.ZIndex = 2;

            _blobsLayer.AddChild(cluster);
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

        Perimeter = [topLeft, topRight, bottomRight, bottomLeft, topLeft];

        _perimeterLine.Points = [topLeft, topRight, bottomRight, bottomLeft];
        _perimeterLine.DefaultColor = color;
        _perimeterLine.Width = thickness;

        _drawingLine.DefaultColor = Colors.HotPink;
        _drawingLine.Width = thickness;

        foreach (var child in _borderPhysics.GetChildren()) child.QueueFree();

        for (int i = 0; i < Perimeter.Length - 1; i++)
        {
            var shape = new CollisionShape2D
            {
                Shape = new SegmentShape2D { A = Perimeter[i], B = Perimeter[i + 1] }
            };
            _borderPhysics.AddChild(shape);
        }
    }

    private void SetupArrow()
    {
        _arrow.SetPosition(new Vector2(0, _scaledHeight / 2f));
        _arrow.ZIndex = 2;
        _arrow.StartImmunity(StartImmunityDuration);
    }

    private void UpdateProgressAndCheckWin(float claimedArea)
    {
        if (_arrow.CurrentEnergy == Energy.A)
            _claimedAreaA += claimedArea;
        else
            _claimedAreaB += claimedArea;

        _totalClaimedArea = _claimedAreaA + _claimedAreaB;

        _progressBar.UpdateProgress(_claimedAreaA / _totalArea, _claimedAreaB / _totalArea);

        if (_claimedAreaA / _totalArea >= ToReveal && _claimedAreaB / _totalArea >= ToReveal) Reveal();
    }

    private void KillPlayer()
    {
        if (_playerController.ActiveLine.Count > 0)
            _sfxErase.Play();
        else
            _sfxSnap.Play();

        _playerController.ResetToStart();
    }

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Game Initialization Failed: {errorMessage}");
        OS.Alert("Something went wrong loading Chromonia.", "Initialization Error");
        GetTree().Quit();
    }
}