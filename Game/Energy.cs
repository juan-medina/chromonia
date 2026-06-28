// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Game;

public class Energy(Energy.Tint tint = Energy.Tint.A)
{
    public enum Tint
    {
        A,
        B,
        Combined
    }

    public bool Primary => CurrentTint == Tint.A;

    public Tint CurrentTint { get; set; } = tint;

    public Color Line => CurrentTint switch { Tint.A => A.Line, Tint.B => B.Line, _ => Combined.Line };
    public Color Fill => CurrentTint switch { Tint.A => A.Fill, Tint.B => B.Fill, _ => Combined.Fill };
    public Color Marker => CurrentTint switch { Tint.A => A.Marker, Tint.B => B.Marker, _ => Combined.Marker };

    public void Cycle()
    {
        CurrentTint = CurrentTint switch
        {
            Tint.A => Tint.B,
            Tint.B => Tint.A,
            _ => CurrentTint
        };
    }

    public abstract class A
    {
        // Glowing Neon Cyan Line (for drawing)
        public static readonly Color Line = new(0.6f, 2.2f, 2.2f);

        // Neon Cyan Fill (strong glow)
        public static readonly Color Fill = new(0.0f, 1.8f, 1.8f);

        // Bright pastel Cyan (non-glowing, for UI markers)
        public static readonly Color Marker = new(0.6f, 1.0f, 1.0f);
    }

    public abstract class B
    {
        // Glowing Neon Magenta Line (for drawing)
        public static readonly Color Line = new(2.4f, 0.2f, 2.4f);

        // Neon Magenta Fill (strong glow)
        public static readonly Color Fill = new(2.0f, 0.0f, 2.0f);

        // Bright pastel Magenta (non-glowing, for UI markers)
        public static readonly Color Marker = new(1.0f, 0.4f, 1.0f);
    }

    public abstract class Combined
    {
        // Glowing White/Yellow Line (Danger)
        public static readonly Color Line = new(2.2f, 2.2f, 1.2f);

        // Neon White/Yellow Fill (Danger)
        public static readonly Color Fill = new(1.8f, 1.8f, 0.8f);

        // Bright pastel White/Yellow
        public static readonly Color Marker = new(1.0f, 1.0f, 0.8f);
    }
}