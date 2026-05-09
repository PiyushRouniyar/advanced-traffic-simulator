// ============================================================
//  TrafficLightController.cs  –  Unity 6  |  Traffic Simulator
//  Attach this script to a TrafficSystem GameObject.
//  Controls North/South and East/West traffic light groups
//  using simple SetActive() switching. No animations, no shaders.
// ============================================================

using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  INSPECTOR VARIABLES
    //  Drag your light group GameObjects here in the Inspector.
    // ──────────────────────────────────────────────────────────

    [Header("North / South Light Groups")]
    [Tooltip("The GameObject that holds all NS green light meshes.")]
    public GameObject nsGreenLights;

    [Tooltip("The GameObject that holds all NS red light meshes.")]
    public GameObject nsRedLights;

    [Header("East / West Light Groups")]
    [Tooltip("The GameObject that holds all EW green light meshes.")]
    public GameObject ewGreenLights;

    [Tooltip("The GameObject that holds all EW red light meshes.")]
    public GameObject ewRedLights;

    // ──────────────────────────────────────────────────────────
    //  PUBLIC STATE  (read these from CarAI, StopZone, or UI)
    // ──────────────────────────────────────────────────────────

    [Header("Current State  (Read-Only at Runtime)")]
    [Tooltip("True when North/South direction has a green light.")]
    public bool isNorthSouthGreen = false;

    [Tooltip("True when East/West direction has a green light.")]
    public bool isEastWestGreen = false;

    // ──────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ──────────────────────────────────────────────────────────

    private void Start()
    {
        // Safety check – warn the developer early if anything is missing.
        if (!ValidateAssignments()) return;

        // Start the intersection with North/South GREEN by default.
        SetNorthSouthGreen();
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    // ──────────────────────────────────────────────────────────
    //  INPUT
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reads keyboard input each frame.
    /// Press 1 → North/South Green.
    /// Press 2 → East/West Green.
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetNorthSouthGreen();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetEastWestGreen();
        }
    }

    // ──────────────────────────────────────────────────────────
    //  PUBLIC SWITCHING METHODS
    //  Call these from other scripts (UI buttons, auto-timers, etc.)
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Sets North/South to GREEN and East/West to RED.
    /// Safe to call from other scripts (e.g. a UI button).
    /// </summary>
    public void SetNorthSouthGreen()
    {
        // NS → Green
        SetActive(nsGreenLights, true);
        SetActive(nsRedLights,   false);

        // EW → Red
        SetActive(ewGreenLights, false);
        SetActive(ewRedLights,   true);

        // Update state flags
        isNorthSouthGreen = true;
        isEastWestGreen   = false;

        Debug.Log("[TrafficLight] North/South → GREEN  |  East/West → RED");
    }

    /// <summary>
    /// Sets East/West to GREEN and North/South to RED.
    /// Safe to call from other scripts (e.g. a UI button).
    /// </summary>
    public void SetEastWestGreen()
    {
        // EW → Green
        SetActive(ewGreenLights, true);
        SetActive(ewRedLights,   false);

        // NS → Red
        SetActive(nsGreenLights, false);
        SetActive(nsRedLights,   true);

        // Update state flags
        isEastWestGreen   = true;
        isNorthSouthGreen = false;

        Debug.Log("[TrafficLight] East/West → GREEN  |  North/South → RED");
    }

    // ──────────────────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Null-safe wrapper around SetActive().
    /// Prevents a missing reference from crashing the game.
    /// </summary>
    private void SetActive(GameObject lightGroup, bool active)
    {
        if (lightGroup != null)
        {
            lightGroup.SetActive(active);
        }
        else
        {
            Debug.LogWarning("[TrafficLight] A light group reference is missing (null). " +
                             "Check all four Inspector slots on TrafficLightController.", this);
        }
    }

    /// <summary>
    /// Checks that all four light group references are assigned.
    /// Logs one clear error message per missing reference on Start().
    /// Returns false if anything is missing so Start() can bail out safely.
    /// </summary>
    private bool ValidateAssignments()
    {
        bool valid = true;

        if (nsGreenLights == null) { LogMissing("NS Green Lights"); valid = false; }
        if (nsRedLights   == null) { LogMissing("NS Red Lights");   valid = false; }
        if (ewGreenLights == null) { LogMissing("EW Green Lights"); valid = false; }
        if (ewRedLights   == null) { LogMissing("EW Red Lights");   valid = false; }

        return valid;
    }

    private void LogMissing(string fieldName)
    {
        Debug.LogError($"[TrafficLight] '{fieldName}' is not assigned in the Inspector! " +
                       $"Please drag the correct GameObject into the '{fieldName}' slot " +
                       $"on the TrafficLightController component.", this);
    }

    // ──────────────────────────────────────────────────────────
    //  GIZMOS  (Scene View label so you can identify this object)
    // ──────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw a small traffic-light icon position in Scene View
        UnityEditor.Handles.color = isNorthSouthGreen ? Color.green : Color.red;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            isNorthSouthGreen
                ? "NS: GREEN  |  EW: RED"
                : "NS: RED    |  EW: GREEN"
        );
    }
#endif
}