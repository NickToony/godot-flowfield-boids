using Godot;
using System.Collections.Generic;

public partial class Unit : RigidBody2D
{
    private PathFinder _pathFinder;
    private Area2D _area2D;
    private Sprite2D _sprite;
    
    private Vector2 _velocity = new Vector2();
    private float _speed = 2; // pixels per physics frame
    private int _radius = 50; // radius to consider another unit as part of flock
    private float _avoidDistance = 32; // distance to gently push other units away
    private float _spriteSmoothing = 0.8f; // smooth out jitter
    
    private float _cohesionForce = 0f; // cohesion wasn't super useful here
    private float _alignForce = 0.4f; // Stops units constantly walking into each other
    private float _separationForce = 0.4f; // Force units apart (not always possible)
    private float _mouseFollowForce = 0.4f; // Path following force. We can be lenient because flow field compensates

    private readonly List<Unit> _nearby = new();
    
    private bool _isOnWall = false;
    private Vector2 _drawPos = new();
    private Vector2 _alignVector = new();
    private Vector2 _cohesionVector = new();
    private Vector2 _separationVector = new();
    
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
        _drawPos = (_drawPos * _spriteSmoothing) + (Position * (1 - _spriteSmoothing));
        
        UpdateFlock();

        // Direction from flow field
        var pathDirection = (Vector2) _pathFinder.GetDirection(Position);
        // Calculate path follow force
        var mouseVector = pathDirection * _speed * _mouseFollowForce;
        var cohesionVector = _cohesionVector * _cohesionForce; // force to keep units together
        var alignVector = _alignVector * _alignForce; // force to align units
        var separationVector = _separationVector * _separationForce; // force to push units apart
        var boidForces = cohesionVector + alignVector + separationVector; // combined to become boids calc

        // Movement velocity before walls
        _velocity = mouseVector + boidForces;
        
        _isOnWall = false;

        var highestCostNearby = _pathFinder.GetHighestCostAround(Position);
        // If we're near a wall
        if (highestCostNearby is { neighbour: not null, cost: >= 255 })
        {
            // Push away from the direction of wall
            var obstaclePosition = Position + (highestCostNearby.neighbour.Value * 16);
            var directionFromObstacle = obstaclePosition.DirectionTo(Position);
            _velocity += directionFromObstacle * _speed;

            // Flock logic may adjust
            _isOnWall = true;
        }
                

        Position = Position.MoveToward(Position + _velocity * 10, _speed);
        // Position = Position + (_velocity.Normalized() * _speed);

    }
    
    private void UpdateFlock()
    {
        var flockCenter = Vector2.Zero;
        _cohesionVector = Vector2.Zero;
        _alignVector = Vector2.Zero;
        _separationVector = Vector2.Zero;

        var count = 0;
        foreach (var neighbour in _nearby)
        {
            // Don't count yourself
            if (neighbour == this) continue;

            var neighbourPos = neighbour.Position;

            count += 1;
            _alignVector += neighbour._velocity;
            flockCenter += neighbourPos;

            var distanceToNeighbour = Position.DistanceTo(neighbourPos);

            if (distanceToNeighbour > _avoidDistance) continue;

            if (distanceToNeighbour > 0)
            {
                var speed = _speed * (_avoidDistance / distanceToNeighbour);
                // If we/they are on a wall, we want to avoid shoving them further into the wall
                var multiplier = _isOnWall ? 0.5f : (neighbour._isOnWall ? 2 : 1);
                _separationVector -= (neighbourPos - Position).Normalized() * speed * multiplier;
            }
            else
            {
                // We're exactly on top of the other neighbour
                // You could rotate to a random direction here, but that wouldn't be deterministic
                _separationVector -= new Vector2(0, 1).Rotated(GetIndex()) * _avoidDistance * 10;
            }
        }

        // If no flockmates met criteria, no further calculations required
        if (count <= 0) return;
        
        _alignVector /= count;
        flockCenter /= count;

        // We want to move towards the center of our flockmates
        var centerDir = Position.DirectionTo(flockCenter);
        var centerSpeed = _speed * (Position.DistanceTo(flockCenter) / _radius);
        _cohesionVector = centerDir * centerSpeed;
    }
}
