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
    public At_PlayerState getPlayerState(string guid)
    {
        // loop through the list of Saved Players in the scene
        for (int i = 0; i < playerStates.Count; i++)
        {
            // if the name is found, return
            if (playerStates[i].guid == guid)
            {
                return playerStates[i];
            }
        }
        return null;
    }


  /******************************************************************************************************
  * 
  *                                  dynamic random players state
  * 
  * ***************************************************************************************************/
    // List of At_PLayerState maintaining the state of each player attached to a GameObject in the scene 
    public List<At_DynamicRandomPlayerState> randomPlayerStates = new List<At_DynamicRandomPlayerState>();

    // Get whole PLayer State with the GameObject Name
    public At_DynamicRandomPlayerState getRandomPlayerState(string guid)
    {
        // loop through the list of Saved Players in the scene
        for (int i = 0; i < randomPlayerStates.Count; i++)
        {
            // if the name is found, return
            if (randomPlayerStates[i].guid == guid)
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
