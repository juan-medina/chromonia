using Godot;

namespace Chromonia.Scripts;

public partial class BlobEnemy : RigidBody2D
{
    public Energy BlobEnergy { get; } = new();
    public Energy.Tint BaseTint { get; private set; }
    public float Radius { get; set; } = 40f;
    private float _speed;

    public BlobEnemy(Energy.Tint tint, float speed)
    {
        BaseTint = tint;
        BlobEnergy.CurrentTint = tint;
        _speed = speed;

        // Collision layers:
        // Layer 2 = Blob layer (what I am)
        // Mask 1 = Default environment layer (what I bounce against)
        CollisionLayer = 2;
        CollisionMask = 1;

        GravityScale = 0;
        LinearDampMode = DampMode.Replace;
        LinearDamp = 0;
        AngularDampMode = DampMode.Replace;
        AngularDamp = 0;

        PhysicsMaterialOverride = new PhysicsMaterial
        {
            Bounce = 1.0f,
            Friction = 0.0f
        };

        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = this.Radius }
        };
        AddChild(shape);

        // Randomize initial direction
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        LinearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
    }

    public BlobEnemy()
    {
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, BlobEnergy.Fill);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, BlobEnergy.Line, 3f);
    }
}