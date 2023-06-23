using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/* Network Faction Manager (Mirror): script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine 
 * Note: All RPC client methods call commands from within the same component, the server then calls the appropriate methods on the host's component.
 */

namespace RTSEngine
{
    public class NetworkFactionManager_Mirror : NetworkBehaviour
    {
        public static NetworkFactionManager_Mirror HostFactionMgr { private set; get; }

        //host_only_attributes:
        //the lockstep cycle represents the time at which the host sends collected input commands to other players.
        [SerializeField]
        private float lockStepCycle = 0.2f; //the cycle's length, at which the player can send inputs to the host.
        private float cycleTimer;
        public static float DeltaTime { private set; get; } //can't always rely on Time.deltaTime since we need to keep the same rate even when the game is frozen.

        private const float frozenTimeScale = 0.000001f; //the time scale when the multiplayer game is frozen.

        private List<NetworkInput> inputs = new List<NetworkInput>(); //collected inputs are stored here.
        public void AddInput(NetworkInput input) { inputs.Add(input); } //adds an input message to the list

        private int readyPlayers = 0; //amount of players who have loaded the map scene and are ready to start the game, all players must successfully load the map scene before the game actually starts
        private bool allPlayersReady = false; //when all players are ready, this is true and that's when players are allowed to send their inputs to the server/host

        public static int ServerTurn { private set; get; } //a counter how many lockstep cycle have passed in the server

        public static List<NetworkPlayer_Mirror> NetworkPlayers { private set; get; } //this holds a list of all players that have successfully loaded the game scene/map and ready to play
        public void AddNetworkClient(NetworkPlayer_Mirror newPlayer) //add new player
        {
            if (isServer == false || GameManager.HostFactionID != GameManager.PlayerFactionID) //make sure that this is the server
                return;
            NetworkFactionManager_Mirror.NetworkPlayers.Add(newPlayer);
        }

        [SerializeField]
        private float timeOutDuration = 5.0f; //if clients don't respond within this time, they will be kicked.
        public float GetTimeOutDuration() { return timeOutDuration; }

        [SerializeField]
        private SyncTest_Mirror syncTest = new SyncTest_Mirror();

        //client_related_attributes:
        [SyncVar]
        private bool initialized = false; //has this component been initialized?

        private int currentTurn; //a counter for how many lockstep cycles have passed in the client.
        private int receivedCommands = 0; //how many commands have been received by the client in the last lockstep turn.

        private bool isFactionSpawned = false; //has the faction associated with this manager spawned or not?

        NetworkLobbyManager_Mirror manager;
        GameManager gameMgr;

        [Command]
        public void CmdInit(int factionID)
        {
            //TO BE CHANGED.
        }

        //after CmdInit is called, this method will be called to initialize this component
        private void Start()
        {
            this.gameMgr = FindObjectOfType(typeof(GameManager)) as GameManager; //get the game manager component
            this.manager = FindObjectOfType(typeof(NetworkLobbyManager_Mirror)) as NetworkLobbyManager_Mirror;

            if (isLocalPlayer)
            {
                gameMgr.GetFaction(GameManager.PlayerFactionID).FactionManager_Mirror = this;

                if (GameManager.HostFactionID == GameManager.PlayerFactionID) //if this is the host
                {
                    HostFactionMgr = this;
                    HostFactionMgr.gameObject.name = "this_host";
                    NetworkPlayers = new List<NetworkPlayer_Mirror>(); //init the network players list

                    syncTest.Init(); //initialize the sync test instance
                }
            }

            isFactionSpawned = false; //by default faction is marked as not spawned, yet.

            receivedCommands = 0;
            currentTurn = 0;
            ServerTurn = 0;

            if (isServer) //if this component is initiated on the server's side
                gameMgr.GetFaction(GameManager.PlayerFactionID).ConnID_Mirror = connectionToClient.connectionId; //set the unique connection ID field for the host/server (will be used to check for clients that disconnect during the game).

            initialized = true;
        }

        void Update()
        {
            if (!isLocalPlayer || !initialized) //if this is not the local player
                return; //do not proceed
            //if the game manager hasn't been initialized yet, the faction here hasn't been initialized in the multiplayer game and client scene is ready 
            //=> the player successfully entered the game map/scene and the server/host will be asked to mark it as initialized
            //this component must have been initialized before the faction associate to it can be spawned
            if (isFactionSpawned == false && gameMgr.Initialized == true && ClientScene.ready == true)
            {
                InputManager.FactionManager_Mirror = this; //assign the local player's mirror's faction manager to the input manager which will handle communications between the mirror network components and the rest of the game components.

                manager.OnGameMapLoaded(); //trigger this event on the manager.
                isFactionSpawned = true; //faction is now marked as spawned.
                CmdSendGameSceneReadyMessage(GameManager.PlayerFactionID); //let the server know that this faction is ready
            }

            if (isServer == false || allPlayersReady == false) //if this is not the server or not all players are ready 
                return; //do not proceed.

            DeltaTime = Time.deltaTime; //if the multiplayer game is frozen, we'd like to have the same delta time for the following as in a normal game.
            if (GameManager.GameState == GameState.frozen)
                DeltaTime *= 1 / frozenTimeScale;

            if (syncTest.AllClientsSynced == true) //if all clients are synced in correctly:
                OnServerSyncedUpdate();
            else //at least one client is not synced in
                OnServerUnsyncedUpdate();

            syncTest.Update(); //update the sync test.
        }

        //called only on the server to update the component when all clients are synced
        private void OnServerSyncedUpdate()
        {
            cycleTimer += DeltaTime; //update the lockstep cycle timer
            if (cycleTimer > lockStepCycle) //when a lockstep cycle is done
            {
                cycleTimer -= lockStepCycle; //reset the input cycle's timer:
                ServerTurn++; //increase the game turn on the server amount.

                int inputsSize = inputs.Count; //in order to avoid breaking the enumeration when sending the inputs, we won't be using a foreach loop
                for (int i = 0; i < inputsSize; i++)
                { //go through all collected inputs
                    RpcSendCommand(inputs[i]); //send the input as a command to all clients
                }
                RpcOnCommandsReceived(inputsSize); //this checks if all commands have been received by clients
                inputs.RemoveRange(0, inputsSize); //remove the sent inputs.

                if (syncTest.IsEnabled == false) //if the sync test was disabled
                    syncTest.OnEnableAttempt(); //attempt enabling it.
            }
        }

        //called only on the server to update the component when not all clients are in sync
        private void OnServerUnsyncedUpdate()
        {
            foreach (NetworkPlayer_Mirror player in NetworkPlayers) //go through all clients
                player.OnTimeOutUpdate(); //update timing out for each client
        }

        //clients call this method to let the server/host know that they're ready:
        [Command]
        public void CmdSendGameSceneReadyMessage(int readyFactionID)
        {
            NetworkPlayer_Mirror newPlayer = new NetworkPlayer_Mirror(gameMgr, readyFactionID); //create new instance of the Network Client class for the client that just got the game scene ready
            HostFactionMgr.AddNetworkClient(newPlayer); //register the client on the ready clients' list on the server/host's component

            HostFactionMgr.OnClientGameSceneReady(readyFactionID); //inform the server/host that one of the player's scene is now ready
        }

        //whenever a client's game scene is ready, this method is called on the host's component:
        public void OnClientGameSceneReady(int readyFactionID)
        {
            readyPlayers++;
            if (readyPlayers == gameMgr.GetFactionCount()) //if all registered players are ready.
            {
                allPlayersReady = true; //this will allow the server to start sending input commands.
                syncTest.OnAllClientsReady(); //all clients are now ready
            }
        }

        //clients use this command to send input to the server/host:
        [Command]
        public void CmdSendInput(NetworkInput input) //server/host rec
        {
            HostFactionMgr.AddInput(input); //add the received input
        }

        //called when the server sends a collected input (which is now a command) to clients
        [ClientRpc]
        public void RpcSendCommand(NetworkInput input)
        {
            receivedCommands++; //increment the amount of received commands.
            gameMgr.InputMgr.LaunchCommand(input); //the input manager will now execute this command.
        }

        //after the server sends commands to clients, this method is called by the server on all clients
        [ClientRpc]
        public void RpcOnCommandsReceived(int commandsCount)
        {
            if (commandsCount == receivedCommands) //if the commands count matches with the amount of received commands
            {
                currentTurn++; //increment the client's current lockstep turn
                //in return each client sends a message to the server informing him the commands have been successfully received:
                gameMgr.GetFaction(GameManager.PlayerFactionID).FactionManager_Mirror.CmdReportSuccessfulTurn(GameManager.PlayerFactionID); //report the successful turn back to the server
                receivedCommands = 0;
            }
            else
                CmdReportFailedTurn();
        }

        //triggered by the client in the server when the client successfully receives all commands in a turn
        [Command]
        public void CmdReportSuccessfulTurn(int index)
        {
            foreach (NetworkPlayer_Mirror player in NetworkPlayers) //go through all network clients
                if (player.FactionID == index) //if the faction ID matches
                {
                    player.IncrementSyncedTurn(); //increment the client's turn in the network's client list

                    if (syncTest.AllClientsSynced == false) //if a client reports a successful turn when the game was frozen and not all clients were synced
                        syncTest.Execute(); //execute a sync test because this might the client that was lacking behind.

                    return;
                }
        }

        //triggered by the client in the server when the client fails to receive all commands in a turn
        [Command]
        public void CmdReportFailedTurn()
        {
            //Work in progress.
        }

        //called on the server when a faction is defeated:
        public void OnFactionDefeated(int factionID)
        {
            foreach (NetworkPlayer_Mirror player in NetworkPlayers) //go through all network players
                if (player.FactionID == factionID) //if the faction ID matches
                {
                    player.Disconnected = true; //mark the player as disconnected

                    //if the game was frozen:
                    if (GameManager.GameState == GameState.frozen)
                        syncTest.Execute(); //execute sync test

                    return;
                }
        }

        //freeze/unfreeze the game
        [ClientRpc]
        public void RpcFreezeGame(bool freeze)
        {
            GameManager.SetGameState((freeze == true) ? GameState.frozen : GameState.running); //set the game state
            Time.timeScale = (freeze == false) ? 1.0f : frozenTimeScale; //set the time scale
            CustomEvents.OnGameStateUpdated();
        }
    }
}



