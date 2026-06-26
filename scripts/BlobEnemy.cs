using Godot;

namespace Chromonia.Scripts;

public partial class BlobEnemy : Node2D
{
    private Energy BlobEnergy { get; } = new();
    private float Radius { get; set; } = 40f;
    private Vector2 Velocity { get; set; }
    private Rect2 Bounds { get; set; }

    public BlobEnemy(Energy.Tint tint, Rect2 bounds, float speed)
    {
        BlobEnergy.CurrentTint = tint;
        Bounds = bounds;

        // Randomize initial direction
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
    }

    public BlobEnemy()
    {
    }

    public override void _Process(double delta)
    {
        Vector2 pos = Position;
        pos += Velocity * (float)delta;

        // Bounce horizontally
        if (pos.X - Radius < Bounds.Position.X)
        {
            pos.X = Bounds.Position.X + Radius;
            Velocity = new Vector2(Mathf.Abs(Velocity.X), Velocity.Y);
        }
        else if (pos.X + Radius > Bounds.End.X)
        {
            pos.X = Bounds.End.X - Radius;
            Velocity = new Vector2(-Mathf.Abs(Velocity.X), Velocity.Y);
        }

        // Bounce vertically
        if (pos.Y - Radius < Bounds.Position.Y)
        {
            pos.Y = Bounds.Position.Y + Radius;
            Velocity = new Vector2(Velocity.X, Mathf.Abs(Velocity.Y));
        }
        else if (pos.Y + Radius > Bounds.End.Y)
        {
            pos.Y = Bounds.End.Y - Radius;
            Velocity = new Vector2(Velocity.X, -Mathf.Abs(Velocity.Y));
        }

        Position = pos;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, BlobEnergy.Fill);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, BlobEnergy.Line, 3f);
    }
}