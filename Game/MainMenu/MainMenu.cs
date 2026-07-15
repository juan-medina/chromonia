// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Library;
using Chromonia.Music;
using Chromonia.Settings;
using Chromonia.Theme;
using Chromonia.Transition;
using Godot;
using System.Collections.Generic;

namespace Chromonia.MainMenu;

public partial class MainMenu : Node2D
{
    [Export] private Control _mainButtonsContainer = null!;
    [Export] private Control _optionsContainer = null!;
    [Export] private Control _aboutContainer = null!;
    [Export] private Button _playButton = null!;
    [Export] private Button _optionsButton = null!;
    [Export] private Button _aboutButton = null!;
    [Export] private Button _exitButton = null!;
    [Export] private SettingsPanel _settingsPanel = null!;
    [Export] private BigTextPanel _bigTextPanel = null!;
    [Export] private Button _backButton = null!;
    [Export] private Button _aboutBackButton = null!;
    [Export] private Control _difficultyContainer = null!;
    [Export] private Button _normalButton = null!;
    [Export] private Button _mediumButton = null!;
    [Export] private Button _hardButton = null!;
    [Export] private Button _zenButton = null!;
    [Export] private Label _difficultyDescription = null!;
    [Export] private Button _difficultyBackButton = null!;
    [Export] private CanvasGroup _blobsLayer = null!;
    [Export] private SubViewport _blobsViewport = null!;
    [Export] private Label _logoText = null!;
    [Export] private Sprite2D _blobsDisplay = null!;

    [Export] private PackedScene _menuBlobScene = null!;
    private PaintingLibrary _paintingLibrary = null!;
    private SettingsManager _settingsManager = null!;

    private static readonly Dictionary<GameDifficulty, string> DifficultyDescriptions = new()
    {
        [GameDifficulty.Normal] = "A quiet canvas. Two pairs drift through open space.",
        [GameDifficulty.Medium] = "The canvas stirs. Three pairs roam the unclaimed field.",
        [GameDifficulty.Hard]   = "Four pairs fill the space. Room to breathe, but barely.",
        [GameDifficulty.Zen]    = "Still waters. Just you and the painting."
    };

    private const int MaxBlobs = 15;
    private TransitionManager _transitionManager = null!;
    private AudioTheme _audioTheme = null!;
    private ErrorManager.ErrorManager _errorManager = null!;
    private BlobData[] _blobs = [];
    private MusicPlayer _music = null!;
    private float _colorPhase;

    private struct BlobData
    {
        public MenuBlob.MenuBlob Node;
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
        _errorManager = GetNode<ErrorManager.ErrorManager>("/root/ErrorManager");
        _transitionManager = GetNode<TransitionManager>("/root/TransitionManager");
        _audioTheme = GetNode<AudioTheme>("/root/AudioTheme");
        _paintingLibrary = GetNode<PaintingLibrary>("/root/PaintingLibrary");
        _music = GetNode<MusicPlayer>("/root/MusicPlayer");
        _settingsManager = GetNode<SettingsManager>("/root/SettingsManager");

        _transitionManager.OnTransitionFailed += OnFatalAppError;
        _music.OnPlaybackFailed += OnFatalAppError;

        SetupAbout();
        _music.Play();
        SetupButtons();
        CreateBlobs();
        SetupChromeEffect();
    }

    private void SetupAbout()
    {
        _bigTextPanel.OnLoadFailed += OnFatalAppError;
        _bigTextPanel.Init("res://About/ABOUT.txt");
    }

    private void SetupChromeEffect()
    {
        var viewportTex = _blobsViewport.GetTexture();
        _blobsDisplay.Texture = viewportTex;

        if (_logoText.Material is ShaderMaterial mat) mat.SetShaderParameter("reflection_map", viewportTex);
    }

    private void SetupButtons()
    {
        // add events
        _playButton.Pressed += OnPlayPressed;
        _optionsButton.Pressed += OnOptionsPressed;
        _aboutButton.Pressed += OnAboutPressed;
        _exitButton.Pressed += OnExitPressed;
        _backButton.Pressed += OnBackPressed;
        _aboutBackButton.Pressed += OnAboutBackPressed;
        _difficultyBackButton.Pressed += OnDifficultyBackPressed;

        _normalButton.Pressed += () => OnDifficultySelected(GameDifficulty.Normal);
        _mediumButton.Pressed += () => OnDifficultySelected(GameDifficulty.Medium);
        _hardButton.Pressed += () => OnDifficultySelected(GameDifficulty.Hard);
        _zenButton.Pressed += () => OnDifficultySelected(GameDifficulty.Zen);

        _normalButton.FocusEntered += () => UpdateDifficultyDescription(GameDifficulty.Normal);
        _mediumButton.FocusEntered += () => UpdateDifficultyDescription(GameDifficulty.Medium);
        _hardButton.FocusEntered += () => UpdateDifficultyDescription(GameDifficulty.Hard);
        _zenButton.FocusEntered += () => UpdateDifficultyDescription(GameDifficulty.Zen);

        // first button is focus
        _playButton.GrabFocus();

        // setup sounds in the buttons
        _audioTheme.ConnectMenuSounds(this);
    }

    private void CreateBlobs()
    {
        _blobs = new BlobData[MaxBlobs];
        for (int i = 0; i < _blobs.Length; i++)
        {
            var b = _menuBlobScene.Instantiate<MenuBlob.MenuBlob>();
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
        if (IsInstanceValid(_bigTextPanel)) _bigTextPanel.OnLoadFailed -= OnFatalAppError;
    }

    private void OnPlayPressed() =>
        TransitionToMenu(_mainButtonsContainer, _difficultyContainer, _normalButton,
            () => UpdateDifficultyDescription(_settingsManager.Difficulty));

    private void OnDifficultyBackPressed() =>
        TransitionToMenu(_difficultyContainer, _mainButtonsContainer, _playButton);

    private void OnDifficultySelected(GameDifficulty difficulty)
    {
        _settingsManager.SetDifficulty(difficulty);
        _paintingLibrary.Shuffle();
        _transitionManager.TransitionToGame();
    }

    private void UpdateDifficultyDescription(GameDifficulty difficulty)
    {
        _difficultyDescription.Text = DifficultyDescriptions[difficulty];
    }

    private void TransitionToMenu(Control hideMenu, Control showMenu, Control focusControl,
        System.Action? onShowCallback = null)
    {
        GetTree().Root.GuiDisableInput = true;

        var tween = CreateTween();
        tween.TweenProperty(hideMenu, "modulate:a", 0.0f, 0.15f);

        tween.TweenCallback(Callable.From(() =>
        {
            hideMenu.Visible = false;
            showMenu.Visible = true;
            showMenu.Modulate = new Color(1, 1, 1, 0);
            onShowCallback?.Invoke();
        }));

        tween.TweenProperty(showMenu, "modulate:a", 1.0f, 0.15f);
        tween.TweenCallback(Callable.From(() =>
        {
            GetTree().Root.GuiDisableInput = false;
            focusControl.GrabFocus();
        }));
    }

    private void OnOptionsPressed() =>
        TransitionToMenu(_mainButtonsContainer, _optionsContainer, _settingsPanel.GetFirstFocusableControl(),
            _settingsPanel.Refresh);

    private void OnBackPressed() =>
        TransitionToMenu(_optionsContainer, _mainButtonsContainer, _optionsButton);

    private void OnAboutPressed() =>
        TransitionToMenu(_mainButtonsContainer, _aboutContainer, _aboutBackButton);

    private void OnAboutBackPressed() =>
        TransitionToMenu(_aboutContainer, _mainButtonsContainer, _aboutButton);

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel")) return;
        if (_difficultyContainer.Visible)
        {
            OnDifficultyBackPressed();
            GetViewport().SetInputAsHandled();
        }
        else if (_optionsContainer.Visible)
        {
            OnBackPressed();
            GetViewport().SetInputAsHandled();
        }
        else if (_aboutContainer.Visible)
        {
            OnAboutBackPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnExitPressed() => GetTree().Quit();

    private void OnFatalAppError(Result.Result err) => _errorManager.NotifyFatalError(err);
}