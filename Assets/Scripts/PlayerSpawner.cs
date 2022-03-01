using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;

    public void Awake()
    {
        instance = this;
    }

    public GameObject playerPrefab;
    private GameObject player;
    public GameObject deathEffect;
    public float respawnTime = 3f;



    public void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetFFASpawnPoint();

        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public void Die(string damager)
    {
        UiController.instance.deathText.SetText("You were killed by: " + damager);
        UiController.instance.hitMarker.SetActive(false);
        MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
        if(player != null)
        {
            StartCoroutine(Respawn());
        }
    }

    public IEnumerator Respawn()
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        UiController.instance.deathScreen.SetActive(true);

        yield return new WaitForSeconds(respawnTime);
        UiController.instance.deathScreen.SetActive(false);
        SpawnPlayer();
    }
}

