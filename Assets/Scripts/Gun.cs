using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public bool isAutomatic;
    public float timeBetweenShots = .1f; 
    public int maxAmmoCount = 20, ammoCount = 0;
    public GameObject muzzleFlash;
}
