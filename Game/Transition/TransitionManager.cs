// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Chromonia.Core;
using Godot;

namespace Chromonia.Transition;

public partial class TransitionManager : CanvasLayer
{
    [Export] private ColorRect _fadeRect = null!;
    [Export] private PackedScene _mainMenuScene = null!;
    [Export] private PackedScene _gameScene = null!;

    public event Action<Result>? OnTransitionFailed;

    private bool _isTransitioning;
    private const float FadeDuration = 0.5f;
    private const float BlackScreenDuration = 0.2f;
    private static readonly Color Black = new(0, 0, 0, 0);

    public override void _Ready()
    {
        _fadeRect.Visible = false;
        _fadeRect.Color = new Color(0, 0, 0, 0);
        _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public void ReloadCurrentScene()
    {
        if (_isTransitioning) return;
        var currentPath = GetTree().CurrentScene.SceneFilePath;
        var currentPackedScene = ResourceLoader.Load<PackedScene>(currentPath);
        ChangeSceneInternal(currentPackedScene);
    }

    public void TransitionToMenu()
    {
        if (_isTransitioning) return;
        ChangeSceneInternal(_mainMenuScene);
    }

    public void TransitionToGame()
    {
        if (_isTransitioning) return;
        ChangeSceneInternal(_gameScene);
    }

    private async void ChangeSceneInternal(PackedScene targetScene)
    {
        SceneTree tree = null!;
        try
        {
            tree = GetTree();
            _isTransitioning = true;
            _fadeRect.MouseFilter = Control.MouseFilterEnum.Stop;
            tree.Paused = true;
            _fadeRect.Color = Black;
            _fadeRect.Visible = true;

            Tween fadeOutTween = CreateTween();
            fadeOutTween.TweenProperty(_fadeRect, "color:a", 1.0f, FadeDuration);
            await ToSignal(fadeOutTween, Tween.SignalName.Finished);

            tree.CallDeferred(SceneTree.MethodName.ChangeSceneToPacked, targetScene);

            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await ToSignal(tree.CreateTimer(BlackScreenDuration, true, false, true),
                SceneTreeTimer.SignalName.Timeout);

            Tween fadeInTween = CreateTween();
            fadeInTween.TweenProperty(_fadeRect, "color:a", 0.0f, FadeDuration);
            await ToSignal(fadeInTween, Tween.SignalName.Finished);
        }
        catch (Exception ex)
        {
            OnTransitionFailed?.Invoke(Result.Fail($"Scene transition failed: {ex.Message}"));
        }
        finally
        {
            if (tree != null) tree.Paused = false;
            _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            _isTransitioning = false;
            _fadeRect.Color = Black;
            _fadeRect.Visible = false;
        }
    }
}