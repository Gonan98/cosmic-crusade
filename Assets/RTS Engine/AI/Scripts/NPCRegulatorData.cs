﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class NPCRegulatorData : ScriptableObject
    {
        //minimum amount of item type (NPC would try to have this minimum amount of the item type created urgently).
        [SerializeField, Tooltip("Minimum amount of instances to create.")]
        private IntRange minAmountRange = new IntRange(1, 2);
        public int GetMinAmount() { return minAmountRange.getRandomValue(); }

        //maximum amount of item type (can not have more than the amount below).
        [SerializeField, Tooltip("Maximum amount of instances to be created.")]
        private IntRange maxAmountRange = new IntRange(10, 15);
        public int GetMaxAmount () { return maxAmountRange.getRandomValue(); }

        [SerializeField, Tooltip("Maximum amount of instances that can be pending creation at the same time.")]
        private IntRange maxPendingAmount = new IntRange(1,2); //the amount of items that can be pending creation at the same time.
        public int GetMaxPendingAmount () { return maxPendingAmount.getRandomValue(); }

        //when another NPC component (excluding the main component that can create it) requests the creation of this item type, can it be created?
        [SerializeField, Tooltip("Can NPC Components (except the NPCUnitCreator) request to create this?")]
        private bool createOnDemand = true;
        public bool CanCreateOnDemand () { return createOnDemand; }

        [SerializeField, Tooltip("When should the NPC start creating the first instance after the game starts?")]
        private FloatRange startCreatingAfter = new FloatRange(10.0f, 15.0f); //delay time in seconds after which this component will start creating items.
        public float GetCreationDelayTime () { return startCreatingAfter.getRandomValue(); }

        [SerializeField, Tooltip("Time required between spawning two consecutives instances.")]
        private FloatRange spawnReloadRange = new FloatRange(15.0f, 20.0f); //time needed between spawning two consecutive items.
        public float GetSpawnReload () { return spawnReloadRange.getRandomValue(); }

        [SerializeField, Tooltip("Automatically create instances when requirements are met?")]
        private bool autoCreate = true; //Automatically create this unit type to meet the ratio requirements.
        //whether Auto Create is true or false, the minimum amount chosen above must be met.
        public bool CanAutoCreate () { return autoCreate; }
    }
}
