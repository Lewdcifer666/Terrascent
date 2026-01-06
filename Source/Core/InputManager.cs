using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terrascent.Core;

/// <summary>
/// Handles all input state tracking with current/previous frame comparison.
/// Provides methods for detecting key presses, releases, and holds.
/// </summary>
public class InputManager
{
    // Keyboard state
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;

    // Mouse state
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    /// <summary>
    /// Current mouse position in screen coordinates.
    /// </summary>
    public Point MousePosition => _currentMouse.Position;

    /// <summary>
    /// Mouse position as Vector2 for calculations.
    /// </summary>
    public Vector2 MousePositionV => _currentMouse.Position.ToVector2();

    /// <summary>
    /// Change in scroll wheel since last frame.
    /// </summary>
    public int ScrollWheelDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

    /// <summary>
    /// Must be called at the start of each frame to update input states.
    /// </summary>
    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }

    #region Keyboard Methods

    /// <summary>
    /// Returns true while the key is held down.
    /// </summary>
    public bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);

    /// <summary>
    /// Returns true while the key is not pressed.
    /// </summary>
    public bool IsKeyUp(Keys key) => _currentKeyboard.IsKeyUp(key);

    /// <summary>
    /// Returns true only on the frame the key was first pressed.
    /// </summary>
    public bool IsKeyPressed(Keys key) =>
        _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    /// <summary>
    /// Returns true only on the frame the key was released.
    /// </summary>
    public bool IsKeyReleased(Keys key) =>
        _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);

    /// <summary>
    /// Gets horizontal movement input (-1 left, 0 none, 1 right).
    /// </summary>
    public int GetHorizontalAxis()
    {
        int axis = 0;
        if (IsKeyDown(Keys.A) || IsKeyDown(Keys.Left)) axis -= 1;
        if (IsKeyDown(Keys.D) || IsKeyDown(Keys.Right)) axis += 1;
        return axis;
    }

    /// <summary>
    /// Gets vertical movement input (-1 up, 0 none, 1 down).
    /// </summary>
    public int GetVerticalAxis()
    {
        int axis = 0;
        if (IsKeyDown(Keys.W) || IsKeyDown(Keys.Up)) axis -= 1;
        if (IsKeyDown(Keys.S) || IsKeyDown(Keys.Down)) axis += 1;
        return axis;
    }

    #endregion

    #region Mouse Methods

    /// <summary>
    /// Returns true while left mouse button is held.
    /// </summary>
    public bool IsLeftMouseDown() => _currentMouse.LeftButton == ButtonState.Pressed;

    /// <summary>
    /// Returns true while right mouse button is held.
    /// </summary>
    public bool IsRightMouseDown() => _currentMouse.RightButton == ButtonState.Pressed;

    /// <summary>
    /// Returns true only on the frame left mouse was clicked.
    /// </summary>
    public bool IsLeftMousePressed() =>
        _currentMouse.LeftButton == ButtonState.Pressed &&
        _previousMouse.LeftButton == ButtonState.Released;

    /// <summary>
    /// Returns true only on the frame right mouse was clicked.
    /// </summary>
    public bool IsRightMousePressed() =>
        _currentMouse.RightButton == ButtonState.Pressed &&
        _previousMouse.RightButton == ButtonState.Released;

    /// <summary>
    /// Returns true only on the frame left mouse was released.
    /// </summary>
    public bool IsLeftMouseReleased() =>
        _currentMouse.LeftButton == ButtonState.Released &&
        _previousMouse.LeftButton == ButtonState.Pressed;

    /// <summary>
    /// Returns true only on the frame right mouse was released.
    /// </summary>
    public bool IsRightMouseReleased() =>
        _currentMouse.RightButton == ButtonState.Released &&
        _previousMouse.RightButton == ButtonState.Pressed;

    #endregion
}