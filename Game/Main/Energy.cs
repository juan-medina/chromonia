// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Main;

public enum Energy
{
    A,
    B,
    Combined
}

public static class EnergyExtensions
{
    // A - Cyan
    private static readonly Color LineA = new(0.6f, 2.2f, 2.2f);
    private static readonly Color FillA = new(0.0f, 1.8f, 1.8f);
    private static readonly Color MarkerA = new(0.6f, 2.2f, 2.2f);

    // B - Magenta
    private static readonly Color LineB = new(2.4f, 0.2f, 2.4f);
    private static readonly Color FillB = new(2.0f, 0.0f, 2.0f);
    private static readonly Color MarkerB = new(2.4f, 0.2f, 2.4f);

    // Combined - Yellow/White
    private static readonly Color LineCombined = new(2.2f, 2.2f, 1.2f);
    private static readonly Color FillCombined = new(2.2f, 2.2f, 1.2f);
    private static readonly Color MarkerCombined = new(2.2f, 2.2f, 1.2f);

    public static Color Line(this Energy type) => type switch
    {
        Energy.A => LineA,
        Energy.B => LineB,
        _ => LineCombined
    };

    public static Color Fill(this Energy type) => type switch
    {
        Energy.A => FillA,
        Energy.B => FillB,
        _ => FillCombined
    };

    public static Color Marker(this Energy type) => type switch
    {
        Energy.A => MarkerA,
        Energy.B => MarkerB,
        _ => MarkerCombined
    };

    public static Energy Cycle(this Energy type) => type switch
    {
        Energy.A => Energy.B,
        Energy.B => Energy.A,
        _ => type
    };
}