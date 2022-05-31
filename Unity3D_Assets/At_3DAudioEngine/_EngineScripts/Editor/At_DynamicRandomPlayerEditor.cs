using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using SFB;
using UnityEngine.SceneManagement;

[CanEditMultipleObjects]
[CustomEditor(typeof(At_DynamicRandomPlayer))]
public class At_DynamicRandomPlayerEditor : Editor
{
    At_DynamicRandomPlayer randomPlayer;
    List<string> fileNamesList;
    // create your style
    GUIStyle horizontalLine;

    string gameObjectName;
    At_DynamicRandomPlayerState randomPlayerState;
    string[] attenuationType = { "None", "Lin", "Log" };

    string externAssetsPath;

    bool shouldSave = false;

    private bool mRunningInEditor;
    bool previousIsEditor;
    bool previousIsPlaying;
    int currentOutputChannelCount;

    At_OutputState outputState;

    SerializedProperty serialized_fileNames;
    SerializedProperty serialized_gain;
    SerializedProperty serialized_is3D;
    SerializedProperty serialized_isDirective; //modif mathias 06-17-2021
    SerializedProperty serialized_omniBalance;
    SerializedProperty serialized_attenuation;
    // minimum distance above which the sound produced by the source is attenuated
    SerializedProperty serialized_minDistance;
    SerializedProperty serialized_channelRouting;
    SerializedProperty serialized_spawnMinAngle;
    SerializedProperty serialized_spawnMaxAngle;
    SerializedProperty serialized_spawnDistance;
    SerializedProperty serialized_spawnMinRateMs;
    SerializedProperty serialized_spawnMaxRateMs;
    SerializedProperty serialized_maxInstances;

    bool isSceneLoading = false;


    public void OnEnable()
    {

        SceneManager.sceneLoaded += OnSceneLoaded;

        serialized_fileNames = serializedObject.FindProperty("fileNames");
        serialized_gain = serializedObject.FindProperty("gain");
        serialized_is3D = serializedObject.FindProperty("is3D");
        serialized_isDirective = serializedObject.FindProperty("isDirective"); //modif mathias 06-17-2021
        serialized_omniBalance = serializedObject.FindProperty("omniBalance");
        serialized_attenuation = serializedObject.FindProperty("attenuation");
        // minimum distance above which the sound produced by the source is attenuated
        serialized_minDistance = serializedObject.FindProperty("minDistance");
        serialized_channelRouting = serializedObject.FindProperty("channelRouting");
        serialized_spawnMinAngle = serializedObject.FindProperty("spawnMinAngle");
        serialized_spawnMaxAngle = serializedObject.FindProperty("spawnMaxAngle");
        serialized_spawnDistance = serializedObject.FindProperty("spawnDistance");
        serialized_spawnMinRateMs = serializedObject.FindProperty("spawnMinRateMs");
        serialized_spawnMaxRateMs = serializedObject.FindProperty("spawnMaxRateMs");
        serialized_maxInstances = serializedObject.FindProperty("maxInstances");

        previousIsEditor = Application.isEditor;
        previousIsPlaying = Application.isPlaying;

        mRunningInEditor = Application.isEditor && !Application.isPlaying;
        


        //externAssetsPath = GameObject.FindObjectOfType<At_ExternAssets>().externAssetsPath_audio;

        fileNamesList = new List<string>();
        // get a reference to the At_Player isntance (core engine of the player)
        randomPlayer = (At_DynamicRandomPlayer)target;

        gameObjectName = randomPlayer.gameObject.name;

        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;

        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, randomPlayer.guid, gameObjectName);//getRandomPlayerStateWithName(gameObjectName);
        if (randomPlayerState ==null)
        {
           
            randomPlayerState = At_AudioEngineUtils.createNewRandomPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, randomPlayer.guid, gameObjectName);
            
            randomPlayerState.gain = serialized_gain.floatValue;
            randomPlayerState.is3D = serialized_is3D.boolValue;
            randomPlayerState.isDirective = serialized_isDirective.boolValue; //modif mathias 06-17-2021
            randomPlayerState.omniBalance = serialized_omniBalance.floatValue;
            randomPlayerState.attenuation = serialized_attenuation.floatValue;
            // minimum distance above which the sound produced by the source is attenuated
            randomPlayerState.minDistance = serialized_minDistance.floatValue;            
            randomPlayerState.spawnMinAngle = serialized_spawnMinAngle.floatValue;
            randomPlayerState.spawnMaxAngle = serialized_spawnMaxAngle.floatValue;
            randomPlayerState.spawnDistance = serialized_spawnDistance.floatValue;

            randomPlayerState.spawnMinRateMs = serialized_spawnMinRateMs.floatValue;
            randomPlayerState.spawnMaxRateMs = serialized_spawnMaxRateMs.floatValue;
            randomPlayerState.maxInstances = serialized_maxInstances.intValue;

            //randomPlayerState.fileNames = serializedObject.FindProperty("fileNames");
            randomPlayerState.fileNames = new string[serialized_fileNames.arraySize];
            for (int i = 0; i < serialized_fileNames.arraySize; i++)
            {
                SerializedProperty property = serialized_fileNames.GetArrayElementAtIndex(i);
                randomPlayerState.fileNames[i] = property.stringValue; // seems to be impossible

            }
            

            randomPlayerState.channelRouting = new int[serialized_channelRouting.arraySize];

            for (int i = 0; i < serialized_channelRouting.arraySize; i++)
            {
                SerializedProperty property = serialized_channelRouting.GetArrayElementAtIndex(i);
                randomPlayerState.channelRouting[i] = property.intValue; // seems to be impossible

            }

            randomPlayerState.maxChannelInAudiofile = serialized_channelRouting.arraySize;
        }
        
        outputState = At_AudioEngineUtils.getOutputState(SceneManager.GetActiveScene().name);

        if (randomPlayerState.fileNames != null)
        {
            for (int i = 0; i < randomPlayerState.fileNames.Length; i++)
            {
                fileNamesList.Add(randomPlayerState.fileNames[i]);
            }
        }

        randomPlayerState.guid = randomPlayer.guid; 

        randomPlayer.fileNames = randomPlayerState.fileNames;
        randomPlayer.gain = randomPlayerState.gain;
        randomPlayer.is3D = randomPlayerState.is3D;
        randomPlayer.isDirective = randomPlayerState.isDirective;
        randomPlayer.attenuation = randomPlayerState.attenuation;
        randomPlayer.omniBalance = randomPlayerState.omniBalance;
        randomPlayer.minDistance = randomPlayerState.minDistance;
        randomPlayer.spawnMinAngle = randomPlayerState.spawnMinAngle;
        randomPlayer.spawnMaxAngle = randomPlayerState.spawnMaxAngle;
        randomPlayer.spawnDistance = randomPlayerState.spawnDistance;

        randomPlayer.spawnMinRateMs = randomPlayerState.spawnMinRateMs;
        randomPlayer.spawnMaxRateMs = randomPlayerState.spawnMaxRateMs;
        randomPlayer.maxInstances = randomPlayerState.maxInstances;


        randomPlayer.maxChannelsInAudioFile = randomPlayerState.maxChannelInAudiofile;


        currentOutputChannelCount = outputState.outputChannelCount;

        if (randomPlayerState.fileNames != null && randomPlayerState.fileNames.Length != 0)
        {
            if (randomPlayerState.channelRouting == null || randomPlayerState.channelRouting.Length == 0)
            {
                
                currentOutputChannelCount = outputState.outputChannelCount;
                int numChannel = randomPlayerState.maxChannelInAudiofile;
                randomPlayerState.channelRouting = new int[currentOutputChannelCount];
                for (int i = 0; i < currentOutputChannelCount; i++)
                {
                    randomPlayerState.channelRouting[i] = i % numChannel;
                }

            }
        }

        randomPlayer.outputChannelCount = outputState.outputChannelCount;
        randomPlayer.channelRouting = randomPlayerState.channelRouting;

        if (outputState.outputChannelCount != randomPlayerState.channelRouting.Length)
        {
            currentOutputChannelCount = outputState.outputChannelCount;
            int numChannel = randomPlayerState.maxChannelInAudiofile;
            randomPlayerState.channelRouting = new int[currentOutputChannelCount];
            if (numChannel > 0)
            {
                for (int i = 0; i < currentOutputChannelCount; i++)
                {
                    randomPlayerState.channelRouting[i] = i % numChannel;
                }

            }
        }
        At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);

    }

    public void OnDisable()
    {
        if (randomPlayer != null && randomPlayer.name != randomPlayer.name)
        {
            if (randomPlayer != null)
            {  
                At_AudioEngineUtils.changeRandomPlayerName(SceneManager.GetActiveScene().name, randomPlayer.name, randomPlayer.name);
            }
        }
        if (shouldSave)
        {
            At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);
            shouldSave = false;
        }

        if (randomPlayer == null)
        {
           

        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isSceneLoading = true;
    }
    void OnDestroy()
    {
       
        if (Application.isEditor == previousIsEditor && previousIsPlaying == Application.isPlaying)
        {
            if (randomPlayer == null)
            {
                if (!isSceneLoading)
                {
                    
                    At_AudioEngineUtils.removeRandomPlayerWithGuid(SceneManager.GetActiveScene().name, randomPlayer.guid);
                }
                else
                {
                    isSceneLoading = false;
                }
            }
            previousIsEditor = Application.isEditor;
            previousIsPlaying = Application.isPlaying;
        }
    }

    //============================================================================================================================
    //                                                       DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {

        if (randomPlayer.name != randomPlayerState.name)
        {
            
            At_AudioEngineUtils.changeRandomPlayerName(SceneManager.GetActiveScene().name, randomPlayerState.name, randomPlayer.name);


        }

        using (new GUILayout.HorizontalScope())
        {
            bool b = GUILayout.Toggle(randomPlayerState.is3D, "3D");
            if (b != randomPlayerState.is3D)
            {
                shouldSave = true;
                randomPlayerState.is3D = b;

            }
        }
       
        using (new GUILayout.HorizontalScope())
        {
            float g = GUILayout.HorizontalSlider(randomPlayerState.gain, 10f, -80f);

            if (g != randomPlayerState.gain)
            {
                randomPlayerState.gain = g;
                shouldSave = true;
            }
            GUILayout.Label(((int)randomPlayerState.gain).ToString() + " dB");
        
        }
        

        if (randomPlayerState.is3D)
        {
            HorizontalLine(Color.grey);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Distance attenuation");
                int selected = EditorGUILayout.Popup(randomPlayerState.selectedAttenuation, attenuationType);

                if (selected != randomPlayerState.selectedAttenuation)
                {
                    shouldSave = true;
                    if (selected == 0) randomPlayerState.attenuation = 0f;
                    if (selected == 1) randomPlayerState.attenuation = 1f;
                    if (selected == 2) randomPlayerState.attenuation = 2f;

                    randomPlayerState.selectedAttenuation = selected;
                }
            }
            HorizontalLine(Color.grey);

            using (new GUILayout.VerticalScope())
            {

                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Omni");

                    float bal = GUILayout.HorizontalSlider(randomPlayerState.omniBalance, 1f, 0);

                    if (bal != randomPlayerState.omniBalance)
                    {
                        randomPlayerState.omniBalance = bal;
                        shouldSave = true;
                    }

                    GUILayout.Label("Cardioid");

                }


                GUILayout.TextField((randomPlayerState.omniBalance).ToString("0.00"));


            }
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    
                    GUILayout.Label("Min Distance");

                    float dist = GUILayout.HorizontalSlider(randomPlayerState.minDistance, 0f, 20);
                    if (dist != randomPlayerState.minDistance)
                    {
                        shouldSave = true;
                        randomPlayerState.minDistance = dist;
                    }


                }


                GUILayout.TextField((randomPlayerState.minDistance).ToString("0.00"));

                HorizontalLine(Color.grey);
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Spawn min Angle");

                    float angle = GUILayout.HorizontalSlider(randomPlayerState.spawnMinAngle, 0f, 540f);
                    if (angle != randomPlayerState.spawnMinAngle)
                    {
                        
                        shouldSave = true;
                        randomPlayerState.spawnMinAngle = angle;
                        if (randomPlayerState.spawnMinAngle > randomPlayerState.spawnMaxAngle)
                        {
                            randomPlayerState.spawnMaxAngle = randomPlayerState.spawnMinAngle;
                        }
                    }


                }


                GUILayout.TextField((randomPlayerState.spawnMinAngle).ToString("0.00"));

            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Spawn max Angle");

                    float angle = GUILayout.HorizontalSlider(randomPlayerState.spawnMaxAngle, 0f, 540f);
                    if (angle != randomPlayerState.spawnMaxAngle)
                    {
                        shouldSave = true;
                        randomPlayerState.spawnMaxAngle = angle;
                        if (randomPlayerState.spawnMaxAngle < randomPlayerState.spawnMinAngle)
                        {
                            randomPlayerState.spawnMinAngle = randomPlayerState.spawnMaxAngle;
                        }
                    }


                }                

                GUILayout.TextField((randomPlayerState.spawnMaxAngle).ToString("0.00"));
                HorizontalLine(Color.grey);
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Spawn min rate (ms)");

                    float rate = GUILayout.HorizontalSlider(randomPlayerState.spawnMinRateMs, 50f, 5000f);
                    if (rate != randomPlayerState.spawnMinRateMs)
                    {

                        shouldSave = true;
                        randomPlayerState.spawnMinRateMs = rate;
                        if (randomPlayerState.spawnMinRateMs > randomPlayerState.spawnMaxRateMs)
                        {
                            randomPlayerState.spawnMaxRateMs = randomPlayerState.spawnMinRateMs;
                        }
                    }


                }


                GUILayout.TextField((randomPlayerState.spawnMinRateMs).ToString("0.00"));

            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Spawn max rate (ms)");

                    float rate = GUILayout.HorizontalSlider(randomPlayerState.spawnMaxRateMs, 50f, 5000f);
                    if (rate != randomPlayerState.spawnMaxRateMs)
                    {
                        shouldSave = true;
                        randomPlayerState.spawnMaxRateMs = rate;
                        if (randomPlayerState.spawnMaxRateMs < randomPlayerState.spawnMinRateMs)
                        {
                            randomPlayerState.spawnMinRateMs = randomPlayerState.spawnMaxRateMs;
                        }
                    }


                }

                GUILayout.TextField((randomPlayerState.spawnMaxRateMs).ToString("0.00"));
                HorizontalLine(Color.grey);
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Spaw distance");

                    float angle = GUILayout.HorizontalSlider(randomPlayerState.spawnDistance, 0f, 90f);
                    if (angle != randomPlayerState.spawnDistance)
                    {
                        shouldSave = true;
                        randomPlayerState.spawnDistance = angle;   
                    }

                }

                GUILayout.TextField((randomPlayerState.spawnDistance).ToString("0.00"));

                HorizontalLine(Color.grey);
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Max Instances");

                    int max = (int)GUILayout.HorizontalSlider(randomPlayerState.maxInstances, 1, 100);
                    if (max != randomPlayerState.maxInstances)
                    {
                        shouldSave = true;
                        randomPlayerState.maxInstances = max;
                    }

                }

                GUILayout.TextField((randomPlayerState.maxInstances).ToString("0"));

                HorizontalLine(Color.grey);
            }

        }

        HorizontalLine(Color.grey);
        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("Add"))
            {
                
                var extensions = new[] {
                new ExtensionFilter("Sound Files", "mp3", "wav", "aiff", "aac", "mp4"),
                };
                string[] paths;
                paths = StandaloneFileBrowser.OpenFilePanel("Open File", At_AudioEngineUtils.GetFilePathForStates(""), extensions, true);
                randomPlayerState.maxChannelInAudiofile = 0;
                foreach(string s in paths)
                {
                    int numChannel = At_Player.getNumChannelInAudioFile(s);
                    if (randomPlayerState.maxChannelInAudiofile < numChannel)
                    {
                        randomPlayerState.maxChannelInAudiofile = numChannel;
                    }

                    string clean_s = "";
                    if (paths.Length != 0)
                    {
                        string rootPath = At_AudioEngineUtils.GetFilePathForStates("");
                        clean_s = s.Replace("\\", "/");
                        clean_s = clean_s.Replace(rootPath, "");

                    }

                    fileNamesList.Add(clean_s); 
                }
                if (fileNamesList[0] == "")
                {
                    fileNamesList.RemoveAt(0);
                }
                
                currentOutputChannelCount = outputState.outputChannelCount;
                randomPlayerState.channelRouting = new int[currentOutputChannelCount];
                for (int i = 0; i < currentOutputChannelCount; i++)
                {
                    randomPlayerState.channelRouting[i] = i % currentOutputChannelCount;
                }
                
                shouldSave = true;
                UpdateFilenamesInState();
                GUIUtility.ExitGUI();
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("Clear All"))
            {
                shouldSave = true;
                fileNamesList.Clear();
                UpdateFilenamesInState();
            }
        }
        
        HorizontalLine(Color.grey);
        int indexFileToRemove = DisplayFileNameWithClearButton();

        if (indexFileToRemove != -1)
        {
            shouldSave = true;
            fileNamesList.RemoveAt(indexFileToRemove);
            UpdateFilenamesInState();
            DisplayFileNameWithClearButton();
        }


        if (randomPlayerState.is3D == false)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("File channel Routing ");
                
                if (randomPlayerState.maxChannelInAudiofile != 0)
                {
                    if (randomPlayerState.channelRouting != null && randomPlayerState.channelRouting.Length == currentOutputChannelCount)
                    {
                        string[] channelRouting = new string[currentOutputChannelCount+1];
                        for (int i = 0; i < channelRouting.Length; i++)
                        {
                            channelRouting[i] = (i % randomPlayerState.maxChannelInAudiofile).ToString();
                        }
                        channelRouting[currentOutputChannelCount] = "none";

                        int[] selectedChannelRouting = new int[currentOutputChannelCount];
                        for (int i = 0; i < selectedChannelRouting.Length; i++)
                        {
                            selectedChannelRouting[i] = i;
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("-Output-");
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("-Input-");
                            }

                        }
                        for (int c = 0; c < currentOutputChannelCount; c++)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("channel " + c);
                                int select = EditorGUILayout.Popup(randomPlayerState.channelRouting[c], channelRouting);

                                if (select != randomPlayerState.channelRouting[c])
                                {
                                    randomPlayerState.channelRouting[c] = select;
                                    shouldSave = true;
                                }

                            }

                        }
                    }

                }
                
            }
        }


        randomPlayer.fileNames = randomPlayerState.fileNames;
        randomPlayer.gain = randomPlayerState.gain;
        randomPlayer.is3D = randomPlayerState.is3D;
        randomPlayer.isDirective = randomPlayerState.isDirective; 
        randomPlayer.attenuation = randomPlayerState.attenuation;
        randomPlayer.omniBalance = randomPlayerState.omniBalance;
        randomPlayer.maxChannelsInAudioFile = randomPlayerState.maxChannelInAudiofile;
        randomPlayer.channelRouting = randomPlayerState.channelRouting;
        randomPlayer.minDistance = randomPlayerState.minDistance;
        randomPlayer.spawnDistance = randomPlayerState.spawnDistance;
        randomPlayer.spawnMinAngle = randomPlayerState.spawnMinAngle;
        randomPlayer.spawnMaxAngle = randomPlayerState.spawnMaxAngle;
        randomPlayer.spawnMinRateMs = randomPlayerState.spawnMinRateMs;
        randomPlayer.spawnMaxRateMs = randomPlayerState.spawnMaxRateMs;
        randomPlayer.maxInstances = randomPlayerState.maxInstances;

        serialized_gain.floatValue = randomPlayerState.gain;
        serialized_is3D.boolValue = randomPlayerState.is3D;
        serialized_isDirective.boolValue = randomPlayerState.isDirective; 
        serialized_omniBalance.floatValue = randomPlayerState.omniBalance;
        serialized_attenuation.floatValue = randomPlayerState.attenuation;
        // minimum distance above which the sound produced by the source is attenuated
        serialized_minDistance.floatValue = randomPlayerState.minDistance;
        
        serialized_spawnMinAngle.floatValue = randomPlayerState.spawnMinAngle;
        serialized_spawnMaxAngle.floatValue = randomPlayerState.spawnMaxAngle;
        serialized_spawnDistance.floatValue = randomPlayerState.spawnDistance;
        serialized_spawnMinRateMs.floatValue = randomPlayerState.spawnMinRateMs;
        serialized_spawnMaxRateMs.floatValue = randomPlayerState.spawnMaxRateMs;
        serialized_maxInstances.intValue = randomPlayerState.maxInstances;


        serialized_fileNames.ClearArray();
        for (int i = 0; i < randomPlayerState.fileNames.Length; i++)
        {
            serialized_fileNames.InsertArrayElementAtIndex(i);
            SerializedProperty property = serialized_fileNames.GetArrayElementAtIndex(i);
            property.stringValue = randomPlayerState.fileNames[i];
        }


        serialized_channelRouting.ClearArray();
        for (int i = 0; i < randomPlayerState.channelRouting.Length; i++)
        {
            serialized_channelRouting.InsertArrayElementAtIndex(i);
            SerializedProperty property = serialized_channelRouting.GetArrayElementAtIndex(i);
            property.intValue = randomPlayerState.channelRouting[i];
        }

        serializedObject.ApplyModifiedProperties();

    }
    void UpdateFilenamesInState()
    {
        if (fileNamesList.Count == 0)
        {
            randomPlayerState.fileNames = new string[1];
            randomPlayerState.fileNames[0] = "";
        }
        else
        {
            randomPlayerState.fileNames = new string[fileNamesList.Count];
            for (int i = 0; i < fileNamesList.Count; i++)
            {
                randomPlayerState.fileNames[i] = fileNamesList[i];
            }
        }

    }

    int DisplayFileNameWithClearButton()
    {
        int indexFileToRemove = -1;

        if (fileNamesList != null)
        {
            for (int i = 0; i < fileNamesList.Count; i++)
            {
                using (new GUILayout.VerticalScope())
                {
                    if (fileNamesList[i] != "")
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Clear"))
                            {
                                indexFileToRemove = i;
                            }
                            GUILayout.TextArea(fileNamesList[i]);
                            // Display and test if the Button "Open" has been clicked
                            
                        }

                    }
                }
            }
        }
        return indexFileToRemove;
    }
    // utility method
    void HorizontalLine(Color color)
    {
        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLine);
        GUI.color = c;
    }

}