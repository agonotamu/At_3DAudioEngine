/**
 * 
 * @file At_DynamicRandomPlayer.cs
 * @author Antoine Gonot
 * @version 1.0
 * @date 19/01/2021
 * 
 * @brief Multichannel audio player and spatializer that can be instantiated at runtime, for one shot multiple audio instance with randomization of a playlist
 * 
 * @details
 * Use NAudio API to play multichannel audio file (above 8 channels)
 * Use 3D Audio Engine API to spatialize an audio file with the WAVE FIELD SYNTHESIS technic (calls of function from "AudioPlugin_AtSpatializer.dll")
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class At_DynamicRandomPlayer : MonoBehaviour
{

    public bool is3D = false;
    public bool isDirective = false; //modif mathias 06-17-2021
    public float gain;
    public string[] fileNames;
    public float attenuation;
    public float omniBalance;   
    List<GameObject> playerInstances;
    // minimum distance above which the sound produced by the source is attenuated
    public float minDistance;

    public int[] channelRouting;

    public string externAssetsPath;
    // max number of channel in the audio files
    public int maxChannelsInAudioFile = 0;
    /// number of channel of the output bus
    public int outputChannelCount;
    public float spawnMinAngle;
    public float spawnMaxAngle;
    public float spawnDistance;
    float time = 0;
    At_DynamicRandomPlayerState randomPlayerState;
    

    // Start is called before the first frame update
    void Start()
    {
        playerInstances = new List<GameObject>();
        externAssetsPath = PlayerPrefs.GetString("externAssetsPath_audio");
    }
    private void Update()
    {
        // Auto-generate (Debug)
        
        time += Time.deltaTime;
        if (time > 0.1f)
        {
            AddOneShotInstanceAndRandomPlay();
            time = 0;
        }
        
        
        
    }

    public void AddOneShotInstanceAndRandomPlay()
    {
        if (fileNames != null && fileNames.Length != 0 && fileNames[0] !="")
        {
            playerInstances.Add(new GameObject(gameObject.name+"(Clone"+ playerInstances.Count+")"));            
            //playerInstances[playerInstances.Count - 1].transform.SetParent(transform);
            float r_angle = Random.Range(spawnMinAngle, spawnMaxAngle) * Mathf.PI / 180f;
            float r_distance = Random.Range(0, spawnDistance);
            playerInstances[playerInstances.Count - 1].transform.position = transform.position + new Vector3(r_distance * Mathf.Cos(r_angle), 0, r_distance * Mathf.Sin(r_angle));
            //playerInstances[playerInstances.Count - 1].transform.localPosition = Vector3.zero;
            
            
            playerInstances[playerInstances.Count - 1].AddComponent<At_Player>();
            At_Player p = playerInstances[playerInstances.Count - 1].GetComponent<At_Player>();
            p.is3D = is3D;
            p.isDirective = isDirective;//modif mathias 06-17-2021
            p.gain = gain;
            p.isLooping = false;
            p.omniBalance = omniBalance;            
            p.attenuation = attenuation;            
            int r = Random.Range(0, fileNames.Length);
            p.fileName = fileNames[r];
            p.isDynamicInstance = true;
            p.channelRouting = channelRouting;
            p.minDistance = minDistance;
            p.initAudioFile();
            GameObject.FindObjectOfType<At_MasterOutput>().addPlayerToList(p);
            p.StartPlaying();
        }

        
    }

    private void OnDrawGizmos()
    {
        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithName(gameObject.name);

        if (is3D)
        {
            //float angleOffset = gameObject.transform.eulerAngles.y;
            //Debug.Log(angleOffset);
            const float numStepDrawCircle = 20;
           
            float startAngle = randomPlayerState.spawnMinAngle * Mathf.PI / 180f;
            float endAngle = randomPlayerState.spawnMaxAngle * Mathf.PI / 180f;
            

            float angle = (endAngle - startAngle) / numStepDrawCircle;
            Gizmos.color = Color.green;

            
            for (int i = 0; i < numStepDrawCircle; i++)
            {
               
                Vector3 center = transform.position + new Vector3(randomPlayerState.spawnDistance * Mathf.Cos(startAngle + i * angle), 0, randomPlayerState.spawnDistance * Mathf.Sin(startAngle + i * angle));
                Vector3 nextCenter = transform.position + new Vector3(randomPlayerState.spawnDistance * Mathf.Cos(startAngle + (i + 1) * angle), 0, randomPlayerState.spawnDistance * Mathf.Sin(startAngle + (i + 1) * angle));
                
                
                if (i == 0) Gizmos.DrawLine(transform.position, center);
                else if (i == numStepDrawCircle-1) Gizmos.DrawLine(nextCenter, transform.position);
                //Debug.DrawLine(center, nextCenter, Color.green);

                Gizmos.DrawLine(center, nextCenter);
            }

            SceneView.RepaintAll();

        }
    }

}
