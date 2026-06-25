using Godot;

namespace Chromonia.Scripts;

public class Energy(Energy.Tint tint = Energy.Tint.A)
{
    public enum Tint
    {
        A,
        B,
    }

    public bool Primary => CurrentTint == Tint.A;

    public Tint CurrentTint { get; set; } = tint;

    public Color Line => CurrentTint == Tint.A ? A.Line : B.Line;
    public Color Fill => CurrentTint == Tint.A ? A.Fill : B.Fill;
    public Color Marker => CurrentTint == Tint.A ? A.Marker : B.Marker;

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
        public static readonly Color Line = new(2.2f, 0.4f, 1.5f);
        // Neon Magenta Fill (strong glow)
        public static readonly Color Fill = new(1.8f, 0.0f, 1.4f);
        // Bright pastel Magenta (non-glowing, for UI markers)
        public static readonly Color Marker = new(1.0f, 0.6f, 0.9f);
    }
}