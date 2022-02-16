using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControler : MonoBehaviour
{
    public Transform viewPoint;
    public float mouseSensativity = 1.5f, moveSpeed = 5f, runSpeed = 8f, jumpForce = 3f, gravityMod = .9f;
    private float vertRotStore;
    public Vector2 mouseInput;
    public bool invertLook;
    private Vector3 moveDir, movement;
    public CharacterController charCon;
    private float activeMoveSpeed;
    private Camera cam;
    public Transform groundCheckPoint, wallCheckPoint;
    private bool isGrounded, canWallRunRight, canWallRunLeft, canWallClimb, canWallJump;
    public LayerMask groundLayers, wallLayers;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        //Horizontal Camera
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensativity;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        //Vertical Camera
        vertRotStore += mouseInput.y;
        vertRotStore = Mathf.Clamp(vertRotStore, -60f, 60f);
        //Camera Inversion
        if (invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(vertRotStore, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(-vertRotStore, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        }

        //Movement
        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        //Sprint check
        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        //Adding Gravity
        float yVel = movement.y;
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)) * activeMoveSpeed;
        movement.y = yVel;

        if (charCon.isGrounded)
        {
            movement.y = 0f;

        }

        //Jump
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);
        canWallClimb = Physics.Raycast(transform.position, wallCheckPoint.forward, 1f, wallLayers);
        canWallRunRight = Physics.Raycast(transform.position, wallCheckPoint.right, 1f, wallLayers);
        canWallRunLeft = Physics.Raycast(transform.position, -wallCheckPoint.right, 1f, wallLayers);
        canWallJump = canWallClimb || canWallRunLeft || canWallRunRight;

        if (Input.GetButtonDown("Jump") && (isGrounded || canWallJump))
        {
            movement.y = jumpForce;
        }


        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        charCon.Move( movement * Time.deltaTime);

        //Free the mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        } else if(Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

    }
    private void LateUpdate()
    {
        cam.transform.position = viewPoint.position;
        cam.transform.rotation = viewPoint.rotation;
    }
}
