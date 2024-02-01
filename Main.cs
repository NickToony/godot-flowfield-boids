using Godot;
using System;

public partial class Main : Node2D
{
    private PackedScene _unitScene = GD.Load<PackedScene>("res://Unit.tscn");

    private Map _map;
    
    public override void _Ready()
    {
        _map = GetNode<Map>("Map");
    }

    public override void _Process(double delta)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var unit = _unitScene.Instantiate<Unit>();
            unit.Position = GetGlobalMousePosition();
            AddChild(unit);
        }
    }
}
