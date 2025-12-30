using System;
using System.Text;
using System.Windows.Input;

namespace FreeMyRam.Developer;

/// <summary>
/// Manages the hidden developer mode activation via secret keyboard code.
/// When the user types the secret code "10720040" anywhere in the app,
/// the developer features will be unlocked.
/// </summary>
public class DevModeManager
{
    private const string SECRET_CODE = "10720040";
    private readonly StringBuilder _inputBuffer = new();
    private readonly int _maxBufferLength;
    
    /// <summary>
    /// Gets whether developer mode is currently active.
    /// </summary>
    public bool IsDevModeActive { get; private set; }
    
    /// <summary>
    /// Event fired when developer mode is activated.
    /// </summary>
    public event Action? DevModeActivated;
    
    /// <summary>
    /// Event fired when developer mode is deactivated.
    /// </summary>
    public event Action? DevModeDeactivated;
    
    public DevModeManager()
    {
        _maxBufferLength = SECRET_CODE.Length;
    }
    
    /// <summary>
    /// Process a key press event. Call this from the Window's KeyDown event.
    /// </summary>
    /// <param name="e">The keyboard event args</param>
    /// <returns>True if the secret code was just entered</returns>
    public bool ProcessKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        // Only accept number keys
        char? inputChar = GetNumberFromKey(e.Key);
        
        if (inputChar.HasValue)
        {
            _inputBuffer.Append(inputChar.Value);
            
            // Keep buffer at max length
            if (_inputBuffer.Length > _maxBufferLength)
            {
                _inputBuffer.Remove(0, _inputBuffer.Length - _maxBufferLength);
            }
            
            // Check if secret code is entered
            if (_inputBuffer.ToString() == SECRET_CODE)
            {
                _inputBuffer.Clear();
                
                if (!IsDevModeActive)
                {
                    IsDevModeActive = true;
                    DevModeActivated?.Invoke();
                    return true;
                }
            }
        }
        else if (e.Key != Key.LeftShift && e.Key != Key.RightShift && 
                 e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
                 e.Key != Key.LeftAlt && e.Key != Key.RightAlt &&
                 e.Key != Key.CapsLock && e.Key != Key.Tab)
        {
            // Non-modifier, non-number key pressed - clear buffer
            _inputBuffer.Clear();
        }
        
        return false;
    }
    
    /// <summary>
    /// Toggle developer mode off.
    /// </summary>
    public void DeactivateDevMode()
    {
        if (IsDevModeActive)
        {
            IsDevModeActive = false;
            _inputBuffer.Clear();
            DevModeDeactivated?.Invoke();
        }
    }
    
    /// <summary>
    /// Convert a Key to its number character (0-9).
    /// </summary>
    private static char? GetNumberFromKey(Key key)
    {
        // Handle number row keys (D0-D9)
        if (key >= Key.D0 && key <= Key.D9)
        {
            return (char)('0' + (key - Key.D0));
        }
        
        // Handle numpad keys (NumPad0-NumPad9)
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            return (char)('0' + (key - Key.NumPad0));
        }
        
        return null;
    }
    
    /// <summary>
    /// Clear the input buffer
    /// </summary>
    public void ClearBuffer()
    {
        _inputBuffer.Clear();
    }
}
