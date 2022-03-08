using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

    public void Awake()
    {
        instance = this;
    }

    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }

    public int killsToWin = 5;
    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterEnding = 5f;
    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;
    private List<LeaderBoardPlayer> lboardPlayers = new List<LeaderBoardPlayer>();
    public bool perpetual;




    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        } else
        {
            NewPlayerSend(PhotonNetwork.NickName);

            state = GameState.Playing;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Tab) && state != GameState.Ending)
        {
                ShowLeaderboard();
        }
        if (Input.GetKeyUp(KeyCode.Tab) && state != GameState.Ending)
        {
            UiController.instance.scoreboard.SetActive(false);
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if(photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            Debug.Log("Recieved event " + theEvent);

            switch(theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerRecieve(data);
                    break;
                case EventCodes.ListPlayers:
                    ListPlayersRecieve(data);
                    break;
                case EventCodes.UpdateStat:
                    UpdateStatsRecieve(data);
                    break;
                case EventCodes.NextMatch:
                    NextMatchRecieve();
                    break;

            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent((byte)EventCodes.NewPlayer, package, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, new SendOptions { Reliability = true });
    }

    public void NewPlayerRecieve(object[] dataRecieved)
    {
        PlayerInfo player = new PlayerInfo((string)dataRecieved[0], (int)dataRecieved[1], (int)dataRecieved[2], (int)dataRecieved[3]);

        allPlayers.Add(player);

        ListPlayersSend();
    }
    public void ListPlayersSend()
    {
        object[] package = new object[allPlayers.Count + 1];

        package[0] = state;

        for(int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent((byte)EventCodes.ListPlayers, package, new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
    }

    public void ListPlayersRecieve(object[] dataRecieved)
    {
        allPlayers.Clear();

        state = (GameState)dataRecieved[0];

        for (int i = 1; i < dataRecieved.Length; i++)
        {
            object[] piece = (object[])dataRecieved[i];

            PlayerInfo player = new PlayerInfo((string)piece[0], (int)piece[1], (int)piece[2], (int)piece[3]);
            allPlayers.Add(player);
            
            if(PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

        //Debug.Log("State check should happen now");
        StateCheck();
    }
    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent((byte)EventCodes.UpdateStat, package, new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
    }

    public void UpdateStatsRecieve(object[] dataRecieved)
    {
        int actor = (int)dataRecieved[0];
        int stat = (int)dataRecieved[1];
        int amount = (int)dataRecieved[2];

        for( int i = 0; i < allPlayers.Count; i++)
        {
            if(allPlayers[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: //kills
                        allPlayers[i].kills += amount;
                        Debug.Log("Player " + allPlayers[i].name + " : kills " + allPlayers[i].kills);
                        break;

                    case 1: //deaths
                        allPlayers[i].deaths += amount;
                        Debug.Log("Player " + allPlayers[i].name + " : deaths " + allPlayers[i].deaths);
                        break;
                }

                if (UiController.instance.scoreboard.activeInHierarchy)
                {
                    ShowLeaderboard();
                }

                break;
            }
        }

        //Debug.Log("Score check should happen now");
        ScoreCheck();
    }

    void ShowLeaderboard()
    {
       // Debug.Log("ShowLeaderboard was ran");
        UiController.instance.scoreboard.SetActive(true);
       // Debug.Log("Leaderboard should be active");

        foreach (LeaderBoardPlayer lp in lboardPlayers)
        {
            Destroy(lp.gameObject);
        }
        lboardPlayers.Clear();

        UiController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers);
        
        foreach(PlayerInfo player in sorted)
        {
            LeaderBoardPlayer newPlayerDisplay = Instantiate(UiController.instance.leaderboardPlayerDisplay, UiController.instance.leaderboardPlayerDisplay.transform.parent);

            newPlayerDisplay.SetDetails(player.name, player.kills, player.deaths);

            newPlayerDisplay.gameObject.SetActive(true);
            lboardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while(sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];
            
            foreach(PlayerInfo player in players)
            {
                if(player.kills > highest)
                {
                    if (!sorted.Contains(player))
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }

            sorted.Add(selectedPlayer);

        }

        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        
        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        bool winnerFound = false;

        foreach(PlayerInfo player in allPlayers)
        {
            if(player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if(PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
               // Debug.Log("Listplayersupdate should happen now");
                ListPlayersSend();
            }
        }
    }

    void StateCheck()
    {
      //  Debug.Log("statecheck has happened");
        if(state == GameState.Ending)
        {
           // Debug.Log("endgame should happen now");
            EndGame();
        }
    }

    void EndGame()
    {
        //Debug.Log("Endgame has happened");
        state = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UiController.instance.roundOver.SetActive(true);
        UiController.instance.scoreboardText.SetActive(false);
       // Debug.Log("Show Leaderboard should happen now");
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.transform.position;
        Camera.main.transform.rotation = mapCamPoint.transform.rotation;

        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);

        if (!perpetual) 
        { 
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!Launcher.instance.changeMapBetweenRounds)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, Launcher.instance.allMaps.Length);

                    if (Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
                    }
                }
            }
        }
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent((byte)EventCodes.NextMatch, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });
    }

    public void NextMatchRecieve()
    {
        state = GameState.Playing;

        UiController.instance.roundOver.SetActive(false);
        UiController.instance.scoreboardText.SetActive(true);
        UiController.instance.scoreboard.SetActive(false);

        foreach (PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        PlayerSpawner.instance.SpawnPlayer();
    }


}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}