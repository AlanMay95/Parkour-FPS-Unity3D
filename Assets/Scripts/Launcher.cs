using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

    private void Awake()
    {
        instance = this;
    }

    public GameObject loadingScreen;
    public TMP_Text loadingText;   
    public GameObject menuButtons;
    public GameObject createGameScreen;
    public TMP_InputField gameNameInput;
    public GameObject gamePanel;
    public TMP_Text gameName, playerName;
    private List<TMP_Text> allPlayerNames = new List<TMP_Text>();
    public GameObject errorPanel;
    public TMP_Text errorText;
    public GameObject gameBrowserScreen;
    public GameButton gameButton;
    private List<GameButton> allGameButtons = new List<GameButton>();
    public GameObject namePanel;
    public TMP_Text nameInput;
    public static bool hasSetName;
    public string levelToPlay;
    public GameObject startButton;
    public GameObject testGameButton;
    public string[] allMaps;
    public bool changeMapBetweenRounds = true;




    void Start()
    {
        CloseMenus();
        
        loadingScreen.SetActive(true);
        loadingText.SetText("Connecting to the server...");

        PhotonNetwork.ConnectUsingSettings();

#if UNITY_EDITOR
        testGameButton.SetActive(true);
#endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createGameScreen.SetActive(false);
        gamePanel.SetActive(false);
        errorPanel.SetActive(false);
        gameBrowserScreen.SetActive(false);
        namePanel.SetActive(false);

    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();

        PhotonNetwork.AutomaticallySyncScene = true;
        loadingText.SetText("Joining lobby...");
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        PhotonNetwork.NickName = "Player " + Random.Range(0, 1000).ToString();

        
        if (!hasSetName)
        {
            CloseMenus();
            namePanel.SetActive(true);

            if (PlayerPrefs.HasKey("playerName"))
            {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
            
        }
        else
        {
           PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }


    public void OpenGameCreate()
    {
        CloseMenus();
        createGameScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(gameName.text))
        {
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 12;

            PhotonNetwork.CreateRoom(gameNameInput.text, options);

            CloseMenus();
            loadingScreen.SetActive(true);
            loadingText.SetText("Creating game...");
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        gamePanel.SetActive(true);

        gameName.SetText(PhotonNetwork.CurrentRoom.Name);

        listAllPlayers();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }else
        {
            startButton.SetActive(false);
        }
    }

    private void listAllPlayers()
    {
        foreach(TMP_Text player in allPlayerNames)
        {
            Destroy(player.gameObject);
        }
        allPlayerNames.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        for(int i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerName = Instantiate(playerName, playerName.transform.parent);
            newPlayerName.text = players[i].NickName;
            newPlayerName.gameObject.SetActive(true);

            allPlayerNames.Add(newPlayerName);

        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerName = Instantiate(playerName, playerName.transform.parent);
        newPlayerName.text = newPlayer.NickName;
        newPlayerName.gameObject.SetActive(true);

        allPlayerNames.Add(newPlayerName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        listAllPlayers();
    }


    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorPanel.SetActive(true);
        errorText.SetText("Failed to create room: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorPanel.SetActive(true);
        errorText.SetText("Failed to create room: " + message);
    }

    public void CloseError()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.SetText("Leaving game...");
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenGameBrowser()
    {
        CloseMenus();
        gameBrowserScreen.SetActive(true);
    }

    public void CloseGameBroswer()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(GameButton gb in allGameButtons)
        {
            Destroy(gb.gameObject);
        }
        allGameButtons.Clear();

        gameButton.gameObject.SetActive(false);

        for(int i = 0; i < roomList.Count; i++)
        {
            if( roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                GameButton newButton = Instantiate(gameButton, gameButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);

                allGameButtons.Add(newButton);
            }
        }
    }

    public void JoinGame(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenus();
        loadingText.SetText("Joining game...");
        loadingScreen.SetActive(true);
    }

    public void SetName()
    {
        if(!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName = nameInput.text;

            PlayerPrefs.SetString("playerName", nameInput.text);

            CloseMenus();
            menuButtons.SetActive(true);

            hasSetName = true;
        }
    }

    public void StartGame()
    {
        //PhotonNetwork.LoadLevel(levelToPlay);

        PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public void QuickJoin()
    {
        PhotonNetwork.CreateRoom("Test");
        CloseMenus();
        loadingText.SetText("Creating Room");
        loadingScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
