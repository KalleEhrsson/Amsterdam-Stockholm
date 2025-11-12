using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Camera_Train_Movement : MonoBehaviour
{
    [Header("Camera and Train Position")]
    [SerializeField] private Transform cameraposistion;
    [SerializeField] private Transform trainposistion;

    [Header("Movement Settings")]
    [SerializeField] private float cameraSpeed;
    private Vector3 offset;
    private Vector3 smoothVelocity = Vector3.zero;

    [Header("Mouse Control Settings")]
    [SerializeField] private float mousesmoothing;

    [Header("Rotation Limits (Turning Radius)")]
    [SerializeField] private float maxHorizontalAngle; // Left-right turn limit
    [SerializeField] private float maxVerticalAngle;   // Up-down turn limit

    private float yaw;   // horizontal rotation
    private float pitch; // vertical rotation

    private void Start()
    {
        offset = cameraposistion.position - trainposistion.position;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (Mouse.current != null)
        {
            yaw += Mouse.current.delta.x.ReadValue() * mousesmoothing;
            pitch -= Mouse.current.delta.y.ReadValue() * mousesmoothing;

            // Clamp rotation angles to define turning radius
            yaw = Mathf.Clamp(yaw, -maxHorizontalAngle, maxHorizontalAngle);
            pitch = Mathf.Clamp(pitch, -maxVerticalAngle, maxVerticalAngle);
        }

        // Apply rotation to offset
        Quaternion camerarotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = trainposistion.position + camerarotation * offset;

        cameraposistion.position = Vector3.SmoothDamp(
            cameraposistion.position,
            desiredPosition,
            ref smoothVelocity,
            cameraSpeed * Time.deltaTime
        );

        cameraposistion.LookAt(trainposistion.position);
    }
}
