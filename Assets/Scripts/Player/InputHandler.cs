using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates raw Unity Input System callbacks from WIZARD_InputActions
/// into semantic game events. Does not execute game logic — fires C# events
/// that PlayerController, TargetingSystem, and UIManager consume.
/// Gates inputs by GameState.
///
/// File: Assets/Scripts/Player/InputHandler.cs
/// Layer: 2 (Depends on GameManager for state gating)
/// </summary>
public class InputHandler : MonoBehaviour, WIZARD_InputActions.IGameplayActions
{
    // ═════════════════════════════════════════════════════════════════════
    //  Events
    // ═════════════════════════════════════════════════════════════════════

    public static event Action<Vector2Int> OnMoveInput;
    public static event Action OnWaitInput;
    public static event Action OnInteractInput;
    public static event Action<int> OnCastSpellInput;
    public static event Action<Vector2Int> OnConfirmTargetInput;
    public static event Action OnCancelInput;
    public static event Action OnExamineInput;
    public static event Action OnOpenInventoryInput;
    public static event Action OnOpenSpellbookInput;
    public static event Action OnOpenCharacterSheetInput;
    public static event Action OnOpenMapInput;
    public static event Action OnOpenLogInput;
    public static event Action OnPauseInput;
    public static event Action OnQuickSaveInput;

    // ═════════════════════════════════════════════════════════════════════
    //  Input Actions Reference
    // ═════════════════════════════════════════════════════════════════════

    private WIZARD_InputActions inputActions;

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new WIZARD_InputActions();
            inputActions.Gameplay.AddCallbacks(this);
        }
        inputActions.Gameplay.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Helper — State Checks
    // ═════════════════════════════════════════════════════════════════════

    private bool IsGameplay =>
        GameManager.Instance != null &&
        GameManager.Instance.CurrentState == GameState.Gameplay;

    private bool IsTargeting =>
        GameManager.Instance != null &&
        GameManager.Instance.CurrentState == GameState.SpellTargeting;

    private bool IsGameplayOrPaused =>
        GameManager.Instance != null &&
        (GameManager.Instance.CurrentState == GameState.Gameplay ||
         GameManager.Instance.CurrentState == GameState.Paused);

    // ═════════════════════════════════════════════════════════════════════
    //  IGameplayActions Implementation
    // ═════════════════════════════════════════════════════════════════════

    public void OnMoveNorth(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnMoveInput?.Invoke(Vector2Int.up);
    }

    public void OnMoveSouth(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnMoveInput?.Invoke(Vector2Int.down);
    }

    public void OnMoveWest(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnMoveInput?.Invoke(Vector2Int.left);
    }

    public void OnMoveEast(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnMoveInput?.Invoke(Vector2Int.right);
    }

    public void OnWait(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnWaitInput?.Invoke();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnInteractInput?.Invoke();
    }

    public void OnCastSpell1(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnCastSpellInput?.Invoke(0);
    }

    public void OnCastSpell2(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnCastSpellInput?.Invoke(1);
    }

    public void OnCastSpell3(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnCastSpellInput?.Invoke(2);
    }

    public void OnCastSpell4(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnCastSpellInput?.Invoke(3);
    }

    public void OnCastSpell5(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnCastSpellInput?.Invoke(4);
    }

    public void OnConfirmTarget(InputAction.CallbackContext context)
    {
        if (context.performed && IsTargeting)
        {
            // TODO: Convert mouse position to grid tile via GridManager.WorldToGrid()
            // For now, fire with zero — the TargetingSystem will resolve the tile
            Vector2 mousePos = inputActions.Gameplay.MousePosition.ReadValue<Vector2>();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);
            OnConfirmTargetInput?.Invoke(gridPos);
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (IsTargeting)
                OnCancelInput?.Invoke();
            else if (IsGameplay)
                OnPauseInput?.Invoke();
        }
    }

    public void OnExamine(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnExamineInput?.Invoke();
    }

    public void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplayOrPaused)
            OnOpenInventoryInput?.Invoke();
    }

    public void OnOpenSpellbook(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplayOrPaused)
            OnOpenSpellbookInput?.Invoke();
    }

    public void OnOpenCharacterSheet(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplayOrPaused)
            OnOpenCharacterSheetInput?.Invoke();
    }

    public void OnOpenMap(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplayOrPaused)
            OnOpenMapInput?.Invoke();
    }

    public void OnOpenLog(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplayOrPaused)
            OnOpenLogInput?.Invoke();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplayOrPaused)
            OnPauseInput?.Invoke();
    }

    public void OnQuickSaveAndQuit(InputAction.CallbackContext context)
    {
        if (context.performed && IsGameplay)
            OnQuickSaveInput?.Invoke();
    }

    public void OnMousePosition(InputAction.CallbackContext context)
    {
        // Mouse position is read directly when needed (OnConfirmTarget).
        // No event fired — this callback satisfies the interface requirement.
    }
}