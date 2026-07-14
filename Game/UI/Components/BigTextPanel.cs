// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;
using Chromonia.Core;

namespace Chromonia.UI.Components;

public partial class BigTextPanel : PanelContainer
{
    [Export] private RichTextLabel _richTextLabel = null!;
    [Export] private ScrollContainer _scrollContainer = null!;

    public event Action<Result>? OnLoadFailed;

    public override void _Ready()
    {
        _richTextLabel.MetaClicked += OnMetaClicked;
    }


    public override void _Process(double delta)
    {
        float scrollInput = Input.GetAxis("ui_up", "ui_down");
        if (Mathf.Abs(scrollInput) > 0.1f)
        {
            _scrollContainer.ScrollVertical += (int)(scrollInput * 500f * delta);
        }
    }

    public void Init(string path)
    {
        if (FileAccess.FileExists(path))
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            // We use basic BBCode replacement for newlines since Godot RichTextLabel might ignore them without [br] or proper formatting
            // However, Godot 4 RichTextLabel handles \n correctly when bbcode is enabled.
            _richTextLabel.Text = file.GetAsText();
        }
        else
        {
            OnLoadFailed?.Invoke(Result.Fail($"Could not load {path}"));
        }
    }

    private static void OnMetaClicked(Variant meta)
    {
        string url = meta.AsString();
        if (url.StartsWith("https")) OS.ShellOpen(url);
    }
}