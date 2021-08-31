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
    //List<GameObject> playerInstances;
    GameObject[] playerInstances;
    float[] playerInstancesCreationTime;
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
    const int maxInstance = 20;
    
    
    public string guid="";

    void Reset()
    {
        setGuid();
        
    }
    void OnValidate()
    {
        Event e = Event.current;

        if (e != null)
        {
            if (e.type == EventType.ExecuteCommand && e.commandName == "Duplicate")
            {
                setGuid();
            }
        }
    }

    public void setGuid()
    {
        guid = System.Guid.NewGuid().ToString();
        //Debug.Log("create player with guid : " + guid);
    }

    // Start is called before the first frame update
    void Start()
    {
        At_MasterOutput mo = GameObject.FindObjectOfType<At_MasterOutput>();//.
        //playerInstances = new List<GameObject>();
        playerInstances = new GameObject[maxInstance];
        playerInstancesCreationTime = new float[maxInstance];

        for (int i = 0;i< maxInstance; i++)
        {
            playerInstances[i] = new GameObject();
            //playerInstances[i].name = randomPlayerState.name + "_" + i.ToString("00");
            playerInstances[i].transform.SetParent(gameObject.transform);
            playerInstances[i].AddComponent<At_Player>();
            mo.addPlayerToList(playerInstances[i].GetComponent<At_Player>());
        }
        
        //externAssetsPath = PlayerPrefs.GetString("externAssetsPath_audio");

    }
    private void Update()
    {
        // Auto-generate (Debug)
        /*
        time += Time.deltaTime;
        if (time > 0.1f)
        {
            AddOneShotInstanceAndRandomPlay();
            time = 0;
        }
        */
        
        //System.GC.Collect();
        //Resources.UnloadUnusedAssets();

    }
    int getFreeSlot()
    {
        int freeSlotIndex = -1;

        for (int i = 0; i < maxInstance; i++){

            At_Player p = playerInstances[i].GetComponent<At_Player>();

            if (!p.isPlaying)
            {
                freeSlotIndex = i;
                break;
            }

        }

        return freeSlotIndex;

    }
    
    int getOlder()
    {
        int indexOlder = 0;
        for (int i = 0; i < maxInstance; i++)
        {
            if (playerInstancesCreationTime[i] < playerInstancesCreationTime[indexOlder])
            {
                indexOlder = i;
            }
        }
        return indexOlder;
    }

    public void AddOneShotInstanceAndRandomPlay()
    {
        if (fileNames != null && fileNames.Length != 0 && fileNames[0] !="")
        {
           
            int indexinstance = getFreeSlot();
            if (indexinstance == -1)
            {
                // we use the first slot
                indexinstance = getOlder();                
            }

            playerInstancesCreationTime[indexinstance] = Time.realtimeSinceStartup;
            //Debug.Log("Inex Random =" + indexinstance);
            At_Player p = playerInstances[indexinstance].GetComponent<At_Player>();
            p.StopPlaying();
            float r_angle = Random.Range(spawnMinAngle, spawnMaxAngle) * Mathf.PI / 180f;
            float r_distance = Random.Range(0, spawnDistance);
            playerInstances[indexinstance].transform.position = transform.position + new Vector3(r_distance * Mathf.Cos(r_angle), 0, r_distance * Mathf.Sin(r_angle));
            p.is3D = is3D;
            p.isDirective = isDirective;//modif mathias 06-17-2021
            p.gain = gain;
            p.isLooping = false;
            p.omniBalance = omniBalance;
            p.attenuation = attenuation;
            int r = Random.Range(0, fileNames.Length);

            p.externAssetsPath_audio = At_AudioEngineUtils.getExternalAssetsState().externAssetsPath_audio;
            p.externAssetsPath_audio_standalone = At_AudioEngineUtils.getExternalAssetsState().externAssetsPath_audio_standalone;

            p.fileName = fileNames[r];
            p.isDynamicInstance = true;
            p.channelRouting = channelRouting;
            p.minDistance = minDistance;
            p.initAudioFile(true);            
            p.StartPlaying();

        }

        
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithGuidAndName(guid, gameObject.name);//getRandomPlayerStateWithName(gameObject.name);

        float spawnMinAngle = 0, spawnMaxAngle = 0, spawnDistance = 0;

        if (randomPlayerState != null)
        {
            spawnMinAngle = randomPlayerState.spawnMinAngle;
            spawnMaxAngle = randomPlayerState.spawnMaxAngle;
            spawnDistance = randomPlayerState.spawnDistance;
        }

        if (is3D)
        {
            //float angleOffset = gameObject.transform.eulerAngles.y;
            //Debug.Log(angleOffset);
            const float numStepDrawCircle = 20;
           
            float startAngle = spawnMinAngle * Mathf.PI / 180f;
            float endAngle = spawnMaxAngle * Mathf.PI / 180f;
            

            float angle = (endAngle - startAngle) / numStepDrawCircle;
            Gizmos.color = Color.green;

            
            for (int i = 0; i < numStepDrawCircle; i++)
            {
               
                Vector3 center = transform.position + new Vector3(spawnDistance * Mathf.Cos(startAngle + i * angle), 0, spawnDistance * Mathf.Sin(startAngle + i * angle));
                Vector3 nextCenter = transform.position + new Vector3(spawnDistance * Mathf.Cos(startAngle + (i + 1) * angle), 0, spawnDistance * Mathf.Sin(startAngle + (i + 1) * angle));
                
                
                if (i == 0) Gizmos.DrawLine(transform.position, center);
                else if (i == numStepDrawCircle-1) Gizmos.DrawLine(nextCenter, transform.position);
                //Debug.DrawLine(center, nextCenter, Color.green);

                Gizmos.DrawLine(center, nextCenter);
            }

            SceneView.RepaintAll();

        }
    }
#endif
}
