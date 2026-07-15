// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;
using Environment = System.Environment;

namespace Chromonia.ErrorManager;

public partial class ErrorManager : Node
{
    private const string UserMessage = "Chromonia ran into a problem and needs to close.";
    private const string DialogTitle = "Unexpected Error";

    private bool _isShowingError;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    public override void _ExitTree()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }

    public void NotifyFatalError(string technicalMessage)
    {
        GD.PrintErr(technicalMessage);
        if (_isShowingError) return;
        _isShowingError = true;

        var dialog = new AcceptDialog
        {
            Title = DialogTitle,
            DialogText = UserMessage,
            ProcessMode = ProcessModeEnum.Always
        };

        dialog.Confirmed += OnDialogClosed;
        dialog.CloseRequested += OnDialogClosed;

        AddChild(dialog);
        GetTree().Paused = true;
        dialog.PopupCentered();
    }

    public void NotifyFatalError(Result.Result result) => NotifyFatalError(result.Message);

    private void OnDialogClosed()
    {
        GetTree().Paused = false;
        GetTree().Quit();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        GD.PrintErr($"Unhandled exception: {e.ExceptionObject}");
        OS.Alert(UserMessage, DialogTitle);
        Environment.Exit(1);
    }
}