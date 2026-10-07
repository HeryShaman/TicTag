using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    public const int KeyboardPlayerCount = 2;
    public const int MaxGamepads = 3;
    public const int PlayersPerGamepad = 2;

    // 2 joueurs clavier + 3 manettes * 2 joueurs
    public const int PlayerCount = KeyboardPlayerCount + MaxGamepads * PlayersPerGamepad;

    [Header("Deadzone manettes")]
    [Range(0f, 0.5f)]
    public float stickDeadzone = 0.15f;

    private Gamepad[] _gamepads = new Gamepad[MaxGamepads];

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        RefreshGamepads();
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            RefreshGamepads();
        }
    }

    private void RefreshGamepads()
    {
        for (int i = 0; i < MaxGamepads; i++)
        {
            _gamepads[i] = null;
        }

        var allGamepads = Gamepad.all;
        int count = Mathf.Min(MaxGamepads, allGamepads.Count);

        for (int i = 0; i < count; i++)
        {
            _gamepads[i] = allGamepads[i];
        }
    }

    private bool IsValidPlayerIndex(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < PlayerCount;
    }

    private Gamepad GetGamepadForPlayer(int playerIndex)
    {
        if (playerIndex < KeyboardPlayerCount)
        {
            return null;
        }

        int gamepadIndex = (playerIndex - KeyboardPlayerCount) / PlayersPerGamepad;

        if (gamepadIndex < 0 || gamepadIndex >= _gamepads.Length)
        {
            return null;
        }

        return _gamepads[gamepadIndex];
    }

    // ---------------------------------------------------------------
    // MOVE
    // ---------------------------------------------------------------

    public Vector2 GetMove(int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
        {
            return Vector2.zero;
        }

        // Clavier joueur 0
        if (playerIndex == 0)
        {
            return ApplyDeadzone(ReadKeyboardPlayer0());
        }

        // Clavier joueur 1
        if (playerIndex == 1)
        {
            return ApplyDeadzone(ReadKeyboardPlayer1());
        }

        // Manettes
        int gamepadIndex = (playerIndex - KeyboardPlayerCount) / PlayersPerGamepad;
        int stickIndex = (playerIndex - KeyboardPlayerCount) % PlayersPerGamepad;

        if (gamepadIndex < 0 || gamepadIndex >= _gamepads.Length)
        {
            return Vector2.zero;
        }

        Gamepad gamepad = _gamepads[gamepadIndex];

        if (gamepad == null)
        {
            return Vector2.zero;
        }

        Vector2 stickValue = stickIndex == 0
            ? gamepad.leftStick.ReadValue()
            : gamepad.rightStick.ReadValue();

        return ApplyDeadzone(stickValue);
    }

    private Vector2 ReadKeyboardPlayer0()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;

        // ZQSD + WASD
        if (keyboard[Key.Z].isPressed || keyboard[Key.W].isPressed)
        {
            move.y += 1f;
        }

        if (keyboard[Key.S].isPressed)
        {
            move.y -= 1f;
        }

        if (keyboard[Key.Q].isPressed || keyboard[Key.A].isPressed)
        {
            move.x -= 1f;
        }

        if (keyboard[Key.D].isPressed)
        {
            move.x += 1f;
        }

        return move.normalized;
    }

    private Vector2 ReadKeyboardPlayer1()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;

        if (keyboard[Key.UpArrow].isPressed)
        {
            move.y += 1f;
        }

        if (keyboard[Key.DownArrow].isPressed)
        {
            move.y -= 1f;
        }

        if (keyboard[Key.LeftArrow].isPressed)
        {
            move.x -= 1f;
        }

        if (keyboard[Key.RightArrow].isPressed)
        {
            move.x += 1f;
        }

        return move.normalized;
    }

    private Vector2 ApplyDeadzone(Vector2 value)
    {
        if (value.sqrMagnitude < stickDeadzone * stickDeadzone)
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(value, 1f);
    }

    // ---------------------------------------------------------------
    // MENU / INTERACTION
    // ---------------------------------------------------------------

    // Bouton pour confirmer / interagir
    public bool GetConfirmPressed(int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
        {
            return false;
        }

        Keyboard keyboard = Keyboard.current;

        // Joueur clavier 0
        if (playerIndex == 0)
        {
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[Key.E].wasPressedThisFrame || keyboard[Key.Space].wasPressedThisFrame;
        }

        // Joueur clavier 1
        if (playerIndex == 1)
        {
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[Key.Enter].wasPressedThisFrame || keyboard[Key.NumpadEnter].wasPressedThisFrame;
        }

        // Manettes
        Gamepad gamepad = GetGamepadForPlayer(playerIndex);

        if (gamepad == null)
        {
            return false;
        }

        return gamepad.buttonSouth.wasPressedThisFrame;
    }

    // Bouton pour revenir en arrière
    public bool GetBackPressed(int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
        {
            return false;
        }

        Keyboard keyboard = Keyboard.current;

        // Joueur clavier 0
        if (playerIndex == 0)
        {
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[Key.Escape].wasPressedThisFrame;
        }

        // Joueur clavier 1
        if (playerIndex == 1)
        {
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[Key.Backspace].wasPressedThisFrame || keyboard[Key.RightShift].wasPressedThisFrame;
        }

        // Manettes
        Gamepad gamepad = GetGamepadForPlayer(playerIndex);

        if (gamepad == null)
        {
            return false;
        }

        return gamepad.buttonEast.wasPressedThisFrame;
    }

    // Pratique pour un menu global : n'importe quel joueur peut confirmer
    public bool GetAnyConfirmPressed()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            if (GetConfirmPressed(i))
            {
                return true;
            }
        }

        return false;
    }

    // Pratique pour un menu global : n'importe quel joueur peut revenir en arrière
    public bool GetAnyBackPressed()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            if (GetBackPressed(i))
            {
                return true;
            }
        }

        return false;
    }
}