// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;
using Environment = System.Environment;

namespace Chromonia.Core;

public partial class ErrorManager : Node
{
    private const string UserMessage = "Chromonia ran into a problem and needs to close.";
    private const string DialogTitle = "Unexpected Error";

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
        OS.Alert(UserMessage, DialogTitle);
        GetTree().Quit();
    }

    public void NotifyFatalError(Result result) => NotifyFatalError(result.Message);

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        GD.PrintErr($"Unhandled exception: {e.ExceptionObject}");
        OS.Alert(UserMessage, DialogTitle);
        Environment.Exit(1);
    }
}