using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

/* Network Map Manager UI (Mirror): script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NetworkLobbyManagerUI_Mirror : MonoBehaviour
    {
        [Header("General")]

        [SerializeField, Scene]
        private string mainMenuScene = ""; //the name of the scene that the player gets back to when leaving the multiplayer menu.

        [SerializeField]
        private Canvas mainCanvas = null; //the main canvas object which should be a child object of the object that has this component.

        [SerializeField]
        private Text gameVersionText = null; //the text that will display the game version
        public void AssignGameVersionText(string gameVersion)
        { //display the game version on the UI text
            if (gameVersionText) //only if it has been already assigned.
                gameVersionText.text = gameVersion;
        }

        private MultiplayerMenu currentMenu;

        [Header("Main MP Menu")]

        [SerializeField]
        private GameObject mainMPMenu = null; //the main multiplayer menu (shown when the multiplayer menu scene starts).
        [SerializeField]
        private InputField addressInputField = null;
        [SerializeField]
        private InputField portInputField = null;

        [Header("Loading Menu")]

        [SerializeField]
        private GameObject loadingMenu = null; //the menu shown when loading to access the lobby.

        [SerializeField]
        private Text infoMessageText = null; //a message shown whenever there's an error/warning.
        [SerializeField]
        private float infoMessageReload = 2.0f;
        private float infoMessageTimer; //how long will the message be shown for.
        public void ShowInfoMessage(string message) //a method that activates the info message text and show a message
        {
            if (infoMessageText == null) //if the info message text UI element is invalid (in case it's destroyed when loading the game).
                return; //do not proceed.

            infoMessageTimer = infoMessageReload;
            infoMessageText.text = message;
            infoMessageText.gameObject.SetActive(true);
        }

        [Header("Lobby Menu")]

        [SerializeField]
        private GameObject lobbyMenu = null; //the lobby menu (shown when the player joins a match).
        [SerializeField]
        private Text lobbyName = null; //the UI Text to show the lobby's name

        [SerializeField]
        private Transform playerLobbyObjectsParent = null; //the parent object of all the player lobby objects.

        public void SetLobbyFactionParent(Transform lobbyFaction)
        {
            lobbyFaction.SetParent(playerLobbyObjectsParent, false);
        }

        [Header("Map Menu")]

        [SerializeField]
        private GameObject mapMenu = null; //the map settings menu (the one that has the map info).
        [SerializeField]
        private Dropdown mapDropDownMenu = null; //the menu that allows the host to select the map.
        public void OnMapChanged () //called when the host changes the map from the map drop down menu
        {
            NetworkLobbyManager_Mirror.LocalLobbyFaction.OnMapUpdated(mapDropDownMenu.value, true, defeatConditionMenu.MenuIndex, speedModifierMenu.MenuIndex);
        }

        public void ToggleMapSettings () //toggles the map interaction settings depending on whether the player is the host or not
        {
            mapDropDownMenu.interactable = manager.IsHost;
        }

        public void UpdateMapDropDownMenu(List<string> mapNames) //update the map drop down menu with the available map names
        {
            mapDropDownMenu.ClearOptions();
            mapDropDownMenu.AddOptions(mapNames);
        }
        //a method that updates the selected map's UI
        public void UpdateMapUIInfo(int ID, string description, string InitialPopulation, string maxFactions)
        {
            mapDropDownMenu.value = ID;

            //show the map's info: population, description and max factions:
            mapInitialPopulationText.text = InitialPopulation;
            mapDescriptionText.text = description;
            mapMaxFactionsText.text = maxFactions;
        }

        [SerializeField]
        private Text mapInitialPopulationText = null; //The UI text to show the map's initial population.
        [SerializeField]
        private Text mapDescriptionText = null; //The UI text to show the selected map's description.
        [SerializeField]
        private Text mapMaxFactionsText = null; //The UI text to show the map's maximum faction.

        //defeat condition:
        public DefeatConditionMenu defeatConditionMenu = new DefeatConditionMenu(); //the host can pick the defeat condition for the game using this menu

        public SpeedModifierMenu speedModifierMenu = new SpeedModifierMenu(); //the host can pick the speed modifier for the game using this menu

        [SerializeField]
        private Button startGameButton = null; //only shown for the host to launch the game.
        public void ToggleStartGameButton (bool show) { startGameButton.gameObject.SetActive(show); }

        NetworkLobbyManager_Mirror manager;
        TelepathyTransport transport;

        private void Awake()
        {
            infoMessageText.gameObject.SetActive(false); //hide the info message text UI element

            //get the network components
            manager = GetComponent<NetworkLobbyManager_Mirror>();
            transport = GetComponent<TelepathyTransport>();

            EnableMenu(MultiplayerMenu.main); //by default, we're in the main MP menu

            //initialize the defeat condition & speed modifier drop down menu options.
            defeatConditionMenu.Init(); 
            speedModifierMenu.Init(); 
        }

        private void Update()
        {
            if (infoMessageText && infoMessageText.gameObject.activeInHierarchy == true) //if the info message text is valid enabled
            {
                if (infoMessageTimer > 0) //run the timer
                    infoMessageTimer -= Time.deltaTime;
                else
                    infoMessageText.gameObject.SetActive(false);
            }
        }

        //a method called when the network address' input field value changes
        public void OnAddressInputValueChange()
        {
            manager.networkAddress = addressInputField.text;
        }

        //a method called when the port input field value changes
        public void OnPortInputValueChange()
        {
            transport.port = ushort.Parse(portInputField.text);
        }

        //a method used to change the multiplayer's menu
        public void EnableMenu(MultiplayerMenu targetMenu)
        {
            if (manager.InGame) //if we're already in the game
                return; //menu has been already destroyed, do not continue

            mainMPMenu.gameObject.SetActive(targetMenu == MultiplayerMenu.main);
            loadingMenu.gameObject.SetActive(targetMenu == MultiplayerMenu.loading);
            lobbyMenu.gameObject.SetActive(targetMenu == MultiplayerMenu.lobby);
            mapMenu.gameObject.SetActive(targetMenu == MultiplayerMenu.lobby);

            currentMenu = targetMenu;

            if (currentMenu == MultiplayerMenu.main) //if we're back to the main menu
            {
                manager.IsHost = false;  //if player was hosting, then reset that.
                addressInputField.text = manager.networkAddress; //display the current address and port
                portInputField.text = transport.port.ToString();
            }

            if (currentMenu == MultiplayerMenu.lobby) //if we're currently in the lobby
                lobbyName.text = manager.networkAddress + ":" + transport.port; //show the lobby's address as the name
        }

        public void StartHost()
        {
            ShowInfoMessage("Starting host...");
            EnableMenu(MultiplayerMenu.loading);
            manager.StartHost();
        }

        public void StartClient()
        {
            ShowInfoMessage("Connecting to lobby...");
            EnableMenu(MultiplayerMenu.loading);
            manager.StartClient();
        }

        public void Back()
        {
            switch(currentMenu)
            {
                case MultiplayerMenu.main: //if we're in the main multiplayer menu
                    LoadMainMenu();
                    break;

                case MultiplayerMenu.loading: //if we're in the loading menu then connection to a lobby was in progress
                    manager.LastDisconnectionType = DisconnectionType.abort; //player aborts connetion
                    manager.LeaveLobby (); //leave the lobby
                    break;

                case MultiplayerMenu.lobby: //and if we were in a game lobby
                    manager.LastDisconnectionType = DisconnectionType.left; //player leaves lobby
                    manager.LeaveLobby(); //leave the lobby
                    break;
            }
        }

        //load the main menu
        public void LoadMainMenu ()
        {
            Destroy(gameObject); //destroy the object that has the network lobby manager components
            SceneManager.LoadScene(mainMenuScene); //move to the main menu scene
        }

        //a method that starts the game from the lobby
        public void StartGame() 
        {
            if (currentMenu != MultiplayerMenu.lobby || manager.IsHost == false) //if this hasn't been called from the lobby menu or this is not the host
                return; //do not proceed.

            //to start the game, all players in lobby must be ready. by default the host is never ready until all the rest of the players are ready
            //so first, we need to check if all players (excluding the host) are ready:

            int minPlayers = manager.GetMap(manager.CurrentMapID).GetMinFactions();
            if(manager.numPlayers < minPlayers) //make sure the min factions amount is met.
            {
                ShowInfoMessage($"Can't start the game with {manager.numPlayers} player(s)!");
                return;
            }

            NetworkLobbyManager_Mirror.LobbyFactions.RemoveAll(lobbyFaction => lobbyFaction == null); //remove all null elements.

            if (manager.allPlayersReady) //if all players are ready
                NetworkLobbyManager_Mirror.LocalLobbyFaction.OnStartGame(); //host will let the players know that the game is starting

            else
                ShowInfoMessage("Not all players are ready!");
        }

        //a method called when the player quits a multiplayer game back to the multiplayer lobby menu
        public void Enable()
        {
            EnableMenu(MultiplayerMenu.main);
            mainCanvas.gameObject.SetActive(true); //enable the main UI canvas.
        }

        //a method called when the game map/scene is loaded and the network lobby UI needs to be hidden
        public void Disable ()
        {
            mainCanvas.gameObject.SetActive(false); //disable the main UI canvas.
            playerLobbyObjectsParent.DetachChildren(); //detach all children (which are the objects holding the network lobby faction components).
            //Destroy(mainCanvas.gameObject); //destroy the main canvas
        }
    }
}