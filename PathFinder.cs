using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class PathFinder : Node2D
{
    private static Vector2I UP_LEFT = Vector2I.Left + Vector2I.Up;
    private static Vector2I UP_RIGHT = Vector2I.Right + Vector2I.Up;
    private static Vector2I DOWN_LEFT = Vector2I.Left + Vector2I.Down;
    private static Vector2I DOWN_RIGHT = Vector2I.Right + Vector2I.Down;
    private static Vector2I UP = Vector2I.Up;
    private static Vector2I DOWN = Vector2I.Down;
    private static Vector2I LEFT = Vector2I.Left;
    private static Vector2I RIGHT = Vector2I.Right;
    
    
    private Map _map;
    private List<int> _costField;
    private int[] _integrationField;
    private Vector2I[] _flowField;
    private bool _debugRedraw = true;
    private bool _debug = true;
    private Vector2I _previousTarget = Vector2I.Zero;
    private Vector2I _target = Vector2I.Zero;

    private float _diagWeight = (float) Math.Sqrt(2) - 1;
    
    public static List<Vector2I> DirectNeighbours = new List<Vector2I>()
    {
        LEFT,
        DOWN,
        UP,
        RIGHT,
        // UP_LEFT,
        // UP_RIGHT,
        // DOWN_LEFT,
        // DOWN_RIGHT,
    };
    
    public static List<Vector2I> AllNeighbours = new List<Vector2I>()
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

    public override void _Ready()
    {
        _map = GetParent().GetNode<Map>("Map");
        _integrationField = new int[_map.MapSize.X * _map.MapSize.Y];
        _flowField = new Vector2I[_map.MapSize.X * _map.MapSize.Y];

        GenerateCostField();
    }

    public override void _Process(double delta)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            _target = _map.LocalToMap(GetGlobalMousePosition());
        }
        
        if (_debug && _debugRedraw)
        {
            QueueRedraw();
            _debugRedraw = false;
        }

        if (_map.Changed)
        {
            GenerateCostField();
            _map.Changed = false;
            _previousTarget = Vector2I.Zero;
        }

        if (_target != _previousTarget)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            GenerateIntegrationField();
            var timeForIntegration = watch.ElapsedMilliseconds;
            GenerateFlowField();
            var timeForFlowField = watch.ElapsedMilliseconds - timeForIntegration;
            watch.Stop();
            _previousTarget = _target;
            GD.Print("Time for Integration: " + timeForIntegration);
            GD.Print("Time for Flow Field: " + timeForFlowField);
        }
        
    }

    public void GenerateCostField()
    {
        _costField = new List<int>(_map.MapSize.X * _map.MapSize.Y);
        for (var x = 0; x < _map.MapSize.X; x += 1)
        {
            for (var y = 0; y < _map.MapSize.Y; y += 1)
            {
                var cell = _map.GetCellAtlasCoords(0, new Vector2I(x, y));
                var cost = cell.X switch
                {
                    1 => 3,
                    2 => 255,
                    _ => 1
                };

                if (cost == 1)
                {
                    foreach (var neighbour in AllNeighbours)
                    {
                        if (_map.GetCellAtlasCoords(0, new Vector2I(x, y) + neighbour).X == 2)
                        {
                            cost += 5;
                            break;
                        }
                    }
                }

                _costField.Add(cost);
            }
        }

        _debugRedraw = true;
    }

    public (Vector2I? neighbour, int cost) GetHighestCostAround(Vector2 pos)
    {
        return GetHighestCostAroundGrid(_map.LocalToMap(pos));
    }

    public (Vector2I? neighbour, int cost) GetHighestCostAroundGrid(Vector2I gridPos)
    {
        var highestCost = 0;
        Vector2I? highestNeighbour = null;

        foreach (var neighbour in AllNeighbours)
        {
            var neighbourIndex = Vector2ToIndex(neighbour + gridPos);
            if (neighbourIndex < 0 || neighbourIndex > _costField.Count - 1) continue;
            var cost = _costField[neighbourIndex];
            if (cost > highestCost)
            {
                highestCost = cost;
                highestNeighbour = neighbour;
            }
        }
        
        return (highestNeighbour, highestCost);
    }

    public void GenerateIntegrationField()
    {
        for (var x = 0; x < _map.MapSize.X; x += 1)
        {
            for (var y = 0; y < _map.MapSize.Y; y += 1)
            {
                _integrationField[Vector2ToIndex(new Vector2I(x, y))] = 999;
            }
        }
        
        _integrationField[Vector2ToIndex(_target)] = 0;
        var openList = new Queue<Vector2I>();
        openList.Enqueue(_target);
        
        while (openList.Count > 0)
        {

            var current = openList.Dequeue();
            var currentIndex = Vector2ToIndex(current);
            var currentIntegration = _integrationField[currentIndex];
            foreach (var neighbour in DirectNeighbours)
            {
                var neighbourPoint = neighbour + current;
                var neighbourIndex = Vector2ToIndex(neighbourPoint);
                if (neighbourIndex < 0 || neighbourIndex > _integrationField.Length - 1) continue;
                
                
                var neighbourIntegration = _integrationField[neighbourIndex];
                var cost = _costField[neighbourIndex];
                
                
                if (neighbourIntegration <= currentIntegration + cost) continue;
                // if (neighbourIntegration != 999) continue;
                
                _integrationField[neighbourIndex] = currentIntegration + cost;
                openList.Enqueue(neighbourPoint);
            }
        }

        _debugRedraw = true;
    }

    public Vector2 GetDirection(Vector2 position)
    {
        if (_flowField == null) return Vector2I.Zero;

        var index = Vector2ToIndex(_map.LocalToMap(position));
        if (index < 0 || index > _flowField.Length - 1) return Vector2I.Zero;
        return _flowField[index];
    }

    private void GenerateFlowField()
    {
        for (var x = 0; x < _map.MapSize.X; x += 1)
        {
            for (var y = 0; y < _map.MapSize.Y; y += 1)
            {
                var current = new Vector2I(x, y);
                var currentIndex = Vector2ToIndex(current);
                float lowestCost = 999;
                foreach (var neighbour in AllNeighbours)
                {
                    var index = Vector2ToIndex(neighbour + current);
                    if (index < 0 || index > _integrationField.Length - 1) continue;
                    
                    var cost = (float) _integrationField[index];
                    if (neighbour.X == 0 || neighbour.Y == 0)
                    {
                        cost += _diagWeight;
                    }
                    if (cost < lowestCost)
                    {
                        _flowField[currentIndex] = neighbour;
                        lowestCost = cost;
                    }
                }
            }
        }
    }
    
    private int Vector2ToIndex(Vector2I vector)
    {
        return vector.X * _map.MapSize.X + vector.Y;
    }
    
    private Vector2I IndexToVector2(int index)
    {
        return new Vector2I(index / _map.MapSize.X, index % _map.MapSize.X);
    }

    public override void _Draw()
    {
        if (!_debug) return;
        
        
        for (var x = 0; x < _map.MapSize.X; x += 1)
        {
            for (var y = 0; y < _map.MapSize.Y; y += 1)
            {
                var index = Vector2ToIndex(new Vector2I(x, y));
                var cost = _costField[index];
                var integration = _integrationField != null ? _integrationField[index] : -1;
                var text = $"{cost} / {integration}";
                DrawString(ThemeDB.FallbackFont, new Vector2I(x, y + 1) * 32, text, HorizontalAlignment.Left, -1f, 8);
                
                if (_flowField != null)
                {
                    var flow = _flowField[index];
                    var middle = (new Vector2I(x, y) * 32) + new Vector2(16, 16);
                    var final = middle + (flow * 16);
                    DrawLine(middle, final, Colors.Red);
                }
            }
        }
    }


    public int GetCost(Vector2 position)
    {
        if (_costField == null) return 255;
        var index = Vector2ToIndex(_map.LocalToMap(position));
        if (index < 0 || index > _costField.Count - 1) return 255;
        return _costField[index];
    }
}
