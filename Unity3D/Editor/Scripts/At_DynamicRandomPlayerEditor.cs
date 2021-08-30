using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using SFB;

[CanEditMultipleObjects]
[CustomEditor(typeof(At_DynamicRandomPlayer))]
public class At_DynamicRandomPlayerEditor : Editor
{
    At_DynamicRandomPlayer randomPlayer;
    List<string> fileNames;
    // create your style
    GUIStyle horizontalLine;

    string gameObjectName;
    At_DynamicRandomPlayerState randomPlayerState;
    string[] attenuationType = { "None", "Lin", "Log" };

    string externAssetsPath;

    bool shouldSave = false;

    public void OnEnable()
    {

        externAssetsPath = GameObject.FindObjectOfType<At_ExternAssets>().externAssetsPath_audio;

        fileNames = new List<string>();
        // get a reference to the At_Player isntance (core engine of the player)
        randomPlayer = (At_DynamicRandomPlayer)target;
        gameObjectName = randomPlayer.gameObject.name;

        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;

        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithName(gameObjectName);
        At_OutputState outputState = At_AudioEngineUtils.getOutputState();
        //randomPlayerState.numChannelInAudiofile = outputState.outputChannelCount;

        if (randomPlayerState.fileNames != null)
        {
            for (int i = 0; i < randomPlayerState.fileNames.Length; i++)
            {
                fileNames.Add(randomPlayerState.fileNames[i]);
            }
        }

        randomPlayer.fileNames = randomPlayerState.fileNames;
        randomPlayer.gain = randomPlayerState.gain;
        randomPlayer.is3D = randomPlayerState.is3D;
        randomPlayer.isDirective = randomPlayerState.isDirective; //modif mathias 06-17-2021
        randomPlayer.attenuation = randomPlayerState.attenuation;
        randomPlayer.omniBalance = randomPlayerState.omniBalance;
        randomPlayer.minDistance = randomPlayerState.minDistance;
        randomPlayer.spawnMinAngle = randomPlayerState.spawnMinAngle;
        randomPlayer.spawnMaxAngle = randomPlayerState.spawnMaxAngle;
        randomPlayer.spawnDistance = randomPlayerState.spawnDistance;

        randomPlayer.maxChannelsInAudioFile = randomPlayerState.maxChannelInAudiofile;
        //randomPlayer.numChannelInAudiofile = randomPlayerState.numChannelInAudiofile;
        //randomPlayer.state = randomPlayerState;

        if (randomPlayerState.fileNames != null && randomPlayerState.fileNames.Length != 0)
        {
            if (randomPlayerState.channelRouting == null || randomPlayerState.channelRouting.Length == 0)//|| player.outputChannelCount != outputState.outputChannelCount)
            {
                //currentOutputChannelCount = outputState.outputChannelCount;
                //int numChannel = At_PL.getNumChannelInAudioFile();
                randomPlayerState.channelRouting = new int[randomPlayerState.maxChannelInAudiofile];
                for (int i = 0; i < randomPlayerState.maxChannelInAudiofile; i++)
                {
                    randomPlayerState.channelRouting[i] = i;
                }

            }
        }

        randomPlayer.outputChannelCount = outputState.outputChannelCount;
        randomPlayer.channelRouting = randomPlayerState.channelRouting;

    }

    public void OnDisable()
    {
        if (shouldSave)
        {
            //At_AudioEngineUtils.SaveRandomPlayerStateWithName(randomPlayer.name); modif mathias 30-06-202
            At_AudioEngineUtils.SaveAllState();
            shouldSave = false;
        }
    }

    //============================================================================================================================
    //                                                       DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {

        if (randomPlayer.name != randomPlayerState.name)
        {
            Debug.Log(randomPlayerState.name + " as changed to : " + randomPlayer.name);
            At_AudioEngineUtils.changeRandomPlayerName(randomPlayerState.name, randomPlayer.name);


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
        //modif mathias 06-17-2021
        /*
        if (randomPlayerState.is3D == true)
        {
            using (new GUILayout.HorizontalScope())
            {
                bool b = GUILayout.Toggle(randomPlayerState.isDirective, "Directive");
                if (b != randomPlayerState.isDirective)
                {
                    shouldSave = true;
                    randomPlayerState.isDirective = b;

                }
            }
        }
        */

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

        }

        

        /*
        HorizontalLine(Color.grey);
        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("Add"))
            {
                shouldSave = true;
                string audioFilePath;
                string[] filter = { "Audio File", "wav,aiff, mp3, aac, mp4" };
                // if it is, open the panel for choosing an audio file
                audioFilePath = EditorUtility.OpenFilePanelWithFilters("Open Audio File", Application.dataPath + "/StreamingAssets/", filter);

                string s = audioFilePath.Replace(Application.dataPath + "/StreamingAssets/", "");
                fileNames.Add(s);
                if (fileNames[0] == "")
                {
                    fileNames.RemoveAt(0);
                }
                UpdateFilenamesInState();

            }
        }
        */
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
                paths = StandaloneFileBrowser.OpenFilePanel("Open File", externAssetsPath, extensions, true);
                randomPlayerState.maxChannelInAudiofile = 0;
                foreach(string s in paths)
                {
                    int numChannel = At_Player.getNumChannelInAudioFile(s);
                    if (randomPlayerState.maxChannelInAudiofile < numChannel)
                    {
                        randomPlayerState.maxChannelInAudiofile = numChannel;
                    }
                    fileNames.Add(s.Replace(externAssetsPath, "")); 
                }
                if (fileNames[0] == "")
                {
                    fileNames.RemoveAt(0);
                }
                randomPlayerState.channelRouting = new int[randomPlayerState.maxChannelInAudiofile];
                for (int i = 0; i < randomPlayerState.maxChannelInAudiofile; i++)
                {
                    randomPlayerState.channelRouting[i] = i;
                }
                /*
                string audioFilePath;
                string[] filter = { "Audio File", "wav,aiff, mp3, aac, mp4" };
                // if it is, open the panel for choosing an audio file
                audioFilePath = EditorUtility.OpenFilePanelWithFilters("Open Audio File", Application.dataPath + "/StreamingAssets/", filter);

                string s = audioFilePath.Replace(Application.dataPath + "/StreamingAssets/", "");
                fileNames.Add(s);
                if (fileNames[0] == "")
                {
                    fileNames.RemoveAt(0);
                }
                */
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
                fileNames.Clear();
                UpdateFilenamesInState();
            }
        }
        
        HorizontalLine(Color.grey);
        int indexFileToRemove = DisplayFileNameWithClearButton();

        if (indexFileToRemove != -1)
        {
            shouldSave = true;
            fileNames.RemoveAt(indexFileToRemove);
            UpdateFilenamesInState();
            DisplayFileNameWithClearButton();
        }


        if (randomPlayerState.is3D == false)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("File channel Routing ");
                if (randomPlayerState.channelRouting != null && randomPlayerState.channelRouting.Length == randomPlayerState.maxChannelInAudiofile)
                {
                    string[] channelRouting = new string[randomPlayer.outputChannelCount];
                    for (int i = 0; i < channelRouting.Length; i++)
                    {
                        channelRouting[i] = i.ToString();
                    }
                    int[] selectedChannelRouting = new int[randomPlayerState.maxChannelInAudiofile];
                    for (int i = 0; i < selectedChannelRouting.Length; i++)
                    {
                        selectedChannelRouting[i] = i;
                    }

                    for (int c = 0; c < randomPlayerState.maxChannelInAudiofile; c++)
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


        randomPlayer.fileNames = randomPlayerState.fileNames;
        randomPlayer.gain = randomPlayerState.gain;
        randomPlayer.is3D = randomPlayerState.is3D;
        randomPlayer.isDirective = randomPlayerState.isDirective; //modif mathias 06-17-2021
        randomPlayer.attenuation = randomPlayerState.attenuation;
        randomPlayer.omniBalance = randomPlayerState.omniBalance;
        randomPlayer.maxChannelsInAudioFile = randomPlayerState.maxChannelInAudiofile;
        randomPlayer.channelRouting = randomPlayerState.channelRouting;
        randomPlayer.minDistance = randomPlayerState.minDistance;
        randomPlayer.spawnDistance = randomPlayerState.spawnDistance;
        randomPlayer.spawnMinAngle = randomPlayerState.spawnMinAngle;
        randomPlayer.spawnMaxAngle = randomPlayerState.spawnMaxAngle;

        //randomPlayer.state = randomPlayerState; 



    }
    void UpdateFilenamesInState()
    {
        if (fileNames.Count == 0)
        {
            randomPlayerState.fileNames = new string[1];
            randomPlayerState.fileNames[0] = "";
        }
        else
        {
            randomPlayerState.fileNames = new string[fileNames.Count];
            for (int i = 0; i < fileNames.Count; i++)
            {
                randomPlayerState.fileNames[i] = fileNames[i];
            }
        }

    }

    int DisplayFileNameWithClearButton()
    {
        int indexFileToRemove = -1;

        if (fileNames != null)
        {
            for (int i = 0; i < fileNames.Count; i++)
            {
                using (new GUILayout.VerticalScope())
                {
                    if (fileNames[i] != "")
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Clear"))
                            {
                                indexFileToRemove = i;
                            }
                            GUILayout.TextArea(fileNames[i]);
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