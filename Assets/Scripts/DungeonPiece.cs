using System;
using UnityEngine;

[Serializable]
public class CompatibleExits
{
    public string[] compatibleExits;
}

public class DungeonPiece : MonoBehaviour
{
    public CompatibleExits[] allCompatibleExits;
    public Transform[] exitLocations;
}