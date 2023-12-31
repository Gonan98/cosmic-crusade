﻿using UnityEngine;
using System.Collections.Generic;

using RTSEngine;

[CreateAssetMenu(fileName = "NewFactionType", menuName = "RTS Engine/Faction Type", order = 1)]
public class FactionTypeInfo : ScriptableObject, IAssetFile
{
    [SerializeField]
    private string _name = "Faction0"; //Provide a name for each faction.
    public string GetName() { return _name; }

    [SerializeField]
    private string code = "faction0"; //A unique code for each faction.
    /// <summary>
    /// Gets the unique code of the faction type.
    /// </summary>
    public string Key { get { return code; } }

    //for NPC factions, if one of the buildings below is unique for the faction, it must be assigned (leave empty if it's not the case)
    [SerializeField]
    private Building capitalBuilding = null;
    public Building GetCapitalBuilding() { return capitalBuilding; }

    [SerializeField]
    private Building centerBuilding = null;
    public Building GetCenterBuilding() { return centerBuilding; }

    [SerializeField]
    private Building populationBuilding = null;
    public Building GetPopulationBuilding() { return populationBuilding; }

    [SerializeField]
    private List<Building> extraBuildings = new List<Building>();
    public IEnumerable<Building> GetExtraBuildings() { return extraBuildings; }

    [SerializeField]
    private List<FactionLimit> limits = new List<FactionLimit>(); //building/unit limits for this faction type.
    public IEnumerable<FactionLimit> GetLimits() { return limits; }
}

[System.Serializable]
public class FactionLimit
{
    public CodeCategoryField code; //the building/unit prefab to limit

    public int maxAmount; //the maximum amount of spawned building/units from the prefab above at the same time
    private int currentAmount; //current amount spawned of the above assigned unit/building

    public bool IsMaxReached() { return currentAmount >= maxAmount; }
    public void Update(int value) { currentAmount += value; }
}
