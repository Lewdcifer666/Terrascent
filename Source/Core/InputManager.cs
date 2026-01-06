using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terrascent.Core;

/// <summary>
/// Handles all input state tracking with current/previous frame comparison.
/// Buffers key presses to ensure they aren't missed between fixed updates.
/// </summary>
public class InputManager
{
    // Keyboard state
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;

    // Mouse state
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    // Buffered key presses (persists until consumed)
    private HashSet<Keys> _bufferedKeyPresses = new();
    private HashSet<Keys> _consumedThisFrame = new();

    // Buffered mouse presses
    private bool _leftMousePressBuffered;
    private bool _rightMousePressBuffered;

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

        // Clear consumed keys from last fixed update
        _consumedThisFrame.Clear();

        // Buffer any new key presses
        foreach (Keys key in Enum.GetValues<Keys>())
        {
            if (_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key))
            {
                _bufferedKeyPresses.Add(key);
            }
        }

        // Buffer mouse presses
        if (_currentMouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Released)
        {
            _leftMousePressBuffered = true;
        }

        if (_currentMouse.RightButton == ButtonState.Pressed &&
            _previousMouse.RightButton == ButtonState.Released)
        {
            _rightMousePressBuffered = true;
        }
    }

    /// <summary>
    /// Call this at the end of FixedUpdate to clear consumed presses.
    /// </summary>
    public void ConsumeBufferedPresses()
    {
        _bufferedKeyPresses.Clear();
        _leftMousePressBuffered = false;
        _rightMousePressBuffered = false;
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
    /// Returns true if the key was pressed (buffered, survives until consumed).
    /// </summary>
    public bool IsKeyPressed(Keys key)
    {
        return _bufferedKeyPresses.Contains(key);
    }

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
    /// Returns true if left mouse was clicked (buffered).
    /// </summary>
    public bool IsLeftMousePressed() => _leftMousePressBuffered;

    /// <summary>
    /// Returns true if right mouse was clicked (buffered).
    /// </summary>
    public bool IsRightMousePressed() => _rightMousePressBuffered;

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