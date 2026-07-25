using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Input
{
    /// <summary>
    /// Owns the current construction pointer screen position.
    ///
    /// Keyboard-and-mouse mode:
    ///     The position follows the real mouse.
    ///
    /// Gamepad mode:
    ///     The left stick moves a virtual cursor.
    ///
    /// This component does not select cells, place objects,
    /// or move the camera. It only exposes pointer intent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class ConstructionPointerController : MonoBehaviour
    {
        [Header("Action Names")]

        [SerializeField]
        private string constructionActionMapName =
            "Construction";

        [SerializeField]
        private string mousePositionActionName =
            "MousePosition";

        [SerializeField]
        private string pointerMoveActionName =
            "PointerMove";


        [Header("Virtual Cursor")]

        [Tooltip(
            "Maximum virtual-cursor movement speed in screen pixels " +
            "per second.")]
        [SerializeField, Min(1f)]
        private float virtualCursorSpeedPixelsPerSecond =
            1100f;

        [Tooltip(
            "Stick input below this amount is ignored.")]
        [SerializeField, Range(0f, 0.95f)]
        private float stickDeadZone =
            0.15f;

        [Tooltip(
            "Distance maintained between the virtual cursor and the " +
            "physical edge of the screen.")]
        [SerializeField, Min(0f)]
        private float screenPaddingPixels =
            10f;

        [Tooltip(
            "Hide the operating-system cursor while gamepad mode is active.")]
        [SerializeField]
        private bool hideSystemCursorWhileUsingGamepad =
            true;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logPointerModeChanges =
            true;


        /// <summary>
        /// Current pointer position in screen pixels.
        /// Both mouse and gamepad expose their position through this value.
        /// </summary>
        public Vector2 ScreenPosition { get; private set; }


        /// <summary>
        /// Outward left-stick pressure while the virtual cursor is
        /// pressed against a screen boundary.
        ///
        /// This does not move the camera by itself.
        /// The camera system will consume it during the next checkpoint.
        /// </summary>
        public Vector2 EdgePanIntent { get; private set; }


        /// <summary>
        /// True when PlayerInput currently has a gamepad paired
        /// through its active control scheme.
        /// </summary>
        public bool IsUsingGamepad { get; private set; }


        public event Action<bool> PointerModeChanged;


        private PlayerInput playerInput;

        private InputActionMap constructionActionMap;

        private InputAction mousePositionAction;

        private InputAction pointerMoveAction;

        private bool actionMapEnabledByThisComponent;

        private bool hasStarted;

        private bool isInitialized;


        private void Awake()
        {
            playerInput =
                GetComponent<PlayerInput>();

            ScreenPosition =
                GetScreenCenter();
        }


        private void Start()
        {
            hasStarted = true;

            Initialize();
        }


        private void OnEnable()
        {
            // OnEnable runs before Start the first time this object starts.
            // If the component is later disabled and re-enabled, restore it.
            if (hasStarted && !isInitialized)
            {
                Initialize();
            }
        }


        private void OnDisable()
        {
            EdgePanIntent =
                Vector2.zero;

            if (constructionActionMap != null
                && actionMapEnabledByThisComponent)
            {
                constructionActionMap.Disable();
            }

            actionMapEnabledByThisComponent = false;

            RestoreSystemCursor();

            isInitialized = false;
        }


        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            UpdatePointerMode();

            if (IsUsingGamepad)
            {
                UpdateGamepadPointer();
            }
            else
            {
                UpdateMousePointer();
            }

            ScreenPosition =
                ClampToScreen(ScreenPosition);
        }


        private void Initialize()
        {
            if (!TryResolveActions())
            {
                enabled = false;
                return;
            }

            actionMapEnabledByThisComponent =
                !constructionActionMap.enabled;

            constructionActionMap.Enable();

            ScreenPosition =
                ClampToScreen(ScreenPosition);

            isInitialized = true;

            UpdatePointerMode(
                forceNotification: true);
        }


        private bool TryResolveActions()
        {
            if (playerInput == null)
            {
                Debug.LogError(
                    $"{nameof(ConstructionPointerController)} requires " +
                    $"a {nameof(PlayerInput)} component.",
                    this);

                return false;
            }

            if (playerInput.actions == null)
            {
                Debug.LogError(
                    $"The {nameof(PlayerInput)} on '{name}' has no " +
                    "Input Action Asset assigned.",
                    this);

                return false;
            }

            constructionActionMap =
                playerInput.actions.FindActionMap(
                    constructionActionMapName,
                    throwIfNotFound: false);

            if (constructionActionMap == null)
            {
                Debug.LogError(
                    $"Could not find an Action Map named " +
                    $"'{constructionActionMapName}' in the PlayerInput " +
                    "action asset.",
                    this);

                return false;
            }

            mousePositionAction =
                constructionActionMap.FindAction(
                    mousePositionActionName,
                    throwIfNotFound: false);

            if (mousePositionAction == null)
            {
                Debug.LogError(
                    $"Could not find an Action named " +
                    $"'{mousePositionActionName}' in the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            pointerMoveAction =
                constructionActionMap.FindAction(
                    pointerMoveActionName,
                    throwIfNotFound: false);

            if (pointerMoveAction == null)
            {
                Debug.LogError(
                    $"Could not find an Action named " +
                    $"'{pointerMoveActionName}' in the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void UpdatePointerMode(
            bool forceNotification = false)
        {
            bool shouldUseGamepad =
                HasPairedGamepad();

            if (!forceNotification
                && shouldUseGamepad == IsUsingGamepad)
            {
                return;
            }

            IsUsingGamepad =
                shouldUseGamepad;

            EdgePanIntent =
                Vector2.zero;

            ApplySystemCursorVisibility();

            if (logPointerModeChanges)
            {
                string modeName =
                    IsUsingGamepad
                        ? "Gamepad virtual cursor"
                        : "Keyboard and mouse";

                Debug.Log(
                    $"Construction pointer switched to: {modeName}.",
                    this);
            }

            PointerModeChanged?.Invoke(
                IsUsingGamepad);
        }


        private bool HasPairedGamepad()
        {
            for (int index = 0;
                 index < playerInput.devices.Count;
                 index++)
            {
                if (playerInput.devices[index] is Gamepad)
                {
                    return true;
                }
            }

            return false;
        }


        private void UpdateMousePointer()
        {
            ScreenPosition =
                mousePositionAction.ReadValue<Vector2>();

            EdgePanIntent =
                Vector2.zero;
        }


        private void UpdateGamepadPointer()
        {
            Vector2 movementInput =
                pointerMoveAction.ReadValue<Vector2>();

            movementInput =
                ApplyDeadZone(
                    movementInput);

            Vector2 previousPosition =
                ScreenPosition;

            Vector2 desiredPosition =
                previousPosition
                + movementInput
                * virtualCursorSpeedPixelsPerSecond
                * Time.unscaledDeltaTime;

            ScreenPosition =
                ClampToScreen(
                    desiredPosition);

            EdgePanIntent =
                CalculateEdgePanIntent(
                    ScreenPosition,
                    movementInput);
        }


        private Vector2 ApplyDeadZone(
            Vector2 input)
        {
            float magnitude =
                input.magnitude;

            if (magnitude <= stickDeadZone)
            {
                return Vector2.zero;
            }

            float adjustedMagnitude =
                Mathf.InverseLerp(
                    stickDeadZone,
                    1f,
                    Mathf.Min(magnitude, 1f));

            return input.normalized
                * adjustedMagnitude;
        }


        private Vector2 CalculateEdgePanIntent(
            Vector2 clampedPosition,
            Vector2 movementInput)
        {
            GetScreenLimits(
                out float minimumX,
                out float maximumX,
                out float minimumY,
                out float maximumY);

            const float boundaryTolerance = 0.01f;

            float horizontalIntent = 0f;
            float verticalIntent = 0f;

            bool pressingLeft =
                clampedPosition.x
                    <= minimumX + boundaryTolerance
                && movementInput.x < 0f;

            bool pressingRight =
                clampedPosition.x
                    >= maximumX - boundaryTolerance
                && movementInput.x > 0f;

            bool pressingDown =
                clampedPosition.y
                    <= minimumY + boundaryTolerance
                && movementInput.y < 0f;

            bool pressingUp =
                clampedPosition.y
                    >= maximumY - boundaryTolerance
                && movementInput.y > 0f;

            if (pressingLeft || pressingRight)
            {
                horizontalIntent =
                    movementInput.x;
            }

            if (pressingDown || pressingUp)
            {
                verticalIntent =
                    movementInput.y;
            }

            return new Vector2(
                horizontalIntent,
                verticalIntent);
        }


        private Vector2 ClampToScreen(
            Vector2 screenPosition)
        {
            GetScreenLimits(
                out float minimumX,
                out float maximumX,
                out float minimumY,
                out float maximumY);

            return new Vector2(
                Mathf.Clamp(
                    screenPosition.x,
                    minimumX,
                    maximumX),

                Mathf.Clamp(
                    screenPosition.y,
                    minimumY,
                    maximumY));
        }


        private void GetScreenLimits(
            out float minimumX,
            out float maximumX,
            out float minimumY,
            out float maximumY)
        {
            float safeHorizontalPadding =
                Mathf.Min(
                    screenPaddingPixels,
                    Screen.width * 0.5f);

            float safeVerticalPadding =
                Mathf.Min(
                    screenPaddingPixels,
                    Screen.height * 0.5f);

            minimumX =
                safeHorizontalPadding;

            maximumX =
                Mathf.Max(
                    minimumX,
                    Screen.width - safeHorizontalPadding);

            minimumY =
                safeVerticalPadding;

            maximumY =
                Mathf.Max(
                    minimumY,
                    Screen.height - safeVerticalPadding);
        }


        private static Vector2 GetScreenCenter()
        {
            return new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f);
        }


        private void ApplySystemCursorVisibility()
        {
            if (!hideSystemCursorWhileUsingGamepad)
            {
                return;
            }

            Cursor.visible =
                !IsUsingGamepad;
        }


        private void RestoreSystemCursor()
        {
            if (hideSystemCursorWhileUsingGamepad)
            {
                Cursor.visible = true;
            }
        }


        private void OnValidate()
        {
            virtualCursorSpeedPixelsPerSecond =
                Mathf.Max(
                    virtualCursorSpeedPixelsPerSecond,
                    1f);

            screenPaddingPixels =
                Mathf.Max(
                    screenPaddingPixels,
                    0f);
        }
    }
}