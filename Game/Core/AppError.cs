// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

namespace Chromonia.Core;

public readonly struct AppError
{
    private readonly bool _success;
    public readonly string Message;

    private AppError(bool success, string message)
    {
        _success = success;
        Message = message;
    }

    public static AppError Ok() => new(true, string.Empty);
    public static AppError Fail(string message) => new(false, message);
    public static implicit operator bool(AppError e) => e._success;
}