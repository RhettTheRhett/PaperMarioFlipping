using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WorldState
{
    Flat2d,
    Flipped3d,
}

public class WorldStateManager : MonoBehaviour
{

    public WorldState worldState;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void ChangeWorldState(WorldState newState)
    {
        worldState = newState;
    }

    public WorldState GetWorldState()
    {
        return worldState;
    }
}
