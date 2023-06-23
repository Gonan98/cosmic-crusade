using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/* Attack Weapon script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    public class AttackWeapon
    {
        [SerializeField]
        private Transform weaponObject = null; //When assigned (must be child of the attack entity main object), this object will be rotated depending on the attack target's position.
        public Transform GetWeaponObject() { return weaponObject; }
        public void Toggle (bool enable) {
            if(weaponObject != null)
                weaponObject.gameObject.SetActive(enable); }

        //freeze the rotation on one or more of the axis
        [SerializeField]
        private bool freezeRotationX = false;
        [SerializeField]
        private bool freezeRotationY = false;
        [SerializeField]
        private bool freezeRotationZ = false;

        [SerializeField]
        private bool smoothRotation = true; //allow smooth rotation?
        [SerializeField, Tooltip("Only rotate the weapon when the target is inside the attacking range?")]
        private bool rotateInRangeOnly = false; //rotate the weapon only when the attack entity is in the attacking range regarding its target?
        [SerializeField]
        private float rotationDamping = 2.0f; //rotation damping (when smooth rotation is enabled)

        [SerializeField]
        private bool forceIdleRotation = true; //when the attacker is in idle mode and this is true, the weapon obj will rotate back to its idle rotation 
        [SerializeField]
        private Vector3 idleAngles = Vector3.zero; //angles of the weapon in idle mode
        private Quaternion idleRotation = Quaternion.identity; //will be used to rotate the weapon rotation


        [SerializeField]
        private float rotationCorrection = 0.0f;

        //init the attack weapon component
        public void Init ()
        {
            if (weaponObject != null)
                idleRotation.eulerAngles = idleAngles;
        }

        [PunRPC]
        protected virtual void RPC_UpdateIdleRotation()
        {
            if (weaponObject == null || forceIdleRotation == false || smoothRotation == false)
                return;

            weaponObject.localRotation = Quaternion.Slerp(weaponObject.localRotation, idleRotation, Time.deltaTime * rotationDamping);
        }

        [PunRPC]
        public void RPC_SetIdleRotation() {
            if (weaponObject == null || forceIdleRotation == false || smoothRotation == true)
                return;

            weaponObject.localRotation = idleRotation;
        }

        [PunRPC]
        public void RPC_UpdateActiveRotation (Vector3 targetPosition, bool inAttackRange)
        {
            if (weaponObject == null
                || (rotateInRangeOnly && !inAttackRange))
                return;

            Vector3 lookAt = targetPosition - weaponObject.position;
            //which axis should not be rotated? 
            if (freezeRotationX == true)
                lookAt.x = 0.0f;
            if (freezeRotationY == true)
                lookAt.y = 0.0f;
            if (freezeRotationZ == true)
                lookAt.z = 0.0f;
            Quaternion targetRotation = Quaternion.LookRotation(lookAt);
            targetRotation *= Quaternion.Euler(0, rotationCorrection, 0); 
            if (smoothRotation == false) //make the weapon instantly look at target
                weaponObject.rotation = targetRotation;
            else //smooth rotation
                weaponObject.rotation = Quaternion.Slerp(weaponObject.rotation, targetRotation, Time.deltaTime * rotationDamping);
        }

        //updates the idle rotation (in case there's smooth rotation)
        public void UpdateIdleRotation ()
        {
            //base.photonView.RPC("RPC_UpdateIdleRotation", RpcTarget.Others);
            if (weaponObject == null || forceIdleRotation == false || smoothRotation == false)
                return;

            weaponObject.localRotation = Quaternion.Slerp(weaponObject.localRotation, idleRotation, Time.deltaTime * rotationDamping);
        }

        //set the idle rotation (in case there's no smooth rotation)
        public void SetIdleRotation() {
            //base.photonView.RPC("RPC_SetIdleRotation", RpcTarget.Others);
            if (weaponObject == null || forceIdleRotation == false || smoothRotation == true)
                return;

            weaponObject.localRotation = idleRotation;
        }

        //update the weapon rotation on idle:
        public void UpdateActiveRotation (Vector3 targetPosition, bool inAttackRange)
        {
            //base.photonView.RPC("RPC_UpdateActiveRotation", RpcTarget.Others);
            if (weaponObject == null
                || (rotateInRangeOnly && !inAttackRange))
                return;

            Vector3 lookAt = targetPosition - weaponObject.position;
            //which axis should not be rotated? 
            if (freezeRotationX == true)
                lookAt.x = 0.0f;
            if (freezeRotationY == true)
                lookAt.y = 0.0f;
            if (freezeRotationZ == true)
                lookAt.z = 0.0f;
            Quaternion targetRotation = Quaternion.LookRotation(lookAt);
            targetRotation *= Quaternion.Euler(0, rotationCorrection, 0); 
            if (smoothRotation == false) //make the weapon instantly look at target
                weaponObject.rotation = targetRotation;
            else //smooth rotation
                weaponObject.rotation = Quaternion.Slerp(weaponObject.rotation, targetRotation, Time.deltaTime * rotationDamping);
        }
    }
}
