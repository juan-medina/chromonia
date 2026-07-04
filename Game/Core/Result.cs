// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

namespace Chromonia.Core;

public readonly struct Result
{
    private readonly bool _success;
    public readonly string Message;

    private Result(bool success, string message)
    {
        _success = success;
        Message = message;
    }

    public static Result Ok() => new(true, string.Empty);
    public static Result Fail(string message) => new(false, message);
    
    public static implicit operator bool(Result r) => r._success;
}

public readonly struct Result<T>
{
    public readonly T Value;
    public readonly string ErrorMessage;
    private readonly bool _success;

    private Result(bool success, T value, string errorMessage)
    {
        _success = success;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Ok(T value) => new(true, value, string.Empty);
    public static Result<T> Fail(string message) => new(false, default!, message);

    public static implicit operator bool(Result<T> r) => r._success;
}
