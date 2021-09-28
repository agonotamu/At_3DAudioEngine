using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class At_SpeakerConfig :MonoBehaviour//: Editor
{

    static float virtualMicScale = 50f;
    static float virtualSpeakerScale = 3f;
    static string virtualMicModel = "At_3DAudioEngine/Prefabs/simpleArrowModel";
    static string virtualSpeakerModel = "At_3DAudioEngine/Prefabs/simpleSpeakerModel";

    static public void updateVirtualMicPosition(At_VirtualMic virtualMic, At_VirtualSpeaker virtualSpeaker, float virtualMicRigSize, float virtualMicWidth, float speakerRigSize)
    {
        float ratio = virtualSpeaker.distance / virtualSpeaker.gameObject.transform.position.magnitude;
        virtualMic.transform.position = virtualSpeaker.transform.position.normalized * ratio * virtualMicRigSize;
    } 

    static public void addSpeakerConfigToScene(ref GameObject[] virtualMic, float virtualMicRigSize, ref GameObject[] speakers, float speakerRigSize,
        int outputChannelCount, int outputConfigDimension, GameObject virtualMicParent, GameObject  virtualSpkParent)
    {
        if (outputConfigDimension == 1)
        {
            linearConfig(ref virtualMic, virtualMicRigSize, 
                ref speakers, speakerRigSize,
                outputChannelCount, virtualMicParent, virtualSpkParent);
        }
        else if (outputConfigDimension == 2)
        {
            circleConfig(ref virtualMic, virtualMicRigSize,
                ref speakers, speakerRigSize,
                outputChannelCount, virtualMicParent, virtualSpkParent);
        }
       

    }

    static void addSpeakers(bool is2D, ref GameObject[] speakers, GameObject[] virtualMic, float speakerRigSize, GameObject virtualSpkParent)
    {
        speakers = new GameObject[virtualMic.Length];
        for (int spkCount = 0; spkCount < virtualMic.Length; spkCount++)
        {
            //float virtualMicTargetwidth = speakerRigSize / (float)virtualMic.Length;
            //float scale = (virtualMicTargetwidth * 0.5f) / speakerWidth;
            if (is2D)
            {
                Vector3 center = virtualSpkParent.transform.parent.transform.position;

                Vector3 position = center + (virtualMic[spkCount].transform.position - center).normalized * speakerRigSize;
                speakers[spkCount] = Instantiate(Resources.Load<GameObject>(virtualSpeakerModel), position, Quaternion.identity);
                //UnityEditor.PrefabUtility.UnpackPrefabInstance(speakers[spkCount], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                speakers[spkCount].transform.localScale = new Vector3(virtualSpeakerScale, virtualSpeakerScale, virtualSpeakerScale);
                speakers[spkCount].transform.eulerAngles = new Vector3(virtualMic[spkCount].transform.eulerAngles.x, virtualMic[spkCount].transform.eulerAngles.y, virtualMic[spkCount].transform.eulerAngles.z);
                speakers[spkCount].GetComponent<At_VirtualSpeaker>().id = virtualMic[spkCount].GetComponent<At_VirtualMic>().id;
                speakers[spkCount].transform.SetParent(virtualSpkParent.transform);
                speakers[spkCount].transform.Rotate(0, 180, 0);
                speakers[spkCount].GetComponent<At_VirtualSpeaker>().distance = speakerRigSize;

            }
            
            else
            {
                Vector3 position = virtualMic[spkCount].transform.position;
                speakers[spkCount] = Instantiate(Resources.Load<GameObject>(virtualSpeakerModel), position, Quaternion.identity);
                //UnityEditor.PrefabUtility.UnpackPrefabInstance(speakers[spkCount], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                speakers[spkCount].transform.localScale = new Vector3(virtualSpeakerScale, virtualSpeakerScale, virtualSpeakerScale);
                speakers[spkCount].transform.eulerAngles = new Vector3(virtualMic[spkCount].transform.eulerAngles.x, virtualMic[spkCount].transform.eulerAngles.y, virtualMic[spkCount].transform.eulerAngles.z);
                speakers[spkCount].GetComponent<At_VirtualSpeaker>().id = virtualMic[spkCount].GetComponent<At_VirtualMic>().id;
                speakers[spkCount].transform.SetParent(virtualSpkParent.transform);
                speakers[spkCount].transform.Rotate(0, 180, 0);
                speakers[spkCount].GetComponent<At_VirtualSpeaker>().distance = 0;

            }
        }
    }

   static void linearConfig(ref GameObject[] virtualMic, float virtualMicRigSize,
        ref GameObject[] speakers, float speakerRigSize,
       int outputChannelCount, GameObject virtualMicParent, GameObject virtualSpkParent)
    {

        float virtualMicTargetwidth = virtualMicRigSize / (float)outputChannelCount;
        //float scale = (virtualMicTargetwidth * 0.5f) / virtualMicWidth;
        //float scale = virtualMicRigSize / 3.0f;
        virtualMic = new GameObject[outputChannelCount];
        for (int micCount = 0; micCount < outputChannelCount; micCount++)
        {
            Vector3 position = new Vector3(-virtualMicRigSize / 2 + virtualMicTargetwidth / 2 + micCount * virtualMicTargetwidth, 0, 0);

            virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);
            //UnityEditor.PrefabUtility.UnpackPrefabInstance(virtualMic[micCount], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            virtualMic[micCount].transform.localScale = new Vector3(virtualMicScale, virtualMicScale, virtualMicScale);
            virtualMic[micCount].GetComponent<At_VirtualMic>().id = micCount;
            virtualMic[micCount].transform.SetParent(virtualMicParent.transform);
        }
        addSpeakers(false, ref speakers, virtualMic, speakerRigSize,virtualSpkParent);
    }

    static void circleConfig(ref GameObject[] virtualMic, float virtualMicRigSize,
        ref GameObject[] speakers, float speakerRigSize,  int outputChannelCount, GameObject virtualMicParent, GameObject virtualSpkParent)
    {

        float virtualMicTargetwidth = virtualMicRigSize / (float)outputChannelCount;
        //float scale = (virtualMicTargetwidth * 0.5f) / virtualMicWidth;
        
        
        //float scale = virtualMicRigSize / 3.0f;
        virtualMic = new GameObject[outputChannelCount];
        float angularStep = 2.0f * Mathf.PI  / (float)outputChannelCount;
        float angle = -angularStep / 2.0f;
        for (int micCount = 0; micCount < outputChannelCount; micCount++)
        {
            Vector3 position = new Vector3(virtualMicRigSize * Mathf.Sin(angle),0, virtualMicRigSize * Mathf.Cos(angle));
            //virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);
            virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);
            //UnityEditor.PrefabUtility.UnpackPrefabInstance(virtualMic[micCount], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            virtualMic[micCount].transform.localScale = new Vector3(virtualMicScale, virtualMicScale, virtualMicScale);
            virtualMic[micCount].transform.LookAt(virtualMicParent.transform.parent.transform.position);
            virtualMic[micCount].transform.Rotate(new Vector3(0, 180, 0));
            virtualMic[micCount].GetComponent<At_VirtualMic>().id = micCount;
            virtualMic[micCount].transform.SetParent(virtualMicParent.transform);
            angle += angularStep;
            
            
        }
        addSpeakers(true, ref speakers, virtualMic, speakerRigSize, virtualSpkParent);
    }
    
}
