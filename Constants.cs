using System;
using System.Collections.Generic;
using Godot;

namespace kingdoms.components.camera;

public class Constants
{
    public readonly static Vector2I UP_LEFT = Vector2I.Left + Vector2I.Up;
    public readonly static Vector2I UP_RIGHT = Vector2I.Right + Vector2I.Up;
    public readonly static Vector2I DOWN_LEFT = Vector2I.Left + Vector2I.Down;
    public readonly static Vector2I DOWN_RIGHT = Vector2I.Right + Vector2I.Down;
    public readonly static Vector2I UP = Vector2I.Up;
    public readonly static Vector2I DOWN = Vector2I.Down;
    public readonly static Vector2I LEFT = Vector2I.Left;
    public readonly static Vector2I RIGHT = Vector2I.Right;
    
    public static float DiagWeight = (float) Math.Sqrt(2) - 1;

    
    public readonly static List<Vector2I> DirectNeighbours = new()
    {
        LEFT,
        DOWN,
        UP,
        RIGHT,
    };
    
    public readonly static List<Vector2I> AllNeighbours = new()
    {
        LEFT,
        DOWN,
        UP,
        RIGHT,
        UP_LEFT,
        UP_RIGHT,
        DOWN_LEFT,
        DOWN_RIGHT,
    };
}