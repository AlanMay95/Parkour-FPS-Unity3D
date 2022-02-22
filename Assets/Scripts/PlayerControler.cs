using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    private bool isGrounded, canWallRunRight, canWallRunLeft, canWallClimb, canWallJump, isReloading;
    public LayerMask groundLayers, wallLayers, killBoxLayers;
    public GameObject bulletImpact;
    public float shotCounter;
    public float muzzelDisplayTime;
    private float muzzleCounter;
    public int reloadTime;
    public Gun[] allGuns;
    private int selectedGun;
    private bool killBox;



    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        SwitchGun();

        Transform newTrans = SpawnManager.instance.GetFFASpawnPoint();
        transform.position = newTrans.position;
        transform.rotation = newTrans.rotation;
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

        //Jump checks
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .5f, groundLayers);
        canWallClimb = Physics.Raycast(transform.position, wallCheckPoint.forward, 1f, groundLayers);
        canWallRunRight = Physics.Raycast(transform.position, wallCheckPoint.right, 1f, groundLayers);
        canWallRunLeft = Physics.Raycast(transform.position, -wallCheckPoint.right, 1f, groundLayers);
        canWallJump = canWallClimb || canWallRunLeft || canWallRunRight;

        //Jumping
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

        if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;

            if (muzzleCounter <= 0)
            {
                allGuns[selectedGun].muzzleFlash.SetActive(false);
            }
        }

        if (isReloading)
            return;


        //Shooting
        //Single Shot
        if (Input.GetMouseButtonDown(0))
        {
                Shoot();
        }
        //Autofire
        if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
        {
            shotCounter -= Time.deltaTime;

            if (shotCounter <= 0)
            {
                    Shoot();
            } 

        }
        //Reload
        if (Input.GetKeyDown(KeyCode.R) || allGuns[selectedGun].ammoCount == 0)
        {
            StartCoroutine(Reload());
        }


        //AmmoDisplay
        UiController.instance.ammo.SetText(allGuns[selectedGun].ammoCount + " / " + allGuns[selectedGun].maxAmmoCount);

        //Wepon Swap
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun++;
            if (selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            SwitchGun();
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun--;
            if (selectedGun < 0)
            {
                selectedGun = allGuns.Length-1;
            }
            SwitchGun();
        }


        
        for(int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                SwitchGun();
            }
        }



        //Kill Box
        killBox = Physics.Raycast(transform.position, -groundCheckPoint.up, 1f, killBoxLayers);
        if (killBox)
        {
            Debug.Log("PLAYER HAS DIED");
        }






    }

    private void Shoot()
    {
        if (allGuns[selectedGun].ammoCount != 0)
        {
            allGuns[selectedGun].ammoCount -= 1;
            Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            ray.origin = cam.transform.position;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 2f);
            }

            shotCounter = allGuns[selectedGun].timeBetweenShots;
        }
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzelDisplayTime;
    }

    IEnumerator Reload()
    {
        isReloading = true;
        UiController.instance.reloading.gameObject.SetActive(true);
        Debug.Log("Start Reloading");
        allGuns[selectedGun].ammoCount = 0;
        yield return new WaitForSeconds(reloadTime);
        allGuns[selectedGun].ammoCount = allGuns[selectedGun].maxAmmoCount;
        Debug.Log("Reloaded");
        isReloading = false;
        UiController.instance.reloading.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        cam.transform.position = viewPoint.position;
        cam.transform.rotation = viewPoint.rotation;
    }

    void SwitchGun()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }
}
