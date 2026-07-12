// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Chromonia.Library;
using Godot;
using Chromonia.Core;
using Chromonia.Enemies;
using BlobEnemy = Chromonia.Enemies.BlobEnemy;
using MusicPlayer = Chromonia.Music.MusicPlayer;
using PaintingLibrary = Chromonia.Library.PaintingLibrary;
using SharedProgressBar = Chromonia.UI.SharedProgressBar;
using ToastNotification = Chromonia.UI.ToastNotification;
using GalleryPlaque = Chromonia.UI.GalleryPlaque;
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
    [Export] private Arrow.Arrow _arrow = null!;
    [Export] private SharedProgressBar _progressBar = null!;
    [Export] private CanvasGroup _blobsLayer = null!;
    [Export] private Line2D _perimeterLine = null!;
    [Export] private Line2D _drawingLine = null!;
    [Export] private Panel _dropShadow = null!;
    [Export] private StaticBody2D _borderPhysics = null!;
    [Export] private AudioStreamPlayer _sfxDrawLoop = null!;
    [Export] private AudioStreamPlayer _sfxSafeLoop = null!;
    [Export] private AudioStreamPlayer _sfxErase = null!;
    [Export] private AudioStreamPlayer _sfxSnap = null!;
    [Export] private AudioStreamPlayer _sfxWaterDrop = null!;
    [Export] private AudioStreamPlayer _sfxPaintStroke = null!;
    [Export] private ToastNotification _toastNotification = null!;
    [Export] private GalleryPlaque _galleryPlaque = null!;
    [Export] private TextureProgressBar _transitionProgressBar = null!;

    private bool _isAdvancingToNextRound;

    private const int ViewportWidth = 1920;
    private const int ViewportHeight = 1080;
    private static readonly Color BorderColor = new(0.75f, 2.25f, 0.75f);
    private const float BorderThickness = 5f;
    private const float ArrowSpeed = 300f;
    private const float TopMargin = 120f;
    private const float BottomMargin = 35f;
    private const float SideMargin = 25f;
    private const float AvailableWidth = ViewportWidth - (SideMargin * 2);
    private const float AvailableHeight = ViewportHeight - (TopMargin + BottomMargin);
    private const float RevealTime = 1.0f;
    private const float PlaqueDisplayTime = 4.0f;
    private const float TransitionDelay = 0.15f;
    private const float TotalWaitTime = RevealTime + PlaqueDisplayTime + TransitionDelay;
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

    private PlayerSystem _playerSystem = null!;
    private CollisionSystem _collisionSystem = null!;
    private ClaimSystem _claimSystem = null!;

    private readonly List<BlobEnemy> _trappedBlobsBuffer = new(32);

    public override void _Ready()
    {
        if (!InitGlobals()) return;

        if (!InitSystems()) return;

        SetupLevel();
    }

    private bool InitGlobals() => InitLibrary() && InitMusic() && InitTransitionManager();

    private bool InitLibrary()
    {
        _library = GetNodeOrNull<PaintingLibrary>("/root/PaintingLibrary");
        if (_library is not null) return true;
        HandleFatalError("PaintingLibrary global autoload is missing.");
        return false;
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


        if (!result) OnFatalAppError(result);
        return result;
    }

    private bool InitTransitionManager()
    {
        _transition = GetNode<TransitionManager>("/root/TransitionManager");
        if (_transition is null)
        {
            HandleFatalError("TransitionManager global autoload is missing.");
            return false;
        }

        _transition.OnTransitionFailed += OnFatalAppError;

        return true;
    }

    private bool InitSystems()
    {
        _playerSystem = new PlayerSystem(_arrow, _drawingLine);
        _playerSystem.OnClaimTriggered += HandleClaimTriggered;

        _collisionSystem = new CollisionSystem(_playfield, _arrow);
        _claimSystem = new ClaimSystem(_playfield, _maskRoot, _perimeterLine, _arrow);

        return true;
    }

    private void SetupLevel()
    {
        var result = TryLoadCurrentPainting();
        if (!result)
        {
            OnFatalAppError(result);
            return;
        }

        SpawnEnemies(_scaledWidth, _scaledHeight);
        SetupArrow();
        SetupTransitionProgressBar();
    }

    private void SetupTransitionProgressBar()
    {
        _transitionProgressBar.Visible = false;
        (_transitionProgressBar.TextureProgress as GradientTexture1D)!.Gradient.Colors =
            [Energy.A.Fill(), Energy.B.Fill()];
    }

    private void OnMusicStarted(Result<ResourceEntry> result)
    {
        if (!result)
        {
            OnFatalAppError(Result.Fail(result.ErrorMessage));
            return;
        }

        var entry = result.Value;
        string subtitle = $"{entry.Name} by {entry.Author}\nPerformed by: {entry.Metadata["performer"]}";

        _toastNotification.ShowToast("Now Playing", subtitle);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_music))
        {
            _music.OnPlaybackFailed -= OnFatalAppError;
            _music.OnPlaybackStarted -= OnMusicStarted;
        }

        if (IsInstanceValid(_transition))
            _transition.OnTransitionFailed -= OnFatalAppError;

        base._ExitTree();
    }

    private void OnFatalAppError(Result err) => HandleFatalError(err.Message);

    private void HandleClaimTriggered(int hitSegmentIndex)
    {
        // get the lines of the active drawing
        var activeArray = _playerSystem.ActiveLine.ToArray();

        // create a poligon with those lines
        var (claimedPoly, newPerimeter, claimedArea) = ClaimSystem.DetermineClaimedPolygon(
            Perimeter, _playerSystem.StartSegmentIndex, hitSegmentIndex, activeArray);

        // get trapped blobs
        _collisionSystem.GetTrappedBlobs(claimedPoly, _trappedBlobsBuffer);

        // do we trap any
        if (_trappedBlobsBuffer.Count > 0)
        {
            // if is lethal
            if (_collisionSystem.IsLethalTrap(_trappedBlobsBuffer))
            {
                KillPlayer();
                return;
            }

            DestroyBlobs();
        }


        // cancel current drawing
        _playerSystem.CancelDrawing();

        //  animate claim
        _claimSystem.CreateClaimVisuals(claimedPoly, GetTree());

        // update the visual perimeter
        _claimSystem.ApplyNewPerimeter(newPerimeter, out var updatedPerimeter);
        Perimeter = updatedPerimeter;

        // update collision area
        _claimSystem.CreateClaimPhysics(claimedPoly);

        // check if we win
        UpdateProgressAndCheckWin(claimedArea);

        // play capture zone sound
        _sfxPaintStroke.Play();
    }

    private void DestroyBlobs()
    {
        // destroy the parents will destroy the children, parent are always BlobCluster, dissolve return true
        //  only if we actually dissolve the cluster,since a previous child may have already dissolved it
        for (int i = 0; i < _trappedBlobsBuffer.Count; i++)
            if (_trappedBlobsBuffer[i].GetParent<BlobCluster>().Dissolve())
                _sfxWaterDrop.Play();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // move the arrow
        Vector2 posBefore = _arrow.Position;
        _playerSystem.MoveArrow(delta, ArrowSpeed, Perimeter);
        Vector2 posAfter = _arrow.Position;

        // update merges
        _collisionSystem.UpdateBlobMergeStates();

        // check if we have collided
        if (_collisionSystem.CheckCollisions(_playerSystem.State, _playerSystem.ActiveLine)) KillPlayer();

        // update our audio moving / drawing
        UpdateAudioState(posBefore != posAfter);
    }

    private void UpdateAudioState(bool actuallyMoved)
    {
        // we play a loop sund when we are moving in a safe area, and a different one if we are drawing
        //  there is not movement sound if we are not moving

        bool shouldPlayDraw = _playerSystem.State == PlayerState.Drawing && actuallyMoved;
        bool shouldPlaySafe = _playerSystem.State == PlayerState.OnPerimeter && actuallyMoved;

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
        if (@event.IsActionPressed("skip_reveal") && _playerSystem.State == PlayerState.Won)
        {
            _sfxWaterDrop.Play();
            AdvanceToNextPainting();
            return;
        }

        if (!@event.IsActionPressed("energy_cycle")) return;
        _playerSystem.CycleColor();
    }

    private void Reveal()
    {
        // we reveal the paint with a effect

        // mark as won and kil all blobs
        _playerSystem.State = PlayerState.Won;
        _collisionSystem.DestroyAllBlobs();

        // animate the reveal
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

        // Show gallery plaque
        var paintingResult = _library.Current();
        if (!paintingResult)
        {
            OnFatalAppError(Result.Fail(paintingResult.ErrorMessage));
            return;
        }

        var paintingInfo = paintingResult.Value;

        string title = $"{paintingInfo.Name} ({paintingInfo.Metadata.GetValueOrDefault("years", "")})";
        string artist = $"{paintingInfo.Author} ({paintingInfo.Metadata.GetValueOrDefault("nationality", "")})";

        // Set plaque position to bottom-right of the painting texture
        // Since painting is centered, the bottom-right corner is (width/2, height/2)
        _galleryPlaque.Position = new Vector2(_paintingWidth / 2f, _paintingHeight / 2f);

        // Show plaque after the painting finishes zooming/revealing
        tween.TweenCallback(Callable.From(() => _galleryPlaque.ShowPlaque(title, artist, PlaqueDisplayTime)));

        // Animate the transition progress bar over PlaqueDisplayTime
        tween.TweenCallback(Callable.From(() => _transitionProgressBar.Visible = true));
        _transitionProgressBar.Value = 0.0f;
        tween.TweenProperty(_transitionProgressBar, "value", 1.0f, PlaqueDisplayTime);

        // Set the timer for automatic advancement
        GetTree().CreateTimer(TotalWaitTime).Timeout += AdvanceToNextPainting;

        _arrow.Visible = false;

        // hide the drawing line and perimter
        _perimeterLine.Visible = false;
        _drawingLine.Visible = false;
    }

    private void AdvanceToNextPainting()
    {
        if (_isAdvancingToNextRound) return;
        _isAdvancingToNextRound = true;

        _library.MoveNext();
        _transition.ReloadCurrentScene();
    }

    private Result TryLoadCurrentPainting()
    {
        var result = _library.Current();
        return !result ? Result.Fail(result.ErrorMessage) : TryLoadPainting();
    }

    private Result TryLoadPainting()
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
        SetupShaderMask();

        CreateBorder(_scaledWidth, _scaledHeight, BorderColor, BorderThickness);

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
            var cluster = new BlobCluster(energy, speed);

            float px = (float)GD.RandRange(bounds.Position.X + 70f, bounds.End.X - 70f);
            float py = (float)GD.RandRange(bounds.Position.Y + 70f, bounds.End.Y - 70f);
            cluster.Position = new Vector2(px, py);
            cluster.ZIndex = 2;

            _blobsLayer.AddChild(cluster);
        }

        RegisterSpawnedBlobs();
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
        // if we die while drawing we play a different sound that when we are in the perimeter
        if (_playerSystem.ActiveLine.Count > 0)
            _sfxErase.Play();
        else
            _sfxSnap.Play();

        _playerSystem.ResetToStart();
    }

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Game Initialization Failed: {errorMessage}");
        OS.Alert("Something went wrong loading Chromonia.", "Initialization Error");
        GetTree().Quit();
    }
}