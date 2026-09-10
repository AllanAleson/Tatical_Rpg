using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class TacticalCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public TurnManager turnManager;

    [Header("Pan")]
    public float dragPanSensitivity = 0.6f;
    [FormerlySerializedAs("movementSmoothTime")]
    public float panSmoothTime = 0.08f;

    [Header("Rotation")]
    public float rotationSpeed = 45f;

    [Header("Zoom")]
    public float zoomDistance = 14f;
    public float minZoomDistance = 6f;
    public float maxZoomDistance = 22f;
    public float zoomSpeed = 4f;
    public float zoomSmoothTime = 0.08f;

    [Header("Views")]
    public float isometricPitch = 45f;
    public float isometricYaw = 0f;
    public float topDownPitch = 90f;
    public float viewTransitionSpeed = 8f;

    [Header("Turn Focus")]
    public bool turnFocusEnabled = true;
    public float turnFocusZoomAmount = 1.5f;
    public float turnFocusMoveDuration = 0.6f;
    public float turnFocusHoldDuration = 0.4f;
    public float turnFocusReturnDuration = 0.5f;

    [Header("Enemy Follow")]
    public float enemyFollowSmoothTime = 0.18f;

    private enum TurnFocusPhase
    {
        None,
        MoveIn,
        Hold,
        ReturnZoom
    }

    private Vector3 targetPivotPosition;
    private Vector3 pivotVelocity;
    private float targetZoomDistance;
    private float currentZoomDistance;
    private float zoomVelocity;
    private Quaternion targetRotation;
    private Vector3 currentPivotPosition;
    private float currentYaw;
    private Vector3 lastMousePosition;
    private bool isTopDown = false;
    private bool isSubscribedToTurnManager = false;
    private TurnFocusPhase turnFocusPhase = TurnFocusPhase.None;
    private float turnFocusTimer = 0f;
    private Vector3 turnFocusStartPivotPosition;
    private Vector3 turnFocusTargetPivotPosition;
    private float turnFocusStartZoomDistance;
    private float turnFocusZoomDistance;
    private float turnFocusOriginalZoomDistance;
    private UnitStats turnFocusUnit;
    private UnitStats enemyFollowTarget;
    private bool isEnemyFollowingThisFrame = false;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        targetZoomDistance = Mathf.Clamp(zoomDistance, minZoomDistance, maxZoomDistance);
        currentZoomDistance = targetZoomDistance;

        targetPivotPosition = GetInitialPivotPosition();
        currentPivotPosition = targetPivotPosition;
        transform.position = currentPivotPosition;
        currentYaw = isometricYaw;
        targetRotation = GetViewRotation();

        ApplyCameraTransform(true);
    }

    void OnEnable()
    {
        SubscribeToTurnManager();
    }

    void Start()
    {
        if (!isSubscribedToTurnManager)
            SubscribeToTurnManager();
    }

    void OnDisable()
    {
        UnsubscribeFromTurnManager();
        CancelTurnFocus();
    }

    void Update()
    {
        bool hasManualPanOrZoomInput = HasManualPanOrZoomInput();

        HandleRotationInput();
        HandleViewToggleInput();

        if (turnFocusPhase == TurnFocusPhase.ReturnZoom && hasManualPanOrZoomInput)
            CancelTurnFocus();

        if (turnFocusPhase == TurnFocusPhase.None)
        {
            HandlePanInput();
            HandleZoomInput();
        }
        else
        {
            SyncBlockedPanInputState();
        }

        UpdateTurnFocus();
        UpdateEnemyFollow();
        ApplyCameraTransform(false);
    }

    void OnValidate()
    {
        dragPanSensitivity = Mathf.Max(0f, dragPanSensitivity);
        panSmoothTime = Mathf.Max(0f, panSmoothTime);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        zoomSpeed = Mathf.Max(0f, zoomSpeed);
        zoomSmoothTime = Mathf.Max(0f, zoomSmoothTime);
        viewTransitionSpeed = Mathf.Max(0.01f, viewTransitionSpeed);
        turnFocusZoomAmount = Mathf.Max(0f, turnFocusZoomAmount);
        turnFocusMoveDuration = Mathf.Max(0.01f, turnFocusMoveDuration);
        turnFocusHoldDuration = Mathf.Max(0f, turnFocusHoldDuration);
        turnFocusReturnDuration = Mathf.Max(0.01f, turnFocusReturnDuration);
        enemyFollowSmoothTime = Mathf.Max(0f, enemyFollowSmoothTime);

        minZoomDistance = Mathf.Max(0.01f, minZoomDistance);
        maxZoomDistance = Mathf.Max(minZoomDistance, maxZoomDistance);
        zoomDistance = Mathf.Clamp(zoomDistance, minZoomDistance, maxZoomDistance);
        topDownPitch = Mathf.Clamp(topDownPitch, 0f, 90f);
    }

    private void HandleRotationInput()
    {
        float rotationInput = 0f;

        if (Input.GetKey(KeyCode.Q))
            rotationInput -= 1f;

        if (Input.GetKey(KeyCode.E))
            rotationInput += 1f;

        if (Mathf.Approximately(rotationInput, 0f))
            return;

        currentYaw += rotationInput * rotationSpeed * Time.deltaTime;
        targetRotation = GetViewRotation();
    }

    private bool HasManualPanOrZoomInput()
    {
        return Input.GetMouseButtonDown(2) ||
            Input.GetMouseButton(2) ||
            !Mathf.Approximately(Input.mouseScrollDelta.y, 0f);
    }

    private void SyncBlockedPanInputState()
    {
        if (Input.GetMouseButton(2))
            lastMousePosition = Input.mousePosition;
    }

    private void HandlePanInput()
    {
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(2))
            return;

        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;

        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;
        Vector3 movement = (-right * mouseDelta.x) + (-forward * mouseDelta.y);

        targetPivotPosition += movement * dragPanSensitivity * Time.deltaTime;
    }

    private void HandleZoomInput()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Approximately(scroll, 0f))
            return;

        targetZoomDistance = Mathf.Clamp(
            targetZoomDistance - scroll * zoomSpeed,
            minZoomDistance,
            maxZoomDistance
        );
    }

    private void HandleViewToggleInput()
    {
        if (!Input.GetKeyDown(KeyCode.T))
            return;

        isTopDown = !isTopDown;
        targetRotation = GetViewRotation();
    }

    private void SubscribeToTurnManager()
    {
        if (isSubscribedToTurnManager)
            return;

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (turnManager == null)
            return;

        turnManager.OnTurnStarted += HandleTurnStarted;
        isSubscribedToTurnManager = true;
    }

    private void UnsubscribeFromTurnManager()
    {
        if (!isSubscribedToTurnManager || turnManager == null)
            return;

        turnManager.OnTurnStarted -= HandleTurnStarted;
        isSubscribedToTurnManager = false;
    }

    private void HandleTurnStarted(UnitStats activeUnit)
    {
        enemyFollowTarget = IsLivingEnemy(activeUnit) ? activeUnit : null;

        if (!turnFocusEnabled || activeUnit == null || activeUnit.isDowned)
            return;

        BeginTurnFocus(activeUnit);
    }

    private bool IsLivingEnemy(UnitStats unit)
    {
        return unit != null &&
            unit.team == UnitStats.Team.Enemy &&
            !unit.isDowned;
    }

    public IEnumerator WaitForCurrentTurnFocusInitialDelay(UnitStats unit)
    {
        if (unit == null || !ShouldDelayForTurnFocus(unit))
            yield break;

        while (ShouldDelayForTurnFocus(unit))
            yield return null;
    }

    private bool ShouldDelayForTurnFocus(UnitStats unit)
    {
        return turnFocusEnabled &&
            targetCamera != null &&
            unit != null &&
            turnFocusUnit == unit &&
            (turnFocusPhase == TurnFocusPhase.MoveIn ||
                turnFocusPhase == TurnFocusPhase.Hold);
    }

    private void BeginTurnFocus(UnitStats unit)
    {
        EnsureZoomStateIsInitialized();

        Vector3 unitPosition = unit.transform.position;
        bool wasTurnFocusActive = turnFocusPhase != TurnFocusPhase.None;
        turnFocusPhase = TurnFocusPhase.MoveIn;
        turnFocusTimer = 0f;
        turnFocusUnit = unit;

        turnFocusStartPivotPosition = currentPivotPosition;
        turnFocusTargetPivotPosition = new Vector3(
            unitPosition.x,
            currentPivotPosition.y,
            unitPosition.z
        );

        float restoredZoomDistance = wasTurnFocusActive && turnFocusOriginalZoomDistance > 0f
            ? turnFocusOriginalZoomDistance
            : targetZoomDistance;

        turnFocusStartZoomDistance = currentZoomDistance;
        turnFocusOriginalZoomDistance = restoredZoomDistance;
        turnFocusZoomDistance = Mathf.Clamp(
            turnFocusOriginalZoomDistance - turnFocusZoomAmount,
            minZoomDistance,
            maxZoomDistance
        );
        targetPivotPosition = turnFocusStartPivotPosition;
        targetZoomDistance = turnFocusStartZoomDistance;

        pivotVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    private void EnsureZoomStateIsInitialized()
    {
        if (targetZoomDistance > 0f && currentZoomDistance > 0f)
            return;

        targetZoomDistance = Mathf.Clamp(zoomDistance, minZoomDistance, maxZoomDistance);
        currentZoomDistance = targetZoomDistance;
    }

    private void CancelTurnFocus()
    {
        if (turnFocusPhase == TurnFocusPhase.None)
            return;

        turnFocusPhase = TurnFocusPhase.None;
        turnFocusTimer = 0f;
        targetPivotPosition = currentPivotPosition;
        targetZoomDistance = currentZoomDistance;
        pivotVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    private void UpdateTurnFocus()
    {
        if (turnFocusPhase == TurnFocusPhase.None)
            return;

        turnFocusTimer += Time.deltaTime;

        switch (turnFocusPhase)
        {
            case TurnFocusPhase.MoveIn:
                UpdateTurnFocusMoveIn();
                break;
            case TurnFocusPhase.Hold:
                UpdateTurnFocusHold();
                break;
            case TurnFocusPhase.ReturnZoom:
                UpdateTurnFocusReturnZoom();
                break;
        }
    }

    private void UpdateEnemyFollow()
    {
        isEnemyFollowingThisFrame = false;

        if (!ShouldFollowEnemy())
        {
            if (enemyFollowTarget != null &&
                (turnManager == null || !turnManager.IsCurrentUnit(enemyFollowTarget)))
            {
                enemyFollowTarget = null;
            }

            return;
        }

        Vector3 enemyPosition = enemyFollowTarget.transform.position;
        targetPivotPosition = new Vector3(
            enemyPosition.x,
            currentPivotPosition.y,
            enemyPosition.z
        );
        isEnemyFollowingThisFrame = true;
    }

    private bool ShouldFollowEnemy()
    {
        if (!IsLivingEnemy(enemyFollowTarget))
            return false;

        if (turnManager == null || !turnManager.IsCurrentUnit(enemyFollowTarget))
            return false;

        return turnFocusPhase != TurnFocusPhase.MoveIn &&
            turnFocusPhase != TurnFocusPhase.Hold;
    }

    private void UpdateTurnFocusMoveIn()
    {
        float progress = Mathf.Clamp01(turnFocusTimer / turnFocusMoveDuration);
        float easedProgress = SmoothStep(progress);

        targetPivotPosition = Vector3.Lerp(
            turnFocusStartPivotPosition,
            turnFocusTargetPivotPosition,
            easedProgress
        );
        targetZoomDistance = Mathf.Lerp(
            turnFocusStartZoomDistance,
            turnFocusZoomDistance,
            easedProgress
        );

        if (progress < 1f)
            return;

        turnFocusPhase = TurnFocusPhase.Hold;
        turnFocusTimer = 0f;
        targetPivotPosition = turnFocusTargetPivotPosition;
        targetZoomDistance = turnFocusZoomDistance;
    }

    private void UpdateTurnFocusHold()
    {
        targetPivotPosition = turnFocusTargetPivotPosition;
        targetZoomDistance = turnFocusZoomDistance;

        if (turnFocusTimer < turnFocusHoldDuration)
            return;

        turnFocusPhase = TurnFocusPhase.ReturnZoom;
        turnFocusTimer = 0f;
        turnFocusStartZoomDistance = currentZoomDistance;
    }

    private void UpdateTurnFocusReturnZoom()
    {
        float progress = Mathf.Clamp01(turnFocusTimer / turnFocusReturnDuration);
        float easedProgress = SmoothStep(progress);

        targetPivotPosition = turnFocusTargetPivotPosition;
        targetZoomDistance = Mathf.Lerp(
            turnFocusStartZoomDistance,
            turnFocusOriginalZoomDistance,
            easedProgress
        );

        if (progress < 1f)
            return;

        turnFocusPhase = TurnFocusPhase.None;
        turnFocusTimer = 0f;
        turnFocusUnit = null;
        targetZoomDistance = turnFocusOriginalZoomDistance;
    }

    private float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private void ApplyCameraTransform(bool snap)
    {
        if (targetCamera == null)
            return;

        float pivotSmoothTime = isEnemyFollowingThisFrame
            ? enemyFollowSmoothTime
            : panSmoothTime;

        Vector3 pivotPosition = snap || pivotSmoothTime <= 0f
            ? targetPivotPosition
            : Vector3.SmoothDamp(
                currentPivotPosition,
                targetPivotPosition,
                ref pivotVelocity,
                pivotSmoothTime
            );

        currentPivotPosition = pivotPosition;

        if (targetCamera.transform != transform)
            transform.position = pivotPosition;

        currentZoomDistance = snap || zoomSmoothTime <= 0f
            ? targetZoomDistance
            : Mathf.SmoothDamp(
                currentZoomDistance,
                targetZoomDistance,
                ref zoomVelocity,
                zoomSmoothTime
            );

        Quaternion currentRotation = snap
            ? targetRotation
            : Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-viewTransitionSpeed * Time.deltaTime)
            );

        targetCamera.transform.rotation = currentRotation;
        targetCamera.transform.position =
            pivotPosition + currentRotation * Vector3.back * currentZoomDistance;
    }

    private Vector3 GetInitialPivotPosition()
    {
        if (targetCamera == null)
            return transform.position;

        Vector3 forward = targetCamera.transform.forward;

        if (Mathf.Abs(forward.y) < 0.001f)
            return transform.position;

        float distanceToGround = -targetCamera.transform.position.y / forward.y;
        Vector3 groundPoint = targetCamera.transform.position + forward * distanceToGround;

        return new Vector3(groundPoint.x, transform.position.y, groundPoint.z);
    }

    private Quaternion GetViewRotation()
    {
        float pitch = isTopDown ? topDownPitch : isometricPitch;
        return Quaternion.Euler(pitch, currentYaw, 0f);
    }
}
