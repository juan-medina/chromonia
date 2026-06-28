// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

namespace Chromonia.Core;

public record AppError(bool Success, string Message)
{
    public static AppError Ok() => new(true, string.Empty);
    public static AppError Fail(string msg) => new(false, msg);
}
