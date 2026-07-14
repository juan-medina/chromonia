// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Chromonia.Transition;
using Chromonia.Core;
using Chromonia.Library;
using Chromonia.Music;

namespace Chromonia.UI;

public partial class MainMenu : Node2D
{
    [Export] private Control _mainButtonsContainer = null!;
    [Export] private Control _optionsContainer = null!;
    [Export] private Button _playButton = null!;
    [Export] private Button _optionsButton = null!;
    [Export] private Button _aboutButton = null!;
    [Export] private Button _exitButton = null!;
    [Export] private Components.SettingsPanel _settingsPanel = null!;
    [Export] private Button _backButton = null!;
    [Export] private CanvasGroup _blobsLayer = null!;
    [Export] private SubViewport _blobsViewport = null!;
    [Export] private Label _logoText = null!;
    [Export] private Sprite2D _blobsDisplay = null!;

    private PaintingLibrary _paintingLibrary = null!;

    private const int MaxBlobs = 15;
    private TransitionManager _transitionManager = null!;
    private UiAudioManager _uiAudioManager = null!;
    private BlobData[] _blobs = [];
    private MusicPlayer _music = null!;
    private float _colorPhase;

    private struct BlobData
    {
        public MenuBlob Node;
        public float PhaseX;
        public float PhaseY;
        public float SpeedX;
        public float SpeedY;
        public float RadiusX;
        public float RadiusY;
        public float CenterX;
        public float CenterY;
    }


    public override void _Ready()
    {
        _transitionManager = GetNode<TransitionManager>("/root/TransitionManager");
        _uiAudioManager = GetNode<UiAudioManager>("/root/UiAudioManager");

        _transitionManager.OnTransitionFailed += OnFatalAppError;

        if (!InitPaintingLibrary()) return;
        if (!InitMusic()) return;

        SetupButtons();
        CreateBlobs();
        SetupChromeEffect();
    }

    private void SetupChromeEffect()
    {
        var viewportTex = _blobsViewport.GetTexture();
        _blobsDisplay.Texture = viewportTex;

        if (_logoText.Material is ShaderMaterial mat) mat.SetShaderParameter("reflection_map", viewportTex);
    }

    private bool InitPaintingLibrary()
    {
        _paintingLibrary = GetNodeOrNull<PaintingLibrary>("/root/PaintingLibrary");
        if (_paintingLibrary is not null) return true;
        HandleFatalError("PaintingLibrary global autoload is missing.");
        return false;
    }

    private bool InitMusic()
    {
        _music = GetNodeOrNull<MusicPlayer>("/root/MusicPlayer");
        if (_music is null)
        {
            HandleFatalError("MusicPlayer global autoload is missing.");
            return false;
        }

        _music.OnPlaybackFailed += OnFatalAppError;
        _music.Play();
        return true;
    }


    private void SetupButtons()
    {
        // add events
        _playButton.Pressed += OnPlayPressed;
        _optionsButton.Pressed += OnOptionsPressed;
        _aboutButton.Pressed += OnAboutPressed;
        _exitButton.Pressed += OnExitPressed;
        _backButton.Pressed += OnBackPressed;

        // first button is focus
        _playButton.GrabFocus();

        // setup sounds in the buttons
        _uiAudioManager.ConnectMenuSounds(this);
    }

    private void CreateBlobs()
    {
        _blobs = new BlobData[MaxBlobs];
        for (int i = 0; i < _blobs.Length; i++)
        {
            var b = new MenuBlob();
            b.Radius = (float)GD.RandRange(70f, 180f);
            b.ZIndex = 0;
            _blobsLayer.AddChild(b);

            _blobs[i] = new BlobData
            {
                Node = b,
                PhaseX = (float)GD.RandRange(0, Mathf.Tau),
                PhaseY = (float)GD.RandRange(0, Mathf.Tau),
                SpeedX = (float)GD.RandRange(0.1f, 0.6f),
                SpeedY = (float)GD.RandRange(0.1f, 0.6f),
                RadiusX = (float)GD.RandRange(150f, 600f),
                RadiusY = (float)GD.RandRange(150f, 600f),
                CenterX = 0f,
                CenterY = 0f
            };
        }
    }

    public override void _Process(double delta)
    {
        // color cycle blobs in HDR colors
        float fDelta = (float)delta;

        _colorPhase += fDelta * 0.1f;
        if (_colorPhase > 1f) _colorPhase -= 1f;

        Color currentColor = Color.FromHsv(_colorPhase, 0.8f, 3.5f);

        for (int i = 0; i < _blobs.Length; i++)
        {
            ref var b = ref _blobs[i];
            b.PhaseX += b.SpeedX * fDelta;
            b.PhaseY += b.SpeedY * fDelta;

            float x = b.CenterX + Mathf.Cos(b.PhaseX) * b.RadiusX;
            float y = b.CenterY + Mathf.Sin(b.PhaseY) * b.RadiusY;

            b.Node.Position = new Vector2(x, y);
            b.Node.DisplayColor = currentColor;
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_music)) _music.OnPlaybackFailed -= OnFatalAppError;
        if (IsInstanceValid(_transitionManager)) _transitionManager.OnTransitionFailed -= OnFatalAppError;
    }

    private void OnPlayPressed()
    {
        // get a new set of paintings every time we play
        _paintingLibrary.Shuffle();
        _transitionManager.TransitionToGame();
    }

    private void OnOptionsPressed()
    {
        GetTree().Root.GuiDisableInput = true;

        var tween = CreateTween();
        tween.TweenProperty(_mainButtonsContainer, "modulate:a", 0.0f, 0.15f);

        tween.TweenCallback(Callable.From(() =>
        {
            _mainButtonsContainer.Visible = false;
            _optionsContainer.Visible = true;
            _optionsContainer.Modulate = new Color(1, 1, 1, 0);
            _settingsPanel.Refresh();
        }));

        tween.TweenProperty(_optionsContainer, "modulate:a", 1.0f, 0.15f);
        tween.TweenCallback(Callable.From(() =>
        {
            GetTree().Root.GuiDisableInput = false;
            _settingsPanel.GetFirstFocusableControl().GrabFocus();
        }));
    }

    private void OnBackPressed()
    {
        GetTree().Root.GuiDisableInput = true;

        var tween = CreateTween();
        tween.TweenProperty(_optionsContainer, "modulate:a", 0.0f, 0.15f);

        tween.TweenCallback(Callable.From(() =>
        {
            _optionsContainer.Visible = false;
            _mainButtonsContainer.Visible = true;
            _mainButtonsContainer.Modulate = new Color(1, 1, 1, 0);
        }));

        tween.TweenProperty(_mainButtonsContainer, "modulate:a", 1.0f, 0.15f);
        tween.TweenCallback(Callable.From(() =>
        {
            GetTree().Root.GuiDisableInput = false;
            _optionsButton.GrabFocus();
        }));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_optionsContainer.Visible || !@event.IsActionPressed("ui_cancel")) return;
        OnBackPressed();
        GetViewport().SetInputAsHandled();
    }

    private static void OnAboutPressed() => GD.Print("About Pressed");
    private void OnExitPressed() => GetTree().Quit();

    private void OnFatalAppError(Result err) => HandleFatalError(err.Message);

    private void HandleFatalError(string errorMessage)
    {
        GD.PrintErr($"Transition Failed: {errorMessage}");
        OS.Alert("Something went wrong loading the game.", "Transition Error");
        GetTree().Quit();
    }
}