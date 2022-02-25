using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerControler : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;
    public float mouseSensativity = 1.5f, moveSpeed = 5f, runSpeed = 8f, wallRunSpeed = 10f, jumpForce = 3f, gravityMod = .9f;
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
    public Gun[] allGuns;
    private int selectedGun;
    private bool killBox;
    public GameObject playerHitImpact;
    public float maxHealth = 100;
    public float currentHealth = 100;
    public float healFactor;
    public float missingHelath = 0;
    public Animator knifeAnim;
    private bool canDoubleJump;
    public float timeToHeal = 2f;




    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        UiController.instance.damageIndicator.gameObject.SetActive(false);


        //Transform newTrans = SpawnManager.instance.GetFFASpawnPoint();
        //transform.position = newTrans.position;
        //transform.rotation = newTrans.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {

            //Horizontal Camera
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensativity;
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            //Vertical Camera
            vertRotStore += mouseInput.y;
            vertRotStore = Mathf.Clamp(vertRotStore, -85f, 85f);
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
            if((canWallRunLeft || canWallRunRight) && !isGrounded)
            {
                activeMoveSpeed = wallRunSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
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

            if (isGrounded || canWallJump)
            {
                canDoubleJump = true;
            }

            //Jumping
            if (Input.GetButtonDown("Jump"))
            {
                if (isGrounded || canWallJump)
                {
                    Jump();
                }
                else if (canDoubleJump)
                {
                    Jump();
                    canDoubleJump = false;
                }
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charCon.Move(movement * Time.deltaTime);

            //Free the mouse
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
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


            //Shooting
            //Single Shot
            if (Input.GetMouseButtonDown(0))
            {
                if (!isReloading && selectedGun != 0)
                {
                    Shoot();
                }
                else if (selectedGun == 0)
                {
                    Stab();
                } 
            }
            //Autofire
            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic && !isReloading && selectedGun != 0)
            {
                shotCounter -= Time.deltaTime;

                if (shotCounter <= 0)
                {
                    Shoot();
                }

            }
            //Reload
            if (!isReloading && selectedGun != 0 && ((Input.GetKeyDown(KeyCode.R) || (Input.GetMouseButtonDown(0) && allGuns[selectedGun].ammoCount == 0))))
            {
                StartCoroutine("Reload");
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
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                
                selectedGun--;
                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }



            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
            }

            //Kill Box
           
            killBox = Physics.Raycast(transform.position, -groundCheckPoint.up, 1f, killBoxLayers);
            if (killBox)
            {
                PlayerSpawner.instance.Die("Out of bounds");
            }
            
            if (currentHealth < maxHealth)
            {
                timeToHeal -= Time.deltaTime;
                missingHelath = ((maxHealth - currentHealth) * .01f);

                UiController.instance.damageIndicator.gameObject.SetActive(true);
                var tempColor = UiController.instance.damageIndicator.color;
                tempColor.a = missingHelath;
                UiController.instance.damageIndicator.color = tempColor;

                if (timeToHeal <= 0)
                {
                    currentHealth += healFactor;
                    missingHelath = ((maxHealth - currentHealth) * .01f);
                    tempColor.a = missingHelath;
                    UiController.instance.damageIndicator.color = tempColor;
                }
            }
            else if(currentHealth >= maxHealth)
            {
                currentHealth = maxHealth;
                UiController.instance.damageIndicator.gameObject.SetActive(false);
            }

        }

    }

    private void Shoot()
    {
        if (allGuns[selectedGun].ammoCount != 0 && !isReloading)
        {
            allGuns[selectedGun].ammoCount -= 1;
            Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            ray.origin = cam.transform.position;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.tag == "Player")
                {
                    Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                    PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                    StartCoroutine(HitMarker());
                    

                    hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].damage);
                }
                else
                {
                    GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                    Destroy(bulletImpactObject, 2f);
                }
            }

            shotCounter = allGuns[selectedGun].timeBetweenShots;
            allGuns[selectedGun].muzzleFlash.SetActive(true);
            muzzleCounter = muzzelDisplayTime;
        }
    }

    private void Stab()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;
        knifeAnim.SetTrigger("Stab");
        


        if (Physics.Raycast(ray, out RaycastHit hit, 2.5f))
        {
            if (hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                StartCoroutine(HitMarker());


                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].damage);
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 2f);
            }
        }
    }

    private void Jump()
    {
        movement.y = jumpForce;
    }

    IEnumerator HitMarker()
    {
        UiController.instance.hitMarker.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        UiController.instance.hitMarker.gameObject.SetActive(false);
    }

    IEnumerator Reload()
    {
        isReloading = true;
        UiController.instance.reloading.gameObject.SetActive(true);
        allGuns[selectedGun].ammoCount = 0;
        yield return new WaitForSeconds(allGuns[selectedGun].reloadTime);
        allGuns[selectedGun].ammoCount = allGuns[selectedGun].maxAmmoCount;
        isReloading = false;
        UiController.instance.reloading.gameObject.SetActive(false);
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount)
    {
        TakeDamage(damager, damageAmount);
    }

    public void TakeDamage(string damager, int damageAmount)
    {
        if (photonView.IsMine)
        {
            //Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);

            currentHealth -= damageAmount;
            timeToHeal = 2f;
            
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
            }
        }
    }

    

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
        cam.transform.position = viewPoint.position;
        cam.transform.rotation = viewPoint.rotation;
        }    
    }

    void SwitchGun()
    {
        StopCoroutine("Reload");
        UiController.instance.reloading.gameObject.SetActive(false);
        isReloading = false;
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }
}
