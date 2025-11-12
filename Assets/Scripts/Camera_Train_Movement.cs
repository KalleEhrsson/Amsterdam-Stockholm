using UnityEngine;
using UnityEngine.InputSystem;

public class Camera_Train_Movement : MonoBehaviour
{
    [Header("Camera and Train Position")]
    [SerializeField] private Transform cameraposistion;
    [SerializeField] private Transform trainposistion;

    [Header("Movement Settings")]
    [SerializeField] private float cameraSpeed = 1.0f;
    private Vector3 offset;
    private Vector3 smoothVelocity = Vector3.zero;

    [Header("Mouse Control Settings")]
    [SerializeField] private float mousesmoothing = 2.0f;

    [Header("Rotation Limits")]
    private float mouseX;
    private float mouseY;
    private float rotationY = 4.0f;
    private float rotationX = 4.0f;

    private void Start()
    {
        offset = cameraposistion.position - trainposistion.position;
        Cursor.lockState = CursorLockMode.Locked;
    }

    #region trainmovement region

    private void Update()
    {
        //mousesmoothing = PlayerPrefs.GetFloat("Mouse_Sensitivity", 2.0f);
        if (Mouse.current != null)
        {
            mouseX += Mouse.current.delta.x.ReadValue() * mousesmoothing * Time.deltaTime;
            mouseY -= Mouse.current.delta.y.ReadValue() * mousesmoothing * Time.deltaTime;
            mouseY = Mathf.Clamp(mouseY, -20, 20);
        }


        Quaternion camerarotation = Quaternion.Euler(mouseY * rotationY, mouseX * rotationX, 0);
        Vector3 desiredPosition = trainposistion.position + camerarotation * offset;
        cameraposistion.position = Vector3.SmoothDamp(cameraposistion.position, desiredPosition, ref smoothVelocity, cameraSpeed * Time.deltaTime);
        cameraposistion.LookAt(trainposistion.position);

    }
    


    #endregion
}


