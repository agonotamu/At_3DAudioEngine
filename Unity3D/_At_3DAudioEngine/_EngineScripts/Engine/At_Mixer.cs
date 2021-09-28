/**
 * 
 * @file At_Mixer.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 19/01/2021
 * 
 * @brief Mixer for the audio players
 * 
 * @details
 * This class maintain the list of all At_Player class instantiated in the scene. When the the asio driver buffer 
 * must be filled the At_MasterOutput call the fillMasterChannelInput() which sums the /p playerOutputBuffer of
 * each At_Player class
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class At_Mixer : MonoBehaviour
{
    /// constants used for array initialization 
    const int MAX_BUF_SIZE = 2048;

    /// List of all the At_PLayer classes instantiated in the scene
    List<At_Player> playerList;

    /// temporary buffer filled by a At_Player object with the samples for one channel
    private float[] tmpMonoBuffer = new float[MAX_BUF_SIZE];


    /**
    * @brief Method called by the At_MasterOutput to set the list of At_PLayer classes instantiated in the scene
    * 
    * @param[in] pl : list of At_PLayer
    * 
    */
    public void setPlayerList(List<At_Player> pl)
    {
        playerList = pl;
    }

    /**
    * @brief Method called by the At_Mixer, which sum all the buffers filled by the different players in the scene. 
    * 
    * @param[out] mixerInputBuffer : Monophonic buffer to fill for a given output channel
    * @param[in] bufferSize : size of the output buffer (number of sample for a frame)
    * @param[in] channelIndex : index of the channel to fill
    * 
    */
    public void fillMasterChannelInput(ref float[] mixBuffer, int bufferSize, int channelIndex, List<int> spatIDToDestroy)
    {
        if (playerList != null)
        {            
            for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
            {
                if (!playerIsDestroyedOnNextFrame(playerList[playerIndex], spatIDToDestroy))
                {
                    
                    // ask the At_Player object to copy the output of the player in the "tmpMonoBuffer" array
                    playerList[playerIndex].fillMixerChannelInputWithPlayerOutput(ref tmpMonoBuffer, bufferSize, channelIndex);
                    // add the samples of the "tmpMonoBuffer" array to the samples of the "mixBuffer" array provided by the At_MasterOutput object
                    Add(ref mixBuffer, tmpMonoBuffer, bufferSize);
                    // clear the "tmpMonoBuffer" array
                    System.Array.Clear(tmpMonoBuffer, 0, tmpMonoBuffer.Length);
                }
                
            }            
        }
    }

    bool playerIsDestroyedOnNextFrame(At_Player player, List<int> spatIDToDestroy)
    {
        for (int i = 0; i < spatIDToDestroy.Count; i++)
        {
            if (player.spatID == spatIDToDestroy[i])
            {
                return true;
            }
        }
        return false;
    }

    // add the samples of "buffer" array to the samples of "addBuffer" array
    void Add(ref float[] addBuffer, float [] buffer, int bufferSize)
    {
        for (int sampleIndex = 0; sampleIndex < bufferSize; sampleIndex++)
        {
            addBuffer[sampleIndex] += buffer[sampleIndex];
        }
    }

}

