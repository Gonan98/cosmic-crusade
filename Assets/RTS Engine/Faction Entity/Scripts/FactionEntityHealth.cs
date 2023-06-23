using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    //this requires a Faction Entity component
    [RequireComponent(typeof(FactionEntity))]
    public abstract class FactionEntityHealth : MonoBehaviourPun
    {
        private FactionEntity factionEntity; //the faction's entity component

        //[SerializeField]
        //private Image barHealth;

        //health settings:

        //maximum health points of the faction entity
        [SerializeField]
        private int maxHealth = 100;
        //the maximum health must always be > 0.0
        public int MaxHealth
        {
            set
            {
                if (value > 0.0)
                    maxHealth = value;
            }
            get
            {
                return maxHealth;
            }
        }

        //current health points of the faction entity
        public int CurrHealth { set; get; }

        [SerializeField]
        private float hoverHealthBarY = 4.0f; //this is the height of the hover health bar (in case the feature is enabled).
        public float GetHoverHealthBarY() { return hoverHealthBarY; }
        protected bool isDead = false; //is this faction entity dead? 
        public bool IsDead() { return isDead; }
        public FactionEntity KilledBy { private set; get; } //when the faction entity is dead, this holds the faction entity that caused it if it exists

        //Damage settings:
        [SerializeField]
        private bool canBeAttacked = true; //can this faction entity be attacked?
        public bool CanBeAttacked
        {
            set
            {
                canBeAttacked = value;
            }
            get
            {
                return canBeAttacked;
            }
        }

        [SerializeField]
        private bool takeDamage = true; //does this faction entity lose damage when it is attacked?
        public bool CanTakeDamage() { return takeDamage; }
        [SerializeField]
        private EffectObj damageEffect = null; //appears when a damage is received in the contact point between the attack object and this faction entity
        public EffectObj GetDamageEffect() { return damageEffect; }

        //Destruction settings:
        public bool IsDestroyed { set; get; } //is the faction entity destroyed?
        protected int killerFactionID = -1; //the faction ID of the killer/destroyer of this faction entity
        [SerializeField]
        private bool destroyObject = true; //destroy the object on destruction?
        [SerializeField]
        private float destroyObjectTime = 0.0f; //if the above bool is set to true, this is the time it will take to destroy the object.
        [SerializeField]
        private ResourceInput[] destroyAward = new ResourceInput[0]; //these resources will be awarded to the faction that destroyed this faction entity

        //Destruction effects:
        [SerializeField, Tooltip("What audio clip to play when the faction entity is destroyed?")]
        private AudioClipFetcher destructionAudio = new AudioClipFetcher(); //audio played when the faction entity is destroyed.
        [SerializeField]
        private EffectObj destructionEffect = null; //this effect will be shown on the faction entity's position when it is destroyed.

        //other components:
        public List<DamageOverTime> DotComps { set; get; }

        protected GameManager gameMgr;

        public virtual void Init(GameManager gameMgr, FactionEntity source)
        {
            this.gameMgr = gameMgr;
            factionEntity = source; //get the Faction Entity component.
            //initial settings:
            isDead = false; //faction entity is not dead by default.

            DotComps = new List<DamageOverTime>();
        }

        //add/remove health to the faction entity's:
        public void AddHealth(int value, FactionEntity source)
        {
            if (GameManager.MultiplayerGame == false) //if it's a single player game
            {
                AddHealthLocal(value, source); //add the health directly
            }
            else //multiplayer game
            {
                if (RTSHelper.IsLocalPlayer(factionEntity)) //make sure that it's this faction entity belongs to the local player
                {
                    //crete new input
                    NetworkInput newInput = new NetworkInput
                    {
                        sourceMode = (byte)InputMode.factionEntity,
                        targetMode = (byte)InputMode.health,
                        value = value
                    };
                    //send the input to the input manager
                    InputManager.SendInput(newInput, factionEntity, source);
                }
            }
        }

        //add health to the faction entity locally
        public void AddHealthLocal(int value, FactionEntity source)
        {
            //if the faction entity doesn't take damage and the health points to add is negative (damage):
            if (takeDamage == false && value < 0.0f)
                return; //don't proceed.

            CurrHealth += value; //add the input value to the current health value
            base.photonView.RPC("RPC_UpdateCurrHealth", RpcTarget.Others, value);
            Canvas canvas = gameObject?.GetComponentInChildren<Canvas>(true);
            Image barHealth = canvas?.gameObject?.GetComponentsInChildren<Image>(true)[1];

            //Debug.Log("LA BARRA ES " + barHealth == null);
            if (barHealth != null)
            {
                //Debug.Log("LA BARRA ES LAMADA: " + barHealth.name);
                //Debug.Log(barHealth.fillAmount);
                //Debug.Log(CurrHealth);
                //Debug.Log(MaxHealth);
                barHealth.fillAmount = ((float)CurrHealth / (float)MaxHealth);
            }
            if (CurrHealth >= MaxHealth) //if the current health is above the maximum allowed health
                OnMaxHealthReached(value, source);

            if (CurrHealth <= 0.0f && isDead == false)
                OnZeroHealth(value, source);
            //the faction entity isn't "dead"
            else
                OnHealthUpdated(value, source);
        }

        //a method called when the faction entity reaches max health:
        public virtual void OnMaxHealthReached(int value, FactionEntity source)
        {
            CurrHealth = MaxHealth;
        }

        //a method called when the faction entity's health hits null:
        public virtual void OnZeroHealth(int value, FactionEntity source)
        {
            //set it back to 0.0f as we don't allow negative health values.
            CurrHealth = 0;

            if (isDead)
                return;

            isDead = true; //mark as dead
            KilledBy = source; //assign the source that caused the death of this faction entity

            factionEntity.Interactable = false; //no longer interactable.

            //is there a valid source that caused the death of this faction entity?
            if (source != null && !source.IsFree())
                //award the destroy award to the source if the source is not the same faction ID:
                if (destroyAward.Length > 0 && source.FactionID != factionEntity.FactionID)
                    for (int i = 0; i < destroyAward.Length; i++)
                        //award destroy resources to source:
                        gameMgr.ResourceMgr.UpdateResource(source.FactionID, destroyAward[i].Name, destroyAward[i].Amount);


            //destroy the faction entity
            DestroyFactionEntity(false);
        }

        //a method called when the faction entity's health has been updated:
        public virtual void OnHealthUpdated(int value, FactionEntity source)
        {
            if (value < 0.0)
            {
                //if this is the local player's faction ID and the attack warning manager is active:
                if (factionEntity.FactionID == GameManager.PlayerFactionID)
                    gameMgr.AttackWarningMgr?.Add(transform.position); //show attack warning on minimap
            }

            CustomEvents.OnFactionEntityHealthUpdated(factionEntity, value, source); //trigger custom event
        }

        //a method called to destroy the faction entity:
        public void DestroyFactionEntity(bool upgrade)
        {
            //if (GameManager.MultiplayerGame == false) //if it's a single player game
            DestroyFactionEntityLocal(upgrade); //destroy faction entity directly
            base.photonView.RPC("RPC_DestroyFactionEntityLocal", RpcTarget.Others, upgrade);
            /*else //multiplayer game
            {
                if (RTSHelper.IsLocalPlayer(factionEntity)) //make sure that it's this faction entity belongs to the local player
                {
                    //send input action to the input manager
                    NetworkInput NewInputAction = new NetworkInput
                    {
                        sourceMode = (byte)InputMode.destroy,
                        targetMode = (byte)InputMode.factionEntity,
                        value = (upgrade == true) ? 1 : 0 //when upgrade == true, then set to 1. if not set to 0
                    };
                    InputManager.SendInput(NewInputAction, factionEntity, null); //send to input manager
                }
            }*/
        }

        [PunRPC]
        protected virtual void RPC_DestroyFactionEntityLocal(bool upgrade)
        {
            DestroyFactionEntityLocal(upgrade);
        }

        //a method that destroys a faction entity locally
        public virtual bool DestroyFactionEntityLocal(bool upgrade)
        {
            Debug.Log("DESTROY BY BUTTON");
            if (IsDestroyed) //unit already dead?
                return false;

            gameMgr.SelectionMgr.Selected.Remove(factionEntity); //deselect the faction entity if it was selected

            //faction entity death:
            IsDestroyed = true;
            isDead = true;
            CurrHealth = 0;

            //remove the minimap icon:
            factionEntity.GetSelection().DisableMinimapIcon();

            factionEntity.Disable(!upgrade);

            if (destroyObject || upgrade)
            {
                Debug.Log("SE HA ENTRADO AL METODO D DESTRUCCIOM");
                //Destroy the faction entity's object:
                if (destroyObject)
                {
                    Destroy(gameObject, destroyObjectTime);
                    //StartCoroutine(Die(destroyObjectTime));
                }

                if (upgrade)
                {
                    Destroy(gameObject, !upgrade ? destroyObjectTime : 0.0f);
                    //StartCoroutine(Die(0));
                }
            }

            //If this is no upgrade
            if (!upgrade)
            {
                CustomEvents.OnFactionEntityDead(factionEntity); //call the custom events
                if (destructionEffect != null) //do not show desctruction effect if it's not even assigned
                {
                    //get the destruction effect from the pool
                    EffectObj newDestructionEffect = gameMgr.EffectPool.SpawnEffectObj(destructionEffect, transform.position, Quaternion.identity);
                    gameMgr.AudioMgr.PlaySFX(newDestructionEffect.AudioSourceComp, destructionAudio.Fetch(), false); //play the destruction audio
                }
            }

            return true;
        }



        //And function itself
        IEnumerator Die(float destroyTime)
        {
            //play your sound
            yield return new WaitForSeconds(destroyTime); //waits 3 seconds
            Destroy(gameObject); //this will work after 3 seconds.
        }

        #region IPunObservable

        /*public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
		{
			//info.photonView.gameObject.
			//if (info.photonView.IsMine) return;
			Debug.Log("OnPhotonSerializeView");
			if (stream.IsWriting)
			{
				// We own this player: send the others our data
				Debug.Log("Enviando CurrHealth");
				stream.SendNext(CurrHealth);
			}
			else
			{
				// Network player, receive data
				Debug.Log("CurrHealth" + CurrHealth.ToString());
				Debug.Log("(int)stream.ReceiveNext()" + (int)stream.ReceiveNext());
				CurrHealth = (int)stream.ReceiveNext();
			}
		}
        */
        #endregion


        [PunRPC]
        protected virtual void RPC_UpdateCurrHealth(int value)
        {
            //Debug.Log("HE ENTRADO A RPC_UpdateCurrHealth");
            this.CurrHealth += value;
            //Debug.Log("ANTES TRIGGER " + CurrHealth);
            CustomEvents.OnFactionEntityHealthUpdated(factionEntity, value, factionEntity); //trigger custom event
                                                                                            //Debug.Log("DESPUES TRIGGER " + CurrHealth);
            Canvas canvas = gameObject?.GetComponentInChildren<Canvas>(true);
            Image barHealth = canvas?.gameObject?.GetComponentsInChildren<Image>(true)[1];
            //Debug.Log(barHealth?.fillAmount);
            if (barHealth != null)
                barHealth.fillAmount = ((float)CurrHealth / (float)MaxHealth);

            if (CurrHealth >= MaxHealth) //if the current health is above the maximum allowed health
                OnMaxHealthReached(value, factionEntity);

            if (CurrHealth <= 0.0f && isDead == false)
                OnZeroHealth(value, factionEntity);
            //the faction entity isn't "dead"
            else
                OnHealthUpdated(value, factionEntity);
        }
    }
}
