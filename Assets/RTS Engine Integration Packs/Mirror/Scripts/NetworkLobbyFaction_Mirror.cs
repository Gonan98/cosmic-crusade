using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

/* Network Lobby Manager (Mirror): script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class NetworkLobbyFaction_Mirror : NetworkRoomPlayer
    {
        [SyncVar]
        private bool isHost = false;
        public bool IsHost
        {
            get { return isHost; }
            private set { isHost = value; }
        }

        [SyncVar]
        private string factionName = "faction_name"; //holds the player's faction name.
        public string GetFactionName() { return factionName; }

        [SyncVar]
        private int factionTypeID = 0; //holds the player's faction type ID
        public int GetFactionTypeID() { return factionTypeID; }

        [SyncVar]
        private int factionColorID = 0; //the color ID of the faction
        public int GetFactionColorID() { return factionColorID; }

        //UI attributes:
        [SerializeField]
        private Image factionColorImage = null; //showing the faction's color
        [SerializeField]
        private InputField factionNameInput = null; //input/show the faction's name
        [SerializeField]
        private Button readyToBeginButton = null; //to announce that the player is to ready or not
        [SerializeField]
        private Image readyImage = null; //the image to show when the player is ready
        [SerializeField]
        private Dropdown factionTypeMenu = null; //UI Dropdown used to display the list of possible faction types that can be used in the currently selected maps.
        [SerializeField]
        private Button kickButton = null; //the button that the host can use to kick this player.

        NetworkLobbyManager_Mirror manager;

        private bool isInitiated = false;

        public override void OnClientEnterRoom()
        {
            base.OnClientEnterRoom();

            if (isInitiated)
                return;

            manager = (NetworkLobbyManager_Mirror)NetworkLobbyManager_Mirror.singleton; //get the network lobby manager component

            if (manager == null)
                return;

            IsHost = false; //initially marked as non-host.

            manager.UIMgr.SetLobbyFactionParent(transform);

            manager.UIMgr.ToggleMapSettings();

            ResetFactionTypeDropDownMenu(); //so that the faction type drop down menu contains the correct map settings.

            if (isLocalPlayer == true) //if this is the local player (player who owns this network lobby faction)
            {
                InitLocal();
            }
            else
                InitOther();

            NetworkLobbyManager_Mirror.LobbyFactions.Add(this); //add new instance to lobby faction components list

            isInitiated = true;
        }

        public override void OnClientExitRoom()
        {
            NetworkLobbyManager_Mirror.LobbyFactions.Remove(this); //remove this instance from lobby faction components list

            base.OnClientExitRoom();
        }

        //a method that initializes this component's attribtues if it belongs to the local player
        public void InitLocal()
        {
            if (manager.IsHost == true) //if this is the host (then the host just started the lobby)
            {
                manager.UpdateMap(0,0,0); //host just started the scene, by default have the first map for this lobby.

                IsHost = true; //mark as host.
            }
            else  //if this is not the host, then this means that this is a new player that just joined the room and inited their lobby faction component
            {
                IsHost = false;
                CmdOnHostCheck(manager.GetGameVersion()); //compare the new player's game version with the host's game version
            }

            manager.UIMgr.ToggleStartGameButton(IsHost); //only show the start game button for the host.
            manager.UIMgr.defeatConditionMenu.ToggleInteracting(IsHost); //only allow the host to set the defeat condition menu in the lobby
            manager.UIMgr.speedModifierMenu.ToggleInteracting(IsHost); //only allow the host to set the defeat condition menu in the lobby

            kickButton.gameObject.SetActive(false); //hide the kick button for each local player

            factionNameInput.interactable = true; //only the local player can change his faction's name
            factionTypeMenu.interactable = true; //only the local player can pick his faction type.
            readyToBeginButton.interactable = true; //only the local player can change the ready to begin status

            CmdUpdateFactionInfo("faction_name", 0, 0); //set initial values for faction name, color and type.
            //following two UI elements must be updated for the local player manually because the in the Update() method, they only update for non local players.
            factionNameInput.text = "faction_name"; //update name input
            factionTypeMenu.value = 0; //update the faction's type drop down menu.

            NetworkLobbyManager_Mirror.LocalLobbyFaction = this; //set the local lobby faction component of the local player.
        }

        //a method that initializes this component's attribtues if it does not belong to the local player
        public void InitOther()
        {
            factionNameInput.interactable = false; //can't change its name
            factionTypeMenu.interactable = false; //can't change the faction type.
            readyToBeginButton.interactable = false; //can't change the ready to begin status

            kickButton.gameObject.SetActive(manager.IsHost == true); //only show the kick button for the host.
        }

        //a method called on the server so that the host can check the new player's game version and sync the map
        [Command]
        public void CmdOnHostCheck(string newGameVersion)
        {
            if (newGameVersion != manager.GetGameVersion()) //if the lobby's game version is different from the local player's game version
            {
                CmdKickPlayer(true); //kick the player.
                return; //do not continue
            }

            //sync the map:
            NetworkLobbyManager_Mirror.LocalLobbyFaction.OnMapUpdated(
                manager.CurrentMapID, 
                false, 
                manager.UIMgr.defeatConditionMenu.MenuIndex,
                manager.UIMgr.speedModifierMenu.MenuIndex);
        }

        private void Update()
        {
            factionColorImage.color = manager.FactionColor.Get(factionColorID); //keep showing the updated color.

            readyImage.gameObject.SetActive(readyToBegin); //show/hide the ready image depending on the ready status of the lobby player.

            if (isLocalPlayer == true) //if this is not the local owner of this component then we have to show the updated name and faction type
                return;

            factionNameInput.text = factionName; //update name input
            factionTypeMenu.value = factionTypeID; //update the faction's type drop down menu.
        }

        //a method called to ask the server to sync the updated faction info to all clients.
        [Command]
        public void CmdUpdateFactionInfo (string newName, int newTypeID, int newColorID)
        {
            factionName = newName;
            factionTypeID = newTypeID;
            factionColorID = newColorID;
        }

        public void OnMapUpdated (int newMapID, bool resetFactionTypes, int newDefeatConditionIndex, int newSpeedModifierIndex)
        {
            if(isLocalPlayer == true && manager.IsHost == true) //only the host can change the map
                CmdSyncMap(newMapID, resetFactionTypes, newDefeatConditionIndex, newSpeedModifierIndex);
        }

        [Command]
        public void CmdSyncMap(int newMapID, bool resetFactionTypes, int newDefeatConditionIndex, int newSpeedModifierIndex) //called on the server/host to sync the map settings
        {
            RpcSyncMap(newMapID, resetFactionTypes, newDefeatConditionIndex, newSpeedModifierIndex); //sync the map.
        }

        [ClientRpc]
        public void RpcSyncMap(int newMapID, bool resetFactionTypes, int newDefeatConditionIndex, int newSpeedModifierIndex) //called on client to change the map settings
        {
            manager.UpdateMap(newMapID, newDefeatConditionIndex, newSpeedModifierIndex); //this will update the map settings and UI

            if (resetFactionTypes == false) //do not reset faction types?
                return; //do not proceed

            //resetting faction types by going through all the instances of the network lobby faction and resetting each faction types map drop down menu:
            foreach (NetworkLobbyFaction_Mirror nlf in NetworkLobbyManager_Mirror.LobbyFactions)
                nlf.ResetFactionTypes();
        }

        //called when the local player changes the name of the faction from the text input field
        public void OnFactionNameUpdated()
        {
            if (factionNameInput.text == "" || isLocalPlayer == false) //invalid faction name or not the owner of this faction
            {
                factionNameInput.text = factionName; //reset name
                return; //do not proceed
            }

            CmdUpdateFactionInfo(factionNameInput.text, factionTypeID, factionColorID); //sync the new faction name
        }

        //called when the local player changes the color of the faction using the color image
        public void OnFactionColorUpdated()
        {
            if (isLocalPlayer == false) //only the owner of the faction can change its color
                return;

            //the host/server will update the color for all players:
            CmdUpdateFactionInfo(factionName, factionTypeID, manager.FactionColor.GetNextIndex(factionColorID)); //sync the new faction color
        }

        //called when the local player changes the type of the faction using the faction type drop down menu
        public void OnFactionTypeUpdated()
        {
            if (isLocalPlayer == false) //only the owner of the faction can change its color
                return;

            //the host/server will update the color for all players:
            CmdUpdateFactionInfo(factionName, factionTypeMenu.value, factionColorID); //sync the new faction type
        }

        //reset the faction type drop down menu options depending on the map 
        public void ResetFactionTypeDropDownMenu()
        {
            factionTypeMenu.ClearOptions(); //clear all the faction type options.
            factionTypeMenu.AddOptions(manager.GetMapFactionTypeNames()); //add the names of the faction types of the current map
        }

        //a method that resets the faction types drop down
        public void ResetFactionTypes()
        {
            ResetFactionTypeDropDownMenu();
            factionTypeMenu.value = 0; //reset the faction type to the first one
            factionTypeID = 0;
        }

        //a method to toggle the ready status
        public void ToggleReadyStatus()
        {
            if (isLocalPlayer == false) //if this is not the local player or this is the host
                return; //do not proceed

            CmdChangeReadyState(!readyToBegin);
        }

        //called on the server to kick a player
        [Command]
        public void CmdKickPlayer(bool falseGameVersion)
        {
            RpcKickPlayer(falseGameVersion); //call this method for all clients and make the local client leave the lobby
        }

        [ClientRpc]
        public void RpcKickPlayer(bool falseGameVersion)
        {
            if (isLocalPlayer == false) //if this is not the local player then do not proceed
                return;

            manager.LastDisconnectionType = (falseGameVersion == true) ? DisconnectionType.gameVersion : DisconnectionType.kick; //get the disconnection reason
            manager.LeaveLobby(); //make the local player leave the lobby
        }

        //called by the server when the game is about to start:
        public void OnStartGame ()
        {
            if (isLocalPlayer == false || manager.IsHost == false) //the host must be the only one able to start the game
                return;

            List<int> factionSlots = RTSHelper.GenerateIndexList(manager.maxConnections);
            RTSHelper.ShuffleList<int>(factionSlots); //randomize the faction slots list IDs by shuffling it

            RpcOnGameStart(factionSlots.ToArray()); //let all clients know that the game is starting
        }

        //triggerd by the host to all clients/players in the lobby when the game starts
        [ClientRpc]
        public void RpcOnGameStart (int[] factionSlots)
        {
            manager.FactionSlotIndexes = factionSlots; //set the faction slots indexes in case the map allows to randomize the faction slots

            manager.UIMgr.ShowInfoMessage("The game is starting..."); //inform the players that the game is starting

            if (manager.IsHost == true) //if this is the host, load the map scene to start the game
                manager.LoadGameMapScene();
        }
    }
}
