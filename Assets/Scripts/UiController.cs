using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UiController : MonoBehaviour
{
    public static UiController instance;
    
    public TMP_Text reloading, ammo;

    private void Awake()
    {
        instance = this;
    }

}
