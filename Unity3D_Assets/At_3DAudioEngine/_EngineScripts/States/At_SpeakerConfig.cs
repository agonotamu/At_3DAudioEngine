using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class At_SpeakerConfig : MonoBehaviour//: Editor
{

    static float virtualMicScale = 50f;
    static float virtualSpeakerScale = 3f;
    static string virtualMicModel = "At_3DAudioEngine/Prefabs/simpleArrowModel";
    static string virtualSpeakerModel = "At_3DAudioEngine/Prefabs/simpleSpeakerModel";
    static string ripplePrefab = "At_3DAudioEngine/Prefabs/RippleParticle/RipplePrefab";

    static double[,] geodesicSphere42_angles = { {-179.19, -69.54},{59.86, -70.14},{-61.1, -69.84},{-120.13, -53.29},{120.53, -53.97},{-0.62, -53.95},{-82.29, -36.1},{-157.52, -36.24},{157.72, -36.49},{82.4, -36.95},
                                        {37.3, -36.54},{-38.01, -36.13},{-119.91, -21.67},{120.21, -22.23},{-0.1, -22.06},{-179.95, -12.09},{60.18, -11.97},{-60, -11.73},{-90.07, -0.79},{-149.68, -0.94},
                                        {149.85, -1.25},{90.02, -1.29},{30.14, -1.28},{-29.65, -0.93},{-120, 13.37},{120.27, 12.59},{0.02, 12.75},{-179.88, 22.89},{60.08, 22.58},{-60.05, 23.44},
                                        {-98.45, 37.75},{-142.37, 37.34},{141.61, 37.02},{97.54, 36.62},{21.9, 36.93},{-21.61, 37.24},{-179.97, 54.49},{60.14, 54.01},{-59.77, 54.77},{-120.08, 71.59},
                                        {119.56, 70.76},{0.45, 71.05}};

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
        else if (outputConfigDimension == 3)
        {
            geodesicSphere42(ref virtualMic, virtualMicRigSize,
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
                //Vector3 position = center + virtualMic[spkCount].transform.position;
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
            // Uncomment if you want ripple on eah speakers... Debug Only !!
            /*
            GameObject go = Instantiate(Resources.Load<GameObject>(ripplePrefab), Vector3.zero, Quaternion.identity);
            go.transform.parent = speakers[spkCount].transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.eulerAngles = new Vector3(90, 0, 0);
            go.GetComponent<RippleParam>().id = virtualMic[spkCount].GetComponent<At_VirtualMic>().id;
            */
            PrefabUtility.RecordPrefabInstancePropertyModifications(speakers[spkCount].transform);
        }
    }

   static void linearConfig(ref GameObject[] virtualMic, float virtualMicRigSize,
        ref GameObject[] speakers, float speakerRigSize,
       int outputChannelCount, GameObject virtualMicParent, GameObject virtualSpkParent)
    {
        //virtualMicRigSize *= 2;
        float virtualMicTargetwidth = virtualMicRigSize / (float)(outputChannelCount-1);
        //float scale = (virtualMicTargetwidth * 0.5f) / virtualMicWidth;
        //float scale = virtualMicRigSize / 3.0f;
        virtualMic = new GameObject[outputChannelCount];
        for (int micCount = 0; micCount < outputChannelCount; micCount++)
        {
            //Vector3 position = new Vector3(-virtualMicRigSize / 2 + virtualMicTargetwidth / 2 + micCount * virtualMicTargetwidth, 0, 0);
            Vector3 position = new Vector3(-virtualMicRigSize / 2 + micCount * virtualMicTargetwidth, 0, 0);

            virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);
            //UnityEditor.PrefabUtility.UnpackPrefabInstance(virtualMic[micCount], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            virtualMic[micCount].transform.localScale = new Vector3(virtualMicScale, virtualMicScale, virtualMicScale);
            virtualMic[micCount].GetComponent<At_VirtualMic>().id = micCount;
            virtualMic[micCount].transform.SetParent(virtualMicParent.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(virtualMic[micCount].transform);
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
            Vector3 position = new Vector3((virtualMicRigSize/2f) * Mathf.Sin(angle),0, (virtualMicRigSize / 2f) * Mathf.Cos(angle));

            //virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);
            virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);

            virtualMic[micCount].transform.localScale = new Vector3(virtualMicScale, virtualMicScale, virtualMicScale);
            virtualMic[micCount].transform.LookAt(virtualMicParent.transform.parent.transform.position);
            virtualMic[micCount].transform.Rotate(new Vector3(0, 180, 0));
            virtualMic[micCount].GetComponent<At_VirtualMic>().id = micCount;
            virtualMic[micCount].transform.SetParent(virtualMicParent.transform);
            
            //virtualMic[micCount].transform.position = position;
            angle += angularStep;
            PrefabUtility.RecordPrefabInstancePropertyModifications(virtualMic[micCount].transform);


        }
        addSpeakers(true, ref speakers, virtualMic, speakerRigSize, virtualSpkParent);
    }

    static void geodesicSphere42(ref GameObject[] virtualMic, float virtualMicRigSize,
        ref GameObject[] speakers, float speakerRigSize, int outputChannelCount, GameObject virtualMicParent, GameObject virtualSpkParent)
    {

        float virtualMicTargetwidth = virtualMicRigSize / (float)outputChannelCount;
      
        virtualMic = new GameObject[outputChannelCount];

        //float r = (speakerRigSize / 2f);
        for (int micCount = 0; micCount < outputChannelCount; micCount++)
        {
            float az = (float)(90.0 - geodesicSphere42_angles[micCount, 0]);
            float el = (float)(90.0 - geodesicSphere42_angles[micCount, 1]);
            float convRad = Mathf.PI / 180;
            Vector3 position = new Vector3((virtualMicRigSize / 2f) * Mathf.Sin(el * convRad) * Mathf.Cos(az * convRad),
                (virtualMicRigSize / 2f) * Mathf.Cos(el * convRad),
                (virtualMicRigSize / 2f) * Mathf.Sin(el * convRad) * Mathf.Sin(az * convRad));
          
            virtualMic[micCount] = Instantiate(Resources.Load<GameObject>(virtualMicModel), position + virtualMicParent.transform.parent.transform.position, Quaternion.identity);            
            virtualMic[micCount].transform.localScale = new Vector3(virtualMicScale, virtualMicScale, virtualMicScale);
            virtualMic[micCount].transform.LookAt(virtualMicParent.transform.parent.transform.position);
            virtualMic[micCount].transform.Rotate(new Vector3(0, 180, 0));
            virtualMic[micCount].GetComponent<At_VirtualMic>().id = micCount;
            virtualMic[micCount].transform.SetParent(virtualMicParent.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(virtualMic[micCount].transform);
        }
        addSpeakers(true, ref speakers, virtualMic, speakerRigSize, virtualSpkParent);
    }
}
