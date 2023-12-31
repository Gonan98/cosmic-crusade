﻿using UnityEngine;

using RTSEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "RTS Engine/Resource Type", order = 2)]
public class ResourceTypeInfo : ScriptableObject, IAssetFile
{
    [SerializeField]
    private string _name = "new_resource"; //Resource name
    /// <summary>
    /// Get the unique name of the ResourceTypeInfo.
    /// </summary>
    public string Key { get { return _name; } }

    [SerializeField]
    private int startingAmount = 10; //the amount that each team will start with.
    public int GetStartingAmount() { return startingAmount; }

    [SerializeField]
    private Sprite icon = null; //resource Icon.
    public Sprite GetIcon() { return icon; }

    [SerializeField]
    private Color minimapIconColor = Color.green; //the color of the minimap's icon for this resource
    public Color GetMinimapIconColor() { return minimapIconColor; }

    //Audio clips:
    [SerializeField, Tooltip("What audio clip to play when a unit is collecting a resource of this type?")]
    private AudioClipFetcher collectionAudio = new AudioClipFetcher(); //audio played each time the unit collects some of this resource.
    public AudioClip GetCollectionAudio() { return collectionAudio.Fetch(); }
}
