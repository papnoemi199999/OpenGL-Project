using Silk.NET.Maths;
using Szeminarium1_24_02_17_2;

enum Direction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}


class MovingArrow
{
    public GlArrow GlArrow { get; }
    public Vector3D<float> Position { get; set; }
    public Direction Direction { get; }
    private float Speed;
    
    // Arrow init
    public MovingArrow(GlArrow arrow, Vector3D<float> startPos, Direction dir, float speed = 1.0f)
    {
        GlArrow = arrow;
        Position = startPos;
        Direction = dir;
        Speed = speed;
    }

    // update the arrow's pos based on its direction and speed
    public void Update(float deltaTime)
    {
        Vector3D<float> dirVec = Direction switch
        {
            Direction.Left => new Vector3D<float>(-1, 0, 0), // left  
            Direction.Right => new Vector3D<float>(1, 0, 0), // right
            Direction.Up => new Vector3D<float>(0, 0, 1), // forward
            Direction.Down => new Vector3D<float>(0, 0, -1), // backward
            _ => new Vector3D<float>(0, 0, 0)
        };

        Position += dirVec * Speed * deltaTime;
    }

    // returns the modified arrow
    public Matrix4X4<float> GetTransformMatrix()
    {
        // set rotation based on direction
        var rotation = Direction switch
        {
            Direction.Up => Matrix4X4.CreateRotationY<float>(0),
            Direction.Down => Matrix4X4.CreateRotationY<float>((float)Math.PI),
            Direction.Left => Matrix4X4.CreateRotationY<float>(-(float)Math.PI / 2),
            Direction.Right => Matrix4X4.CreateRotationY<float>((float)Math.PI / 2),
            _ => Matrix4X4<float>.Identity
        };

        return Matrix4X4.CreateScale<float>(0.9f) *
               Matrix4X4.CreateRotationX<float>((float)Math.PI / -2) *
               rotation *
               Matrix4X4.CreateTranslation(Position);
    }

}
