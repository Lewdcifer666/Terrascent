using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terrascent.Core;

/// <summary>
/// 2D camera with smooth following, zoom, and screen-to-world coordinate conversion.
/// </summary>
public class Camera
{
    private Vector2 _position;
    private Vector2 _targetPosition;
    private float _zoom = 1f;
    private float _targetZoom = 1f;
    private readonly Viewport _viewport;

    // Configuration
    public float SmoothSpeed { get; set; } = 8f;
    public float ZoomSmoothSpeed { get; set; } = 6f;
    public float MinZoom { get; set; } = 0.25f;
    public float MaxZoom { get; set; } = 4f;

    /// <summary>
    /// Current camera position (center of view).
    /// </summary>
    public Vector2 Position => _position;

    /// <summary>
    /// Current zoom level (1 = normal, 2 = 2x zoom in, 0.5 = 2x zoom out).
    /// </summary>
    public float Zoom => _zoom;

    /// <summary>
    /// Visible area bounds in world coordinates.
    /// </summary>
    public Rectangle VisibleArea
    {
        get
        {
            var size = new Vector2(_viewport.Width, _viewport.Height) / _zoom;
            var topLeft = _position - size / 2f;
            return new Rectangle(
                (int)topLeft.X,
                (int)topLeft.Y,
                (int)size.X,
                (int)size.Y
            );
        }
    }

    public Camera(Viewport viewport)
    {
        _viewport = viewport;
        _position = Vector2.Zero;
        _targetPosition = Vector2.Zero;
    }

    /// <summary>
    /// Update camera position with smooth interpolation.
    /// Call this in VariableUpdate for smooth visuals.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Smooth position interpolation
        _position = Vector2.Lerp(_position, _targetPosition, SmoothSpeed * deltaTime);

        // Smooth zoom interpolation
        _zoom = MathHelper.Lerp(_zoom, _targetZoom, ZoomSmoothSpeed * deltaTime);
    }

    /// <summary>
    /// Set the target position for the camera to follow.
    /// </summary>
    public void Follow(Vector2 target)
    {
        _targetPosition = target;
    }

    /// <summary>
    /// Immediately center the camera on a position (no smoothing).
    /// </summary>
    public void CenterOn(Vector2 position)
    {
        _position = position;
        _targetPosition = position;
    }

    /// <summary>
    /// Adjust zoom level by a delta amount.
    /// </summary>
    public void AdjustZoom(float delta)
    {
        _targetZoom = MathHelper.Clamp(_targetZoom + delta, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Set zoom level directly.
    /// </summary>
    public void SetZoom(float zoom)
    {
        _targetZoom = MathHelper.Clamp(zoom, MinZoom, MaxZoom);
        _zoom = _targetZoom;
    }

    /// <summary>
    /// Get the transformation matrix for SpriteBatch.Begin().
    /// </summary>
    public Matrix GetTransformMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(-_position, 0f)) *
               Matrix.CreateScale(_zoom, _zoom, 1f) *
               Matrix.CreateTranslation(new Vector3(_viewport.Width / 2f, _viewport.Height / 2f, 0f));
    }

    /// <summary>
    /// Convert screen coordinates to world coordinates.
    /// Use this to find where the mouse is clicking in the world.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(
            screenPosition,
            Matrix.Invert(GetTransformMatrix())
        );
    }

    /// <summary>
    /// Convert world coordinates to screen coordinates.
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, GetTransformMatrix());
    }
}