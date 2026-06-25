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

    public abstract class A
    {
        public static readonly Color Line = new(2.25f, 0.0f, 0.0f);
        public static readonly Color Fill = new(2.25f, 0.6f, 0.2f);
    }

    public abstract class B
    {
        public static readonly Color Line = new(0.0f, 0.0f, 2.25f);
        public static readonly Color Fill = new(0.2f, 0.6f, 2.5f);
    }
}