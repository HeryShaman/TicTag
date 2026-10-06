using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference move;

    [Header("Player")]
    [Tooltip("0-1 = clavier (ZQSD / flèches), 2-3 = manette 0, 4-5 = manette 1, 6-7 = manette 2 …")]
    [SerializeField, Range(0, 7)] private int playerIndex;

    public Vector2 MoveDirection => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public int CurrentDevice { get; private set; }

    private InputAction moveAction;
    private int lastAppliedIndex = -1;

    private void OnEnable()
    {
        ApplyPlayerIndex();
    }

    private void OnDisable()
    {
        DisposeActions();
    }

    private void OnDestroy()
    {
        DisposeActions();
    }

    private void Update()
    {
        // Détection d'un changement d'index en Play mode
        if (playerIndex != lastAppliedIndex)
            ApplyPlayerIndex();
    }

    /// <summary>Réassigne l'action au device/groupe correspondant à l'index courant.</summary>
    public void ApplyPlayerIndex()
    {
        DisposeActions();

        (InputDevice device, string group) = ResolveSlot(playerIndex);

        if (device == null)
        {
            Debug.LogWarning($"[InputReader] Aucun device trouvé pour l'index {playerIndex} sur {name}.", this);
            lastAppliedIndex = playerIndex;
            return;
        }

        CurrentDevice = device is Gamepad ? 1 : 0;

        moveAction = move.action.Clone();
        moveAction.bindingMask = InputBinding.MaskByGroup(group);
        moveAction.Enable();

        lastAppliedIndex = playerIndex;
    }

    private void DisposeActions()
    {
        if (moveAction == null) return;
        moveAction.Disable();
        moveAction.Dispose();
        moveAction = null;
    }

    private static (InputDevice, string) ResolveSlot(int index)
    {
        if (index == 0) return (Keyboard.current, "KbLeft");
        if (index == 1) return (Keyboard.current, "KbRight");

        int gamepadIndex = (index - 2) / 2;
        bool isLeft = (index - 2) % 2 == 0;

        if (Gamepad.all.Count <= gamepadIndex)
            return (null, null);

        return (Gamepad.all[gamepadIndex], isLeft ? "GpLeft" : "GpRight");
    }
}