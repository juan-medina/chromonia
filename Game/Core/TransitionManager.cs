// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;

namespace Chromonia.Core;

public partial class TransitionManager : CanvasLayer
{
    [Export] private ColorRect _fadeRect = null!;

    public event Action<Result>? OnTransitionFailed;

    private bool _isTransitioning;
    private const float FadeDuration = 0.5f;
    private const float BlackScreenDuration = 0.2f;

    public override void _Ready()
    {
        _fadeRect.Color = new Color(0, 0, 0, 0);
        _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public void ReloadCurrentScene()
    {
        if (_isTransitioning) return;
        ReloadCurrentSceneAsync();
    }

    private async void ReloadCurrentSceneAsync()
    {
        SceneTree? tree = null;
        try
        {
            tree = GetTree();
            if (tree == null)
            {
                OnTransitionFailed?.Invoke(Result.Fail("Scene transition failed: GetTree fail"));
                return;
            }

            if (!IsInstanceValid(_fadeRect))
            {
                OnTransitionFailed?.Invoke(Result.Fail("Scene transition failed: _fadeRect is invalid"));
                return;
            }

            // we dont want another transition to start while we are already transitioning
            _isTransitioning = true;

            // prevent user input
            _fadeRect.MouseFilter = Control.MouseFilterEnum.Stop;
            tree.Paused = true;

            // Fade Out
            Tween fadeOutTween = CreateTween();
            fadeOutTween.TweenProperty(_fadeRect, "color:a", 1.0f, FadeDuration);
            await ToSignal(fadeOutTween, Tween.SignalName.Finished);

            // Safe Deferred Reload
            tree.CallDeferred(SceneTree.MethodName.ReloadCurrentScene);

            // Wait for next frame, this effectively wait too from relodad the current scene
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            // Rest on the black screen for a moment (ignoring pause state)
            await ToSignal(tree.CreateTimer(BlackScreenDuration, true, false, true),
                SceneTreeTimer.SignalName.Timeout);

            // Fade In
            Tween fadeInTween = CreateTween();
            fadeInTween.TweenProperty(_fadeRect, "color:a", 0.0f, FadeDuration);
            await ToSignal(fadeInTween, Tween.SignalName.Finished);

            // restore user input and unpause the game
            tree.Paused = false;
            _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;

            // we can get a new transition now
            _isTransitioning = false;
        }
        catch (Exception ex)
        {
            // just in case restore
            if (IsInstanceValid(_fadeRect))
            {
                _fadeRect.Color = new Color(0, 0, 0, 0);
                _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            }

            if (tree != null) tree.Paused = false;

            _isTransitioning = false;
            OnTransitionFailed?.Invoke(Result.Fail($"Scene transition failed: {ex.Message}"));
        }
    }
}