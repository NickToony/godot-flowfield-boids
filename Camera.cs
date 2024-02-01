using Godot;

namespace kingdoms.components.camera;

public partial class Camera : Camera2D
{
    [Export] private float _speed = 5f;
    [Export] private float _zoomSpeed = 0.8f;
    [Export] private float _minZoom = 0.2f;
    [Export] private float _maxZoom = 2.0f;

    private float? _targetZoom = null;
    // private Polygon2D _background;

    public override void _Ready()
    {
        // _background = GetNode<Polygon2D>("Background");
        // UpdateBackground();
    }

    public override void _Process(double delta)
    {
        var moveLeft = Input.IsActionPressed("camera_left");
        var moveRight = Input.IsActionPressed("camera_right");
        var moveUp = Input.IsActionPressed("camera_up");
        var moveDown = Input.IsActionPressed("camera_down");

        Position += new Vector2(
            (moveRight ? 1 : 0) + (moveLeft ? -1 : 0),
            (moveDown ? 1 : 0) + (moveUp ? -1 : 0)
        ) * _speed;

        if (moveLeft || moveRight || moveUp || moveDown)
        {
            _targetZoom = null;
        }

        var zoomIn = Input.IsActionJustReleased("camera_zoom_in");
        var zoomOut = Input.IsActionJustReleased("camera_zoom_out");

        if (zoomOut)
        {
            Zoom *= _zoomSpeed;
            _targetZoom = null;
        }
        if (zoomIn)
        {
            Zoom /= _zoomSpeed;
            _targetZoom = null;
        }

        if (_targetZoom.HasValue)
        {
            Zoom = Zoom.Lerp(new Vector2(_targetZoom.Value, _targetZoom.Value), 0.1f);
        }

        var clamped = Mathf.Clamp(Zoom.X, _minZoom, _maxZoom);
        Zoom = new Vector2(clamped, clamped);
        UpdateBackground();
    }

    public void SetTargetZoom(float zoom)
    {
        _targetZoom = zoom;
    }

    public void SetTargetPosition(Vector2 position)
    {
        Position = position;
    }

    private void UpdateBackground()
    {
        // var viewportSize = GetViewport().GetVisibleRect().Size / Zoom;
        // _background.Scale = viewportSize;
        // _background.Position = -viewportSize / 2;
    }
}