using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class RippleControler : MonoBehaviour
{
    RippleParam[] ripples;
    public float maxSize;
    public float rate;
    public At_Player player;
    Vector3 previousPlayerPosition;
    float[] delayArray;
    float[] volumeArray;
    
    // Start is called before the first frame update
    void Start()
    {
        ripples = FindObjectsOfType<RippleParam>();
        if (ripples != null && ripples.Length != 0)
        {
            delayArray = new float[ripples.Length];
            volumeArray = new float[ripples.Length];

            foreach (RippleParam rp in ripples)
            {
                rp.setMaxSize(maxSize);
                rp.setRate(rate);
            }

        }
    }
    
    // Update is called once per frame
    void Update()
    {

        if (player != null && ripples !=null && ripples.Length !=0)
        {
            getDelay(player.spatID, delayArray, ripples.Length);
            getVolume(player.spatID, volumeArray, ripples.Length);

            /*
            string displayedDelay="";
            for (int i = 0; i< ripples.Length; i++)
            {
                displayedDelay += " - " + delayArray[i];
            }
            Debug.Log(displayedDelay);
            */
        }

        if (ripples != null && ripples.Length != 0)
        {
            foreach (RippleParam rp in ripples)
            {
                //rp.setMaxSize(maxSize);
                //rp.setRate(rate);
                if (player != null)
                {                   
                    if (delayArray != null && volumeArray != null)
                    {
                        rp.setParam(delayArray[rp.id], volumeArray[rp.id], maxSize, rate, player.timeReversal);
                    }
                   
                }

            } 
           
        }

    }

    public unsafe void getDelay(int spatID, float[] delay, int arraySize)
    {
        fixed(float* delayPtr = delay)
        {
            AT_SPAT_WFS_getDelay(spatID, (IntPtr)delayPtr, arraySize);
        }
    }
    
    public unsafe void getVolume(int spatID, float[] volume, int arraySize)
    {

        fixed (float* volumePtr = volume)
        {
            AT_SPAT_WFS_getVolume(spatID, (IntPtr)volumePtr, arraySize);
        }
    }
    
    #region DllImport        
    [DllImport("AudioPlugin_AtSpatializer", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AT_SPAT_WFS_getDelay(int id, IntPtr delay, int arraySize);
    [DllImport("AudioPlugin_AtSpatializer", CallingConvention = CallingConvention.Cdecl)]
    private static extern void AT_SPAT_WFS_getVolume(int id, IntPtr volume, int arraySize);
    #endregion
}
