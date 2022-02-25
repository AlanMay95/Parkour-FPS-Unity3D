using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiController : MonoBehaviour
{
    public static UiController instance;
    
    public TMP_Text reloading, ammo;
    public GameObject deathScreen;
    public TMP_Text deathText;
    public GameObject hitMarker;
    public Image damageIndicator;

    private void Awake()
    {
        instance = this;
    }

}
