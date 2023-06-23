using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Sync Test (Mirror): script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine 
 * This class is used to carry a test by the host to check if all clients in the game are in the same sync level.
 */

namespace RTSEngine
{
    [System.Serializable]
    public class SyncTest_Mirror
    {
        public static int SyncedTurns { private set; get; } //a counter for how many lockstep cycles have been synced successfully to all clients

        [SerializeField]
        private int triggerTrun = 2; //the sync test will only start after the server turn count hits value. (turning the sync test for earlier turns than 2 might cause indesired behavior as most clients have just connected by that time).

        [SerializeField]
        private float reloadTime = 0.6f; //time during which the sync test is carried.
        private float timer;

        public bool IsEnabled { private set; get; } //true hen the sync test is enabled and running
        public bool AllClientsSynced { private set; get; } //the rest of the sync test will influence the value of this bool.

        public void Init() //initialize this component
        {
            IsEnabled = false; //sync test is disabled by default.
            if (triggerTrun < 2) //can't have a trigger turn below 2
                triggerTrun = 2;

            SyncedTurns = 0;
        }

        public void OnAllClientsReady() //enable the sync test
        {
            AllClientsSynced = true; //all clients are synced by default
        }

        public void OnEnableAttempt()
        {
            if (NetworkFactionManager_Mirror.ServerTurn < triggerTrun) //the server needs to reach the trigger turn before 
                return;

            IsEnabled = true; //component is enabled.
            timer = reloadTime; //start the timer.
        }

        public void Update() //update the sync test
        {
            if (IsEnabled == false) //not yet enabled?
                return;

            timer -= NetworkFactionManager_Mirror.DeltaTime;

            if (timer <= 0.0f) //timer is through,
                Execute(); //execute the sync test.
        }

        public void Execute() //executes the sync test
        {
            if (IsEnabled == false) //if the sync test is not available 
                return;

            AllClientsSynced = true; //assume that all clients are synced.

            foreach (NetworkPlayer_Mirror player in NetworkFactionManager_Mirror.NetworkPlayers) //go through the network clients
            {
                if (player.Disconnected == true) //if the client has already disconnected, move on to the next one.
                    continue;

                if (player.LastSyncedTurn < SyncedTurns) //if this client current turn count is smaller or equal to the last reported synced turns
                {
                    //this client is still behind
                    AllClientsSynced = false; //not all clients are synced correctly

                    if (player.TimingOut == false) //if the client is not marked as timing out yet
                        player.TriggerTimeOut(true, NetworkFactionManager_Mirror.HostFactionMgr.GetTimeOutDuration()); //client is now timing out
                }
                else if (player.TimingOut == true) //if the client is not behind but he was marked as timed out
                {
                    player.TriggerTimeOut(false); //disable timing out on the client
                }
            }

            if (AllClientsSynced == (GameManager.GameState == GameState.frozen)) //if there's a change in the sync state.
                NetworkFactionManager_Mirror.HostFactionMgr.RpcFreezeGame(!AllClientsSynced); //freeze/unfreeze the game for all clients depedning on the sync state

            if (AllClientsSynced == true) //if all clients are synced in
                SyncedTurns++; //increase the synced tunrs.

            timer = reloadTime; //reload the timer
        }
    }
}
