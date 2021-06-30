using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class At_SpeakerConfig : MonoBehaviour
{
    /*
    static float[,] geodesicSphere42_angles = { {0,-60,-69},{1,60,-69},{2,180,69},{3,0,-53},{4,-120,-53},{5,120,-53},{6,-38,-35},{7,38,-35},{8,-82,-35},{9,82,-35},
                                        {10,-158,-35},{11,158,-35},{12,0,-21},{13,-120,-21},{14,120,-21},{15,60,-11},{16,-60,-11},{17,180,-11},{18,-30,0},{19,30,0},
                                        {20,-90,0},{21,90,0},{22,-150,0},{23,150,0},{24,0,11},{25,-120,11},{26,120,11},{27,-60,21},{28,60,21},{29,180,21},
                                        {30,-22,35},{31,22,35},{32,-98,35},{33,98,35},{34,-142,35},{35,142,35},{36,-60,53},{37,60,53},{38,180,53},{39,0,69},
                                        {40,-120,69},{41,120,69}};
    */
    static double[,] geodesicSphere42_angles = { {-179.19, -69.54},{59.86, -70.14},{-61.1, -69.84},{-120.13, -53.29},{120.53, -53.97},{-0.62, -53.95},{-82.29, -36.1},{-157.52, -36.24},{157.72, -36.49},{82.4, -36.95},
                                        {37.3, -36.54},{-38.01, -36.13},{-119.91, -21.67},{120.21, -22.23},{-0.1, -22.06},{-179.95, -12.09},{60.18, -11.97},{-60, -11.73},{-90.07, -0.79},{-149.68, -0.94},
                                        {149.85, -1.25},{90.02, -1.29},{30.14, -1.28},{-29.65, -0.93},{-120, 13.37},{120.27, 12.59},{0.02, 12.75},{-179.88, 22.89},{60.08, 22.58},{-60.05, 23.44},
                                        {-98.45, 37.75},{-142.37, 37.34},{141.61, 37.02},{97.54, 36.62},{21.9, 36.93},{-21.61, 37.24},{-179.97, 54.49},{60.14, 54.01},{-59.77, 54.77},{-120.08, 71.59},
                                        {119.56, 70.76},{0.45, 71.05}};

    static public void addSpeakerConfigToScene(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, int outputConfigDimension, At_MasterOutput parentObject)
    {
        if (outputConfigDimension == 1)
        {
            linearConfig(ref speakers, prefabPath, speakerRigSize, speakerWidth, outputChannelCount, parentObject);
        }
        else if (outputConfigDimension == 2)
        {
            if (outputChannelCount == 4 || outputChannelCount == 6 || outputChannelCount == 8 || outputChannelCount == 12)
            {
                circleConfig(ref speakers, prefabPath, speakerRigSize, speakerWidth, outputChannelCount, parentObject);
            }

        }
        else if (outputConfigDimension == 3)
        {
            if (outputChannelCount == 8)
            {
                cubeConfig8(ref speakers, prefabPath, speakerRigSize, speakerWidth, outputChannelCount, parentObject);
            }
            else if (outputChannelCount == 12)
            {
                cubeConfig8_4(ref speakers, prefabPath, speakerRigSize, speakerWidth, outputChannelCount, parentObject);
            }
            else if (outputChannelCount == 24)
            {
                cubeConfig12_8_4(ref speakers, prefabPath, speakerRigSize, speakerWidth, outputChannelCount, parentObject);
            }
            else if (outputChannelCount == 42)
            {
                geodesicSphere42(ref speakers, prefabPath, speakerRigSize, speakerWidth, outputChannelCount, parentObject);
            }
        }

    }

   static void linearConfig(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, At_MasterOutput parentObject)
    {
       
        float speakerTargetwidth = speakerRigSize / (float)outputChannelCount;
        //float scale = (speakerTargetwidth * 0.9f) / speakerWidth;
        float scale = speakerRigSize / 3.0f;
        speakers = new GameObject[outputChannelCount];
        for (int speakerCount = 0; speakerCount < outputChannelCount; speakerCount++)
        {
            Vector3 position = new Vector3(-speakerRigSize / 2 + speakerTargetwidth / 2 + speakerCount * speakerTargetwidth, 0, 0);

            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
        }

    }

    static void circleConfig(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, At_MasterOutput parentObject)
    {

        float speakerTargetwidth = speakerRigSize / (float)outputChannelCount;
        //float scale = (speakerTargetwidth * 0.9f) / speakerWidth;
        float scale = speakerRigSize / 3.0f;
        speakers = new GameObject[outputChannelCount];
        float angularStep = 2.0f * Mathf.PI  / (float)outputChannelCount;
        float angle = -angularStep / 2.0f;
        for (int speakerCount = 0; speakerCount < outputChannelCount; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle)/2.0f,0, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }
    }

    static void cubeConfig8(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, At_MasterOutput parentObject)
    {
        float speakerTargetwidth = speakerRigSize / (float)outputChannelCount;
        //float scale = (speakerTargetwidth * 0.9f) / speakerWidth;
        float scale = speakerRigSize / 3.0f;
        speakers = new GameObject[outputChannelCount];
        float angularStep = 2.0f * Mathf.PI / (float)(outputChannelCount/2);
        float angle = -angularStep / 2.0f;
        for (int speakerCount = 0; speakerCount < outputChannelCount/2; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, 0, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }
        
        angle = -angularStep / 2.0f;
        for (int speakerCount = outputChannelCount / 2; speakerCount < outputChannelCount ; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, speakerRigSize/2, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }
        

    }
    static void cubeConfig8_4(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, At_MasterOutput parentObject)
    {
        float speakerTargetwidth = speakerRigSize / (float)outputChannelCount;
        //float scale = (speakerTargetwidth * 0.9f) / speakerWidth;
        float scale = speakerRigSize / 3.0f;
        speakers = new GameObject[outputChannelCount];
        float angularStep = 2.0f * Mathf.PI / 8.0f;
        float angle = -angularStep / 2.0f;
        for (int speakerCount = 0; speakerCount < 8; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, 0, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }

        angularStep = 2.0f * Mathf.PI /4.0f;
        angle = -angularStep / 2.0f;
        for (int speakerCount = 8; speakerCount < 12; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, speakerRigSize / 2, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }
    }
    static void cubeConfig12_8_4(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, At_MasterOutput parentObject)
    {
        float speakerTargetwidth = speakerRigSize / (float)outputChannelCount;
        //float scale = (speakerTargetwidth * 0.9f) / speakerWidth;
        float scale = speakerRigSize / 3.0f;
        speakers = new GameObject[outputChannelCount];
        float angularStep = 2.0f * Mathf.PI / 12.0f;
        float angle = -angularStep / 2.0f;
        for (int speakerCount = 0; speakerCount < 12; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, 0, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }

        angularStep = 2.0f * Mathf.PI / 8.0f;
        angle = -angularStep / 2.0f;
        for (int speakerCount = 12; speakerCount < 20; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, speakerRigSize / 4, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }
        angularStep = 2.0f * Mathf.PI / 4.0f;
        angle = -angularStep / 2.0f;
        for (int speakerCount = 20; speakerCount < 24; speakerCount++)
        {
            Vector3 position = new Vector3(speakerRigSize * Mathf.Sin(angle) / 2.0f, speakerRigSize / 2, speakerRigSize * Mathf.Cos(angle) / 2.0f);
            speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
            speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
            speakers[speakerCount].transform.LookAt(parentObject.transform.position);
            speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
            speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
            speakers[speakerCount].transform.SetParent(parentObject.transform);
            angle += angularStep;
        }
    }
    static void geodesicSphere42(ref GameObject[] speakers, string prefabPath, float speakerRigSize, float speakerWidth, int outputChannelCount, At_MasterOutput parentObject)
    {
        if (outputChannelCount <= 42)
        {
            
            float speakerTargetwidth = speakerRigSize / (float)outputChannelCount;
            float scale = (speakerTargetwidth * 1.5f) / speakerWidth;

            speakers = new GameObject[outputChannelCount];
            float r = (speakerRigSize / 2f);
            for (int speakerCount = 0; speakerCount < outputChannelCount; speakerCount++)
            {
                
                float az = (float) (90.0-geodesicSphere42_angles[speakerCount, 0]);
                float el = (float)(90.0-geodesicSphere42_angles[speakerCount, 1]);
                float convRad = Mathf.PI / 180;
                Vector3 position = new Vector3(r * Mathf.Sin(el * convRad) * Mathf.Cos(az * convRad),
                    r * Mathf.Cos(el * convRad),
                    r * Mathf.Sin(el * convRad) * Mathf.Sin(az * convRad));
                
                position += parentObject.transform.position;

                 speakers[speakerCount] = Instantiate(Resources.Load<GameObject>("At_3DAudioEngine/Prefabs/VirtualMicModel"), position, Quaternion.identity);
                speakers[speakerCount].transform.localScale = new Vector3(scale, scale, scale);
                
                speakers[speakerCount].transform.LookAt(parentObject.transform.position);
                speakers[speakerCount].transform.Rotate(new Vector3(0, 180, 0));
                speakers[speakerCount].GetComponent<At_VirtualMic>().id = speakerCount;
                speakers[speakerCount].transform.SetParent(parentObject.transform);
            }
        }



    }
}
