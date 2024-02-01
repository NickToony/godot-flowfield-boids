using Godot;
using System.Collections.Generic;

public partial class Unit : RigidBody2D
{
    private PathFinder _pathFinder;
    private Area2D _area2D;
    private Sprite2D _sprite;
    private List<Unit> _nearby = new();
    private Vector2 _velocity = new Vector2();
    private float _avoidDistance = 32;
    private float _speed = 2;
    private int _radius = 100;
    private float _cohesionForce = 0f;
    private float _alignForce = 0.2f;
    private float _separationForce = 1f;
    private float _mouseFollowForce = 0.8f;
    private bool _isOnWall = false;
    private Vector2 _drawPos = new();
    
    public override void _Ready()
    {
        _pathFinder = GetParent().GetNode<PathFinder>("/root/Main/PathFinder");
        _area2D = GetNode<Area2D>("Area2D");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _area2D.BodyEntered += (body) =>
        {
            if (body != this && body is Unit unit)
            {
                _nearby.Add(unit);
            }
        };
        _area2D.BodyExited += (body) =>
        {
            if (body != this && body is Unit unit)
            {
                _nearby.Remove(unit);
            }
        };
        _drawPos = Position;
    }

    public override void _Process(double delta)
    {
        _sprite.Position = _drawPos - Position;
    }

    public override void _PhysicsProcess(double delta)
    {
        _drawPos = (_drawPos * 0.8f) + (Position * 0.2f);
        
        var flockStatus = GetFlockStatus();

        // _isOnWall = _pathFinder.GetCost(Position) >= 255;
        var mouseVector = _pathFinder.GetDirection(Position) * _speed * _mouseFollowForce;
        var cohesionVector = flockStatus[0] * _cohesionForce;
        var alignVector = flockStatus[1] * _alignForce;
        var separationVector = flockStatus[2] * _separationForce;
        var boidForces = cohesionVector + alignVector + separationVector;

        _isOnWall = false;

        _velocity = mouseVector + boidForces;

        Modulate = Colors.White;
        var highestCostNearby = _pathFinder.GetHighestCostAround(Position);
        if (highestCostNearby is { neighbour: not null, cost: >= 255 })
        {
            var loopPos = Position + (highestCostNearby.neighbour.Value * 16);
            
            var obstaclePosition = loopPos;
            var obstacleDir = obstaclePosition.DirectionTo(Position);
            var obstacleSpeed = _speed;
            var multiplier = 1f;
            _velocity += obstacleDir * obstacleSpeed * multiplier;

            _isOnWall = true;
        }
                

        Position = Position.MoveToward(Position + _velocity, _speed);
    }
    
    private Vector2[] GetFlockStatus()
    {
        var centerVector = Vector2.Zero;
        var flockCenter = Vector2.Zero;
        var alignVector = Vector2.Zero;
        var avoidVector = Vector2.Zero;

        var count = 0;
        foreach (var neighbour in _nearby)
        {
            if (neighbour == this) continue;

            var neighbourPos = neighbour.Position;

            count += 1;
            alignVector += neighbour._velocity;
            flockCenter += neighbourPos;

            var distanceToNeighbour = Position.DistanceTo(neighbourPos);

            if (!(distanceToNeighbour < _avoidDistance)) continue;

            if (distanceToNeighbour > 0)
            {
                var speed = _speed * (_avoidDistance / distanceToNeighbour);
                var multiplier = _isOnWall ? 0.5f : (neighbour._isOnWall ? 2 : 1);
                avoidVector -= (neighbourPos - Position).Normalized() * speed * multiplier;
            }
            else
            {
                // No point generating an avoid distance here
                avoidVector -= new Vector2(0, 1).Rotated(GetIndex()) * _avoidDistance * 10;
            }
        }

        if (count > 0)
        {
            alignVector /= count;
            flockCenter /= count;

            var centerDir = Position.DirectionTo(flockCenter);
            var centerSpeed = _speed * (Position.DistanceTo(flockCenter) / _radius);
            centerVector = centerDir * centerSpeed;
        }

        return new[]
        {
            centerVector,
            alignVector,
            avoidVector,
        };
    }
}
