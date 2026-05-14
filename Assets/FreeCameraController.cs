using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float sprintSpeed = 50f;
    public float lookSpeed = 2f;

    float rotationX = 0f;
    float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.forward * moveZ + transform.right * moveX;

        if (Input.GetKey(KeyCode.E))
            move += Vector3.up;

        if (Input.GetKey(KeyCode.Q))
            move += Vector3.down;

        transform.position += move * speed * Time.deltaTime;

        rotationX += Input.GetAxis("Mouse X") * lookSpeed;
        rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;

        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}