using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AdvancedFreeCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float sprintSpeed = 52f;
    [SerializeField] private float precisionSpeed = 7f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;
    [SerializeField] private float verticalSpeedMultiplier = 0.9f;
    [SerializeField] private bool enableEdgeScroll = true;
    [SerializeField] private float edgeScrollMargin = 12f;
    [SerializeField] private float edgeScrollStrength = 0.45f;
    [SerializeField] private KeyCode doubleSpeedKey = KeyCode.Space;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 1.8f;
    [SerializeField] private bool invertY;
    [SerializeField] private float lookSmoothing = 16f;
    [SerializeField] private Vector2 pitchClamp = new Vector2(-80f, 80f);

    [Header("Zoom / FOV")]
    [SerializeField] private float zoomSpeed = 130f;
    [SerializeField] private float minFov = 25f;
    [SerializeField] private float maxFov = 85f;
    [SerializeField] private float fovSmoothTime = 0.08f;

    [Header("Optional Height Clamp")]
    [SerializeField] private bool useHeightClamp;
    [SerializeField] private float minHeight = 3f;
    [SerializeField] private float maxHeight = 350f;

    private Camera cachedCamera;
    private Vector3 currentVelocity;
    private Vector3 desiredVelocity;
    private float yaw;
    private float pitch;
    private float targetFov;
    private float fovVelocity;
    private bool cursorLocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMainCameraController()
    {
        Camera main = Camera.main;
        if (main != null && main.GetComponent<AdvancedFreeCameraController>() == null)
        {
            main.gameObject.AddComponent<AdvancedFreeCameraController>();
        }
    }

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
        targetFov = cachedCamera.fieldOfView;
        LockCursor(true);
    }

    private void Update()
    {
        HandleCursorState();
        HandleMouseLook(Time.deltaTime);
        HandleMovement(Time.deltaTime);
        HandleZoom(Time.deltaTime);
    }

    private void HandleMouseLook(float dt)
    {
        if (!cursorLocked)
        {
            return;
        }

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, pitchClamp.x, pitchClamp.y);

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-lookSmoothing * dt));
    }

    private void HandleMovement(float dt)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (cursorLocked && enableEdgeScroll)
        {
            Vector3 m = Input.mousePosition;
            if (m.x < edgeScrollMargin) horizontal -= edgeScrollStrength;
            if (m.x > Screen.width - edgeScrollMargin) horizontal += edgeScrollStrength;
            if (m.y < edgeScrollMargin) vertical -= edgeScrollStrength;
            if (m.y > Screen.height - edgeScrollMargin) vertical += edgeScrollStrength;
        }

        float upDown = 0f;
        if (Input.GetKey(KeyCode.E)) upDown += 1f;
        if (Input.GetKey(KeyCode.Q)) upDown -= 1f;

        float speed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speed = sprintSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            speed = precisionSpeed;
        }

        if (Input.GetKey(doubleSpeedKey))
        {
            speed *= 2f;
        }

        Vector3 planar = transform.right * horizontal + transform.forward * vertical;
        if (planar.sqrMagnitude > 1f)
        {
            planar.Normalize();
        }

        Vector3 verticalMove = Vector3.up * (upDown * speed * verticalSpeedMultiplier);
        desiredVelocity = planar * speed + verticalMove;

        float smoothRate = desiredVelocity.sqrMagnitude > currentVelocity.sqrMagnitude ? acceleration : deceleration;
        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, 1f - Mathf.Exp(-smoothRate * dt));
        transform.position += currentVelocity * dt;

        if (useHeightClamp)
        {
            Vector3 p = transform.position;
            p.y = Mathf.Clamp(p.y, minHeight, maxHeight);
            transform.position = p;
        }
    }

    private void HandleZoom(float dt)
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            targetFov = Mathf.Clamp(targetFov - scroll * zoomSpeed, minFov, maxFov);
        }

        cachedCamera.fieldOfView = Mathf.SmoothDamp(cachedCamera.fieldOfView, targetFov, ref fovVelocity, fovSmoothTime, Mathf.Infinity, dt);
    }

    private void HandleCursorState()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            LockCursor(true);
        }
    }

    private void LockCursor(bool shouldLock)
    {
        cursorLocked = shouldLock;
        Cursor.visible = !shouldLock;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
