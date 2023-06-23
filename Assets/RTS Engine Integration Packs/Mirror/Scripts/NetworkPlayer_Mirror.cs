using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Network Player (Mirror): script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine 
 * Each player in a multiplayer game has an active Network Faction instance that the server uses to monitor the player.
 */

namespace RTSEngine
{
    public class NetworkPlayer_Mirror
    {
        public int FactionID { private set; get; } //Faction ID associated to this client
        public int LastSyncedTurn { private set; get; } //the client's last reported synced turn

        public bool Disconnected { set; get; } //is the client connected or not.
        public bool TimingOut { private set; get; } //is the client timing out? 
        public float kickTimer; //time before the client gets kicked for timing out.

        GameManager gameMgr;

        public NetworkPlayer_Mirror(GameManager gameMgr, int factionID) //constructor
        {
            this.gameMgr = gameMgr;

            this.FactionID = factionID;
            LastSyncedTurn = 0;

            Disconnected = false;
            TimingOut = false;
            kickTimer = 0.0f;
        }

        public void IncrementSyncedTurn() //increment the synced turn on this network client
        {
            LastSyncedTurn++;
        }

        public void TriggerTimeOut(bool enable, float duration = 5.0f) //starts the kicking out timer as the client is marked as timing out
        {
            if (enable == true)
            {
                kickTimer = duration;
                TimingOut = true;
            }
            else
                TimingOut = false;
        }

        public void OnTimeOutUpdate() //updating the client in case of a time out
        {
            if (Disconnected == true || TimingOut == false) //if the client is either already disconnected or not in a time out
                return; //do not continue

            kickTimer -= NetworkFactionManager_Mirror.DeltaTime; //keep the kicking timer going
            if (kickTimer <= 0.0f) //if the timer is over, then the player will be kicked
            {
                if(gameMgr.GetFaction(FactionID).FactionManager_Mirror) //if there's a network faction manager still
                    gameMgr.GetFaction(FactionID).FactionManager_Mirror.connectionToClient.Disconnect(); //disconnect it
                else //no network faction manager?
                {
                    //inform other players that this client has left:
                    NetworkInput newInput = new NetworkInput()
                    {
                        sourceMode = (byte)InputMode.destroy,
                        targetMode = (byte)InputMode.faction,
                        value = FactionID
                    };

                    InputManager.SendInput(newInput, null, null); //send the input to destroy this faction
                }

                Disconnected = true;
            }
        }
    }
}
