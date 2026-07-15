// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;
using Velopack;
using Velopack.Sources;

namespace Chromonia.AppUpdater;

public partial class AppUpdater : Node
{
    private const string UpdateUrl = "https://github.com/juan-medina/chromonia";

    private ToastNotification.ToastNotification _toast = null!;
    private UpdateManager? _manager;
    private UpdateInfo? _update;

    public AppUpdater()
    {
        // Executes immediately when Godot initializes this Autoload.
        // If it's an installer hook, Velopack exits cleanly here before
        // Godot starts spinning up the rest of the game loop.
        VelopackApp.Build().Run();
    }

    public override void _Ready()
    {
        _toast = GetNode<ToastNotification.ToastNotification>("/root/ToastNotification");
        CheckForUpdatesAsync();
    }

    private async void CheckForUpdatesAsync()
    {
        try
        {
            // dont check for update when running in the editor
            if (OS.HasFeature("editor"))
            {
                GD.Print("[UpdateManager] Running in Editor - bypassing update check.");
                return;
            }

            var source = new GithubSource(UpdateUrl, null, false);
            _manager = new UpdateManager(source);

            if (!_manager.IsInstalled) return;

            _update = await _manager.CheckForUpdatesAsync();
            if (_update == null) return;

            await _manager.DownloadUpdatesAsync(_update);

            _toast.ShowToast(
                "Update ready",
                $"Version {_update.TargetFullRelease.Version} available — click to restart.",
                duration: 10.0f,
                onClick: UpdateClick
            );
        }
        catch (System.Exception e)
        {
            GD.PushWarning($"[UpdateManager] Update check failed: {e.Message}");
        }
    }

    private void UpdateClick()
    {
        if (_update == null || _manager == null) return;

        _manager.ApplyUpdatesAndRestart(_update);

        // we apply the update so we quit the game
        GetTree().Quit();
    }
}