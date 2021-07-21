/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : class used for maintaining the state (file path, gain, etc.) that define all the properties of the GUI and the core engine of the Audio Player (At_Player class).  
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_3DAudioEngineState
{

    /******************************************************************************************************
    * 
    *                                  output states
    * 
    * ***************************************************************************************************/

    public At_OutputState outputState = null;// = new At_OutputState();
    
    public At_OutputState getOutputState()
    {
        return outputState;    
    }
    public void setOutputState(At_OutputState state)
    {
        outputState = state;
    }


    /******************************************************************************************************
     * 
     *                                  players state
     * 
     * ***************************************************************************************************/
    // List of At_PLayerState maintaining the state of each player attached to a GameObject in the scene 
    public List<At_PlayerState> playerStates = new List<At_PlayerState>();

    // Get whole PLayer State with the GameObject Name
    public At_PlayerState getPlayerState(string name)
    {
        // loop through the list of Saved Players in the scene
        for (int i = 0; i < playerStates.Count; i++)
        {
            // if the name is found, return
            if (playerStates[i].name == name)
            {
                return playerStates[i];
            }
        }
        return null;
    }

    // Remove a Player State from the list (when the component is removed or the GameObject Destroyed 
    public bool removePlayerState(string name)
    {
        for (int i = 0; i < playerStates.Count; i++)
        {
            if (playerStates[i].name == name)
            {
                Debug.Log("remove " + name);
                playerStates.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

  /******************************************************************************************************
  * 
  *                                  dynamic random players state
  * 
  * ***************************************************************************************************/
    // List of At_PLayerState maintaining the state of each player attached to a GameObject in the scene 
    public List<At_DynamicRandomPlayerState> randomPlayerStates = new List<At_DynamicRandomPlayerState>();

    // Get whole PLayer State with the GameObject Name
    public At_DynamicRandomPlayerState getRandomPlayerState(string name)
    {
        // loop through the list of Saved Players in the scene
        for (int i = 0; i < randomPlayerStates.Count; i++)
        {
            // if the name is found, return
            if (randomPlayerStates[i].name == name)
            {
                return randomPlayerStates[i];
            }
        }
        return null;
    }

    /******************************************************************************************************
    * 
    *                   Virtual Speakers and Virtual Microphone Configuration
    * 
    * ***************************************************************************************************/
    public At_VirtualSpeakerState virtualSpeakerState = new At_VirtualSpeakerState();// = new At_OutputState();

    public At_VirtualSpeakerState getVirtualSpeakerState()
    {
        return virtualSpeakerState;
    }
    public void setVirtualSpeakerState(At_VirtualSpeakerState state)
    {
        virtualSpeakerState = state;
    }

}
