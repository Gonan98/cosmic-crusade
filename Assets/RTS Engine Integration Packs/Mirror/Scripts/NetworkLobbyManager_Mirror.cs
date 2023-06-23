using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;
using System.Linq;

/* Network Map Manager (Mirror): script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NetworkLobbyManager_Mirror : NetworkRoomManager
    {
        [SerializeField, Header("RTS Engine")]
        private string gameVersion = "1.3.8"; //input the game version's into this field, players who don't share the same game version will not be able to join the same room
        public string GetGameVersion() { return gameVersion; }

        public bool IsHost { set; get; }//true when the player is the one who started the host.
        public bool InGame { set; get; } //when true then the game started already.

        public DisconnectionType LastDisconnectionType { set; get; } //the last disconnection reason for the local player is held in this attribute

        public static NetworkLobbyFaction_Mirror LocalLobbyFaction { set; get; } //the local player's lobby object.
        public static List<NetworkLobbyFaction_Mirror> LobbyFactions { set; get; } //all the lobby faction components are stored in this list

        [SerializeField]
        private FactionColorMenu factionColor = new FactionColorMenu();
        public FactionColorMenu FactionColor { private set { factionColor = value; } get { return factionColor; } }

        [SerializeField, Tooltip("Define the maps that players can use in a multiplayer game.")]
        private MapMenu[] availableMaps = new MapMenu[0]; //a list of the game maps that the host can pick for a game
        /// <summary>
        /// Holds the current map ID.
        /// </summary>
        public int CurrentMapID { private set; get; } //the ID of the currently chosen map.
        /// <summary>
        /// Gets the MapMenu instance that defines an available map, if the provided index is valid.
        /// </summary>
        /// <param name="ID">Index of the available map.</param>
        /// <returns>MapMenu instance of the map if the index is valid, otherwise null.</returns>
        public MapMenu GetMap (int ID)
        {
            if(ID < 0 || ID >= availableMaps.Length) //make sure the map ID is valid.
            {
                Debug.LogError($"[NetworkLobbyManager_Mirror] {ID} is an invalid map ID!");
                return null;
            }

            return availableMaps[ID];
        }

        public List<string> GetMapFactionTypeNames() //get the faction type names of a certain map
        {
            return availableMaps[CurrentMapID].GetFactionTypeNames();
        }

        public FactionTypeInfo GetFactionTypeInfo(int index) //get the faction type info with a certain index
        {
            return availableMaps[CurrentMapID].GetFactionTypeInfo(index);
        }

        public int GetMapInitialPopulation() { return availableMaps[CurrentMapID].GetInitialPopulation(); } //get the initial population of the current map
        public void UpdateMap(int ID, int defeatConditionIndex, int speedModifierIndex) //change the lobby map
        {
            CurrentMapID = ID; //update the map ID
            GameplayScene = availableMaps[CurrentMapID].GetScene(); //set the Play scene to load when the game starts.
            maxConnections = availableMaps[CurrentMapID].GetMaxFactions(); //update the max connections to the max amount of factions.

            UIMgr.UpdateMapUIInfo(CurrentMapID,
                availableMaps[CurrentMapID].GetDescription(),
                availableMaps[CurrentMapID].GetInitialPopulation().ToString(),
                availableMaps[CurrentMapID].GetMaxFactions().ToString()); //update the map UI menu.

            //set defeat condition and speed modifier:
            UIMgr.defeatConditionMenu.MenuIndex = defeatConditionIndex;
            UIMgr.speedModifierMenu.MenuIndex = speedModifierIndex;
        }

        public List<Entity> spawnablePrefabs = new List<Entity>(); //unit, building and resource objects that can be spawned in a multiplayer game are registered here.

        //in case the map allows to randomize the player faction slots then we'll use the faction slots (which should be the same for all players in the lobby, chosen by the host when the game starts).
        public int[] FactionSlotIndexes { set; get; }

        //other components:
        public NetworkLobbyManagerUI_Mirror UIMgr { private set; get; }
        GameManager gameMgr;

        public override void Awake()
        {
            base.Awake();
            
            Time.timeScale = 1.0f; //When the game ends, the time scale is set to 0.0f, we reset it now in case we're coming from a finished game

            LobbyFactions = new List<NetworkLobbyFaction_Mirror>(); //clear the lobby factions list
            LocalLobbyFaction = null;

            UIMgr = GetComponent<NetworkLobbyManagerUI_Mirror>();

            UIMgr.AssignGameVersionText(gameVersion); //update the game version's UI.

            Assert.IsTrue(availableMaps.Length > 0, "[NetworkLobbyManager_Mirror] At least one map has to be assigned.");

            CurrentMapID = 0; //set the map ID to 0 by default.
            List<string> mapNames = new List<string>(); //this list will hold all map names

            for (int i = 0; i < availableMaps.Length; i++) //go through all available maps
            {
                availableMaps[i].Validate(i, "NetworkLobbyManager_Mirror"); //see if their attributes are valid or not
                mapNames.Add(availableMaps[i].GetName()); //add the map name to the list
            }

            UIMgr.UpdateMapDropDownMenu(mapNames); //to show the available map names in the map drop down menu

            IsHost = false;
            InGame = false;
        }

        //a method called when the client can't connect to a lobby
        public override void OnClientError(NetworkConnection conn, int errorID)
        {
            base.OnClientError(conn, errorID);

            UIMgr.ShowInfoMessage("Connection to game has failed, error ID: " + errorID.ToString());
            UIMgr.EnableMenu(MultiplayerMenu.main); //get the player back to the main multiplayer menu.
        }

        //called when a client connects to a lobby
        public override void OnRoomClientConnect(NetworkConnection conn)
        {
            base.OnRoomClientConnect(conn);

            UIMgr.ShowInfoMessage("Connection to lobby is successful");
            UIMgr.EnableMenu(MultiplayerMenu.lobby); //enable the lobby's menu.
        }

        //a method called when the client disconnects:
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);

            UIMgr.ShowInfoMessage("The room/game is no longer available.");

            if (gameMgr != null) //if the player wasn't in game when the disconnection happenend
                UIMgr.EnableMenu(MultiplayerMenu.main); //show the main multiplayer menu
        }

        //a method called when the client leaves a game/lobby:
        public override void OnStopClient()
        {
            base.OnStopClient();

            if (InGame) //if the player was in a game already and disconnected/left 
            {
                InGame = false;
                UIMgr.Enable(); //enable the multiplayer lobby menu again
            }

            switch (LastDisconnectionType) //check the disconnection reason and show the player a message depending on the disconnection type.
            {
                case DisconnectionType.left:
                    UIMgr.ShowInfoMessage("You left the room/game.");
                    break;
                case DisconnectionType.kick:
                    UIMgr.ShowInfoMessage("You've been kicked from the game.");
                    break;
                case DisconnectionType.gameVersion:
                    UIMgr.ShowInfoMessage("You don't have the same game version as the lobby's host.");
                    break;
                case DisconnectionType.abort:
                    UIMgr.ShowInfoMessage("Connection to lobby has been aborted.");
                    break;
                case DisconnectionType.timeOut:
                    UIMgr.ShowInfoMessage("Your connection timed out.");
                    break;
                default:
                    break;
            }

            UIMgr.EnableMenu(MultiplayerMenu.main); //show the main multiplayer menu
        }

        //a method called when the player starts hosting:
        public override void OnStartHost()
        {
            base.OnStartHost();

            IsHost = true; //mark as host.
        }

        //a method that removes a player from a lobby
        public void LeaveLobby ()
        {
            if (IsHost == true) //if the player was the host
                StopHost(); //stop hosting the lobby
            else
            {
                StopClient(); //if it was a normal player, just stop the client
                //UIMgr.LoadMainMenu();
            }

            LobbyFactions = new List<NetworkLobbyFaction_Mirror>(); //clear the lobby factions list
        }

        //called on the server when a player disconnects
        public override void OnRoomServerDisconnect(NetworkConnection conn)
        {
            if (InGame == true)
            {
                //game started -> we need to destroy units and buildings from the player's faction that disconnected.
                foreach(FactionSlot faction in gameMgr.GetFactions())
                    if (faction.ConnID_Mirror == conn.connectionId) //if this is faction that belongs to the player that just disconnected
                    {
                        NetworkInput newInput = new NetworkInput()
                        {
                            sourceMode = (byte)InputMode.destroy,
                            targetMode = (byte)InputMode.faction,
                            value = faction.ID
                        };

                        InputManager.SendInput(newInput, null, null); //send the input to destroy this faction
                        break;
                    }
            }

            base.OnRoomServerDisconnect(conn);
        }
        
        //a method that sends the ready status of the host, this triggers the start of the game
        public void ActivateHostReadyState ()
        {
            if (IsHost == true) //making sure this is the host
                LocalLobbyFaction.CmdChangeReadyState(true);
        }

        //a method called when the game scene is loaded for the player
        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection networkConnection, GameObject lobbyPlayer, GameObject gamePlayer)
        {
            base.OnRoomServerSceneLoadedForPlayer(networkConnection, lobbyPlayer, gamePlayer);

            gamePlayer.GetComponent<NetworkFactionManager_Mirror>().CmdInit(lobbyPlayer.GetComponent<NetworkLobbyFaction_Mirror>().index); //sync the faction ID.
            gameMgr = FindObjectOfType(typeof(GameManager)) as GameManager; //get the game manager component
            return true;
        }

        //called when the game map/scene is loaded for a client:
        public void OnGameMapLoaded ()
        {
            InGame = true; //player is now in game
            UIMgr.Disable(); //disable the network lobby menu's UI.
            LastDisconnectionType = DisconnectionType.timeOut; //so that if this client unexpectedly disconnects, it will be marked as a time out.
        }

        public override void OnRoomServerPlayersReady()
        {
            //override this method because we don't want the game play scene to load as soon as all players are ready, the host starts the game when all players are ready
        }

        //called by the host only when all players are ready to start the game
        public void LoadGameMapScene()
        {
            if(IsHost == true && allPlayersReady == true)
                ServerChangeScene(GameplayScene);
        }
    }
}