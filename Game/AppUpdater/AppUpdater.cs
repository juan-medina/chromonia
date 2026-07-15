// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Velopack;
using Velopack.Sources;

namespace Chromonia.AppUpdater;

public partial class AppUpdater : Node
{
    private const string UpdateUrl = "https://github.com/juan-medina/chromonia";
    private const string ReleasesUrl = "https://github.com/juan-medina/chromonia/releases/latest";
    private const string ApiReleasesUrl = "https://api.github.com/repos/juan-medina/chromonia/releases/latest";

    private ToastNotification.ToastNotification _toast = null!;
    private UpdateManager? _manager;
    private UpdateInfo? _update;

    public override void _Ready()
    {
        _toast = GetNode<ToastNotification.ToastNotification>("/root/ToastNotification");
        CheckForUpdatesAsync();
    }

    private async void CheckForUpdatesAsync()
    {
        try
        {
            if (OS.HasFeature("editor"))
            {
                GD.Print("[UpdateManager] Running in Editor - bypassing update check.");
                return;
            }

            var source = new GithubSource(UpdateUrl, null, false);
            _manager = new UpdateManager(source);

            if (_manager.IsInstalled)
            {
                await CheckVelopackUpdateAsync();
            }
            else
            {
                await CheckGithubUpdateAsync();
            }
        }
        catch (System.Exception e)
        {
            GD.PushWarning($"[UpdateManager] Update check failed: {e.Message}");
        }
    }

    private async Task CheckVelopackUpdateAsync()
    {
        _update = await _manager!.CheckForUpdatesAsync();
        if (_update == null) return;

        await _manager.DownloadUpdatesAsync(_update);

        _toast.ShowToast(
            "Update ready",
            $"Version {_update.TargetFullRelease.Version} available — click to restart.",
            duration: 10.0f,
            onClick: UpdateVelopackClick
        );
    }

    private async Task CheckGithubUpdateAsync()
    {
        using var http = new System.Net.Http.HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Chromonia");

        var response = await http.GetStringAsync(ApiReleasesUrl);
        using var json = JsonDocument.Parse(response);

        var tagName = json.RootElement.GetProperty("tag_name").GetString();
        if (tagName == null) return;

        var latestVersion = tagName.TrimStart('v');
        var currentVersion = ProjectSettings.GetSetting("application/config/version").ToString();

        if (!IsNewerVersion(latestVersion, currentVersion)) return;

        _toast.ShowToast(
            "Update available",
            $"Version {latestVersion} is available — click to download.",
            duration: 10.0f,
            onClick: UpdateGithubClick
        );
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (!System.Version.TryParse(latest, out var latestVer)) return false;
        if (!System.Version.TryParse(current, out var currentVer)) return false;
        return latestVer > currentVer;
    }

    private void UpdateVelopackClick()
    {
        if (_update == null || _manager == null) return;

        _manager.ApplyUpdatesAndRestart(_update);

        // we apply the update so we quit the game
        GetTree().Quit();
    }

    private static void UpdateGithubClick()
    {
        OS.ShellOpen(ReleasesUrl);
    }
}