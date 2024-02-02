using Godot;
using System;

public partial class Map : TileMap
{
    public Vector2I MapSize = new(100, 100);
    public bool MapChanged = true;

    public override void _Ready()
    {
        MapSize = GetUsedRect().Size;
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Key1))
        {
            SetCell(0, LocalToMap(GetGlobalMousePosition()), 0, new Vector2I(0, 0));
            MapChanged = true;
        }
        if (Input.IsKeyPressed(Key.Key2))
        {
            SetCell(0, LocalToMap(GetGlobalMousePosition()), 0, new Vector2I(1, 0));
            MapChanged = true;
        }
        if (Input.IsKeyPressed(Key.Key3))
        {
            SetCell(0, LocalToMap(GetGlobalMousePosition()), 0, new Vector2I(2, 0));
            MapChanged = true;
        }
    }
}
