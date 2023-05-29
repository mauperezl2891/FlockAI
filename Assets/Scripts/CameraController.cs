using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    private const string MOUSE_X = "Mouse X";
    private const string MOUSE_Y = "Mouse Y";

    [SerializeField] float moveSpeed;
    [SerializeField] float rotationSpeed;

    private float horizontalInput;
    private float verticalInput;
    private float mouseInputX;
    private float mouseInputY;
    private float currentRotationY;
    private float currentRotationX;


    private void Update()
    {
        GetInput();
        HandleTranslation();
        HandleRotation();
    }

    private void Start()
    {
        currentRotationY = transform.eulerAngles.y;
        currentRotationX = transform.eulerAngles.x;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw(HORIZONTAL);
        verticalInput = Input.GetAxisRaw(VERTICAL);
        mouseInputX = Input.GetAxisRaw(MOUSE_X);    
        mouseInputY = Input.GetAxisRaw(MOUSE_Y);    
    }

    private void HandleTranslation()
    {
        var moveVector = new Vector3(horizontalInput, 0f, verticalInput);
        transform.Translate(moveVector.normalized * Time.deltaTime * moveSpeed);
    }

    private void HandleRotation()
    {
        float yrot = mouseInputX * Time.deltaTime * rotationSpeed;
        currentRotationY += yrot;

        float pitch = mouseInputY * Time.deltaTime * rotationSpeed;
        currentRotationX -= pitch;
        currentRotationX = Mathf.Clamp(currentRotationX, -90, 90);
        transform.localRotation = Quaternion.Euler(currentRotationX, currentRotationY, 0); 
    }

}
