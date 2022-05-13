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
using UnityEngine.SceneManagement;

public class At_DynamicRandomPlayer : MonoBehaviour
{
    public GameObject[] playerInstances;
    float[] playerInstancesCreationTime;
    //--------------------------------------

    public string[] fileNames;
    public float gain;
    public bool is3D = false;
    public bool isDirective = false; //modif mathias 06-17-2021
    public float omniBalance;
    public float attenuation;
    // minimum distance above which the sound produced by the source is attenuated
    public float minDistance;
    public int[] channelRouting;
    public float spawnMinAngle;
    public float spawnMaxAngle;
    public float spawnDistance;

    public string externAssetsPath;
    // max number of channel in the audio files
    public int maxChannelsInAudioFile = 0;
    /// number of channel of the output bus
    public int outputChannelCount;

    At_DynamicRandomPlayerState randomPlayerState;
    const int maxInstance = 10;

    public string guid="";

    At_MasterOutput masterOutput;

    float time = 0;

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
            // if the object has been draged... Then it should be prefab.
            if (e.type == EventType.DragPerform)
            {
                setGuid();
            }
        }
    }

    public void setGuid()
    {
        guid = System.Guid.NewGuid().ToString();

    }

    // Start is called before the first frame update
    void Start()
    {
        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, guid, gameObject.name);
        masterOutput = GameObject.FindObjectOfType<At_MasterOutput>();//.
        playerInstances = new GameObject[maxInstance];
        playerInstancesCreationTime = new float[maxInstance];

        for (int i = 0;i< maxInstance; i++)
        {
            playerInstances[i] = new GameObject();
            playerInstances[i].SetActive(false);
            
            
            playerInstances[i].transform.SetParent(gameObject.transform);
            playerInstances[i].AddComponent<At_Player>();
            At_Player p = playerInstances[i].GetComponent<At_Player>();
            
            p.isDynamicInstance = true;

            masterOutput.addPlayerToList(p); 
        }
       

    }
    private void Update()
    {
        // Auto-generate (Debug)
        
        time += Time.deltaTime;
        if (time > 0.5f)
        {
            AddOneShotInstanceAndRandomPlay(true, Vector3.zero);
            time = 0;
        }        

        int numberOfInstancePlaying = 0;
        for (int i = 0; i< playerInstances.Length; i++)
        {
            if (playerInstances[i].GetComponent<At_Player>().isPlaying == true)
            {
                numberOfInstancePlaying++;
            }
        }
        
    }
    int getFreeSlot()
    {
        int freeSlotIndex = -1;

        for (int i = 0; i < maxInstance; i++){

            At_Player p = playerInstances[i].GetComponent<At_Player>();

            if (!p.isAskedToPlay)
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
    

    public void AddOneShotInstanceAndRandomPlay(bool isRandomPosition, Vector3 position)
    {
        if (randomPlayerState!=null && randomPlayerState.fileNames != null && randomPlayerState.fileNames.Length != 0 && randomPlayerState.fileNames[0] !="")
        {
           
            int indexinstance = getFreeSlot();
            if (indexinstance == -1)
            {
                // we use the first slot
                indexinstance = getOlder();
                
            }

            playerInstancesCreationTime[indexinstance] = Time.realtimeSinceStartup;

            At_Player p = playerInstances[indexinstance].GetComponent<At_Player>();
            p.StopPlaying();
            if (isRandomPosition)
            {
                float r_angle = Random.Range(spawnMinAngle, spawnMaxAngle) * Mathf.PI / 180f;
                float r_distance = Random.Range(0, spawnDistance);
                playerInstances[indexinstance].transform.position = transform.position + new Vector3(r_distance * Mathf.Cos(r_angle), 0, r_distance * Mathf.Sin(r_angle));

            }
            else
            {
                playerInstances[indexinstance].transform.position = position;
            }
            p.is3D = randomPlayerState.is3D;
            p.isDirective = randomPlayerState.isDirective;
            p.gain = randomPlayerState.gain;
            p.isLooping = false;
            p.omniBalance = randomPlayerState.omniBalance;
            p.attenuation = randomPlayerState.attenuation;

            int r = Random.Range(0, fileNames.Length);

            p.fileName = fileNames[r];


            p.isDynamicInstance = true;
            p.channelRouting = randomPlayerState.channelRouting;
            p.minDistance = randomPlayerState.minDistance;

            p.initAudioFile(true);            
            p.gameObject.SetActive(true);
            p.StartPlaying();

        }

        
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, guid, gameObject.name);//getRandomPlayerStateWithName(gameObject.name);

        float spawnMinAngle = 0, spawnMaxAngle = 0, spawnDistance = 0;

        if (randomPlayerState != null)
        {
            spawnMinAngle = randomPlayerState.spawnMinAngle;
            spawnMaxAngle = randomPlayerState.spawnMaxAngle;
            spawnDistance = randomPlayerState.spawnDistance;
        }

        if (is3D)
        {
 
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
                
                Gizmos.DrawLine(center, nextCenter);
            }
            SceneView.RepaintAll();

        }
    }
#endif
}
