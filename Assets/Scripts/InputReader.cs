using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    public const int KeyboardPlayerCount = 2;
    public const int MaxGamepads = 3;
    public const int PlayersPerGamepad = 2;

    // 2 joueurs clavier + 3 manettes * 2 joueurs
    public const int PlayerCount = KeyboardPlayerCount + MaxGamepads * PlayersPerGamepad;

    [Header("Player")]
    [SerializeField, Range(0, PlayerCount - 1)] private int playerIndex = 0;

    [Header("Deadzone manettes")]
    [Range(0f, 0.5f)]
    public float stickDeadzone = 0.15f;

    public int PlayerIndex => playerIndex;
    public bool HasValidIndex => IsValidPlayerIndex(playerIndex);

    /// <summary>À appeler par le GameManager / spawner pour assigner l'index à l'instanciation.</summary>
    public void SetPlayerIndex(int index)
    {
        playerIndex = Mathf.Clamp(index, 0, PlayerCount - 1);
    }

    // ---------------------------------------------------------------
    // API d'instance (le joueur lit SES contrôles)
    // ---------------------------------------------------------------

    public Vector2 GetMove() => ReadMove(playerIndex, stickDeadzone);
    public bool GetConfirmPressed() => ReadConfirm(playerIndex);
    public bool GetBackPressed() => ReadBack(playerIndex);

    // ---------------------------------------------------------------
    // API statique (menus, écran de sélection : on interroge un index sans instance)
    // ---------------------------------------------------------------

    public static bool IsValidPlayerIndex(int index)
    {
        return index >= 0 && index < PlayerCount;
    }

    public static bool GetAnyConfirmPressed()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            if (ReadConfirm(i)) return true;
        }
        return false;
    }

    public static bool GetAnyBackPressed()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            if (ReadBack(i)) return true;
        }
        return false;
    }

    public static Vector2 ReadMove(int index, float deadzone = 0.15f)
    {
        if (!IsValidPlayerIndex(index))
        {
            return Vector2.zero;
        }

        if (index == 0) return ApplyDeadzone(ReadKeyboardPlayer0(), deadzone);
        if (index == 1) return ApplyDeadzone(ReadKeyboardPlayer1(), deadzone);

        Gamepad gamepad = GetGamepadForPlayer(index);
        if (gamepad == null)
        {
            return Vector2.zero;
        }

        int stickIndex = (index - KeyboardPlayerCount) % PlayersPerGamepad;

        Vector2 stickValue = stickIndex == 0
            ? gamepad.leftStick.ReadValue()
            : gamepad.rightStick.ReadValue();

        return ApplyDeadzone(stickValue, deadzone);
    }

    public static bool ReadConfirm(int index)
    {
        if (!IsValidPlayerIndex(index)) return false;

        if (index < KeyboardPlayerCount)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return index == 0
                ? keyboard[Key.E].wasPressedThisFrame || keyboard[Key.Space].wasPressedThisFrame
                : keyboard[Key.Enter].wasPressedThisFrame || keyboard[Key.NumpadEnter].wasPressedThisFrame;
        }

        Gamepad gamepad = GetGamepadForPlayer(index);
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    public static bool ReadBack(int index)
    {
        if (!IsValidPlayerIndex(index)) return false;

        if (index < KeyboardPlayerCount)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return index == 0
                ? keyboard[Key.Escape].wasPressedThisFrame
                : keyboard[Key.Backspace].wasPressedThisFrame || keyboard[Key.RightShift].wasPressedThisFrame;
        }

        Gamepad gamepad = GetGamepadForPlayer(index);
        return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
    }

    // ---------------------------------------------------------------
    // Interne
    // ---------------------------------------------------------------

    // Gamepad.all est maintenu par l'Input System : plus besoin de cache ni d'abonnement
    // à onDeviceChange (qui aurait été dupliqué sur chacune des 8 instances).
    private static Gamepad GetGamepadForPlayer(int index)
    {
        if (index < KeyboardPlayerCount) return null;

        int gamepadIndex = (index - KeyboardPlayerCount) / PlayersPerGamepad;
        if (gamepadIndex < 0 || gamepadIndex >= MaxGamepads) return null;

        var all = Gamepad.all;
        return gamepadIndex < all.Count ? all[gamepadIndex] : null;
    }

    private static Vector2 ReadKeyboardPlayer0()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        Vector2 move = Vector2.zero;

        // ZQSD + WASD
        if (keyboard[Key.Z].isPressed || keyboard[Key.W].isPressed) move.y += 1f;
        if (keyboard[Key.S].isPressed) move.y -= 1f;
        if (keyboard[Key.Q].isPressed || keyboard[Key.A].isPressed) move.x -= 1f;
        if (keyboard[Key.D].isPressed) move.x += 1f;

        return move.normalized;
    }

    private static Vector2 ReadKeyboardPlayer1()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        Vector2 move = Vector2.zero;

        if (keyboard[Key.UpArrow].isPressed) move.y += 1f;
        if (keyboard[Key.DownArrow].isPressed) move.y -= 1f;
        if (keyboard[Key.LeftArrow].isPressed) move.x -= 1f;
        if (keyboard[Key.RightArrow].isPressed) move.x += 1f;

        return move.normalized;
    }

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone)
    {
        if (value.sqrMagnitude < deadzone * deadzone)
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(value, 1f);
    }
}