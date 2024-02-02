using Godot;
using System.Collections.Generic;
using kingdoms.components.camera;

public partial class PathFinder : Node2D
{
    [Export] private Label _debugLabel;
    [Export] private Label _performanceLabel;
    
    enum DebugMode {
        None,
        All,
        Cost,
        Integration,
        Flow,
    }
    
    private Map _map;
    
    private int[] _costField;
    private int[] _integrationField;
    private Vector2I[] _flowField;

    private Vector2I _previousTarget = Vector2I.Zero;
    private Vector2I _target = Vector2I.Zero;
    
    private bool _debugRedraw = true;
    private DebugMode _debugMode = DebugMode.None;

    private long _lastIntegrationTime = 0;
    private long _lastFlowFieldTime = 0;
    private long _totalTime;

    public override void _Ready()
    {
        _map = GetParent().GetNode<Map>("Map");
        _costField = new int[_map.MapSize.X * _map.MapSize.Y];
        _integrationField = new int[_map.MapSize.X * _map.MapSize.Y];
        _flowField = new Vector2I[_map.MapSize.X * _map.MapSize.Y];
    }

    public override void _Process(double delta)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            _target = _map.LocalToMap(GetGlobalMousePosition());
        }

        if (Input.IsActionJustPressed("debug_toggle"))
        {
            _debugMode += 1;
            if (_debugMode > DebugMode.Flow)
            {
                _debugMode = DebugMode.None;
            }
            
            _debugRedraw = true;
        }
        
        if (_debugRedraw)
        {
            QueueRedraw();
            _debugRedraw = false;
            
            if (_debugLabel != null)
            {
                _debugLabel.Text = "(" + _debugMode + ")";
            }
        }

        if (_map.MapChanged)
        {
            GenerateCostField();
            _map.MapChanged = false;
            _previousTarget = Vector2I.Zero;
        }

        if (_target != _previousTarget)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            GenerateIntegrationField();
            _lastIntegrationTime = watch.ElapsedMilliseconds;
            GenerateFlowField();
            watch.Stop();
            _totalTime = watch.ElapsedMilliseconds;
            _lastFlowFieldTime = watch.ElapsedMilliseconds - _lastIntegrationTime;
            _previousTarget = _target;
        }

        if (_performanceLabel != null)
        {
            _performanceLabel.Text = $"Integration: {_lastIntegrationTime}ms\nFlow Field: {_lastFlowFieldTime}ms\nTotal: {_totalTime}ms";
        }
        
    }

    public void GenerateCostField()
    {
        for (var x = 0; x < _map.MapSize.X; x += 1)
        {
            for (var y = 0; y < _map.MapSize.Y; y += 1)
            {
                var coords = new Vector2I(x, y);
                var cell = _map.GetCellAtlasCoords(0, coords);
                var cost = cell.X switch
                {
                    1 => 3,
                    2 => 255,
                    _ => 1
                };

                if (cost == 1)
                {
                    foreach (var neighbour in Constants.AllNeighbours)
                    {
                        if (_map.GetCellAtlasCoords(0, coords + neighbour).X == 2)
                        {
                            cost += 5;
                            break;
                        }
                    }
                }

                _costField[Vector2ToIndex(coords)] = cost;
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

        foreach (var neighbour in Constants.AllNeighbours)
        {
            var neighbourIndex = Vector2ToIndex(neighbour + gridPos);
            if (neighbourIndex < 0 || neighbourIndex > _costField.Length - 1) continue;
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
            foreach (var neighbour in Constants.DirectNeighbours)
            {
                var neighbourPoint = neighbour + current;
                var neighbourIndex = Vector2ToIndex(neighbourPoint);
                if (neighbourIndex < 0 || neighbourIndex > _integrationField.Length - 1) continue;
                
                
                var neighbourIntegration = _integrationField[neighbourIndex];
                var cost = _costField[neighbourIndex];

                if (cost >= 255) continue;
                if (neighbourIntegration <= currentIntegration + cost) continue;
                
                _integrationField[neighbourIndex] = currentIntegration + cost;
                openList.Enqueue(neighbourPoint);
            }
        }

        _debugRedraw = true;
    }

    public Vector2I GetDirection(Vector2 position)
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
                foreach (var neighbour in Constants.AllNeighbours)
                {
                    var index = Vector2ToIndex(neighbour + current);
                    if (index < 0 || index > _integrationField.Length - 1) continue;
                    
                    var cost = (float) _integrationField[index];
                    if (neighbour.X == 0 || neighbour.Y == 0)
                    {
                        cost += Constants.DiagWeight;
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
        if (_debugMode == DebugMode.None) return;
        
        
        for (var x = 0; x < _map.MapSize.X; x += 1)
        {
            for (var y = 0; y < _map.MapSize.Y; y += 1)
            {
                var index = Vector2ToIndex(new Vector2I(x, y));
                var cost = _costField[index];
                var integration = _integrationField != null ? _integrationField[index] : -1;

                var text = "";
                switch (_debugMode)
                {
                    case DebugMode.All:
                        text = $"{cost} / {integration}";
                        break;
                    case DebugMode.Cost:
                        text = $"{cost}";
                        break;
                    case DebugMode.Integration:
                        text = $"{integration}";
                        break;
                    
                }

                if (text.Length > 0)
                {
                    DrawString(ThemeDB.FallbackFont, new Vector2I(x, y + 1) * 32, text, HorizontalAlignment.Left, -1f, 8);
                }
                
                
                if (_flowField != null && (_debugMode == DebugMode.All || _debugMode == DebugMode.Flow))
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
        if (index < 0 || index > _costField.Length - 1) return 255;
        return _costField[index];
    }
}
