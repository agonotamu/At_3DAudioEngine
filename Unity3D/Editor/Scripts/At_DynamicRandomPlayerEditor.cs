using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


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

    public void OnEnable()
    {
       

        fileNames = new List<string>();
        // get a reference to the At_Player isntance (core engine of the player)
        randomPlayer = (At_DynamicRandomPlayer)target;
        gameObjectName = randomPlayer.gameObject.name;

        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;

        randomPlayerState = At_AudioEngineUtils.getRandomPlayerStateWithName(gameObjectName);

        for (int i = 0; i < randomPlayerState.fileNames.Length; i++)//modif mathias 07-02-2021
        {
            fileNames.Add(randomPlayerState.fileNames[i]);//modif mathias 07-02-2021
        }

        randomPlayer.fileNames = randomPlayerState.fileNames;
        randomPlayer.gain = randomPlayerState.gain;
        randomPlayer.is3D = randomPlayerState.is3D;
        randomPlayer.isDirective = randomPlayerState.isDirective; //modif mathias 06-17-2021
        randomPlayer.attenuation = randomPlayerState.attenuation;
        randomPlayer.omniBalance = randomPlayerState.omniBalance;
        //randomPlayer.state = randomPlayerState;

    }

    public void OnDisable()
    {

    }

    //============================================================================================================================
    //                                                       DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {
        bool shouldSave = false;

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
        else //modif mathias 07-01-2021
        {
            randomPlayerState.isDirective = false;
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

        
        randomPlayer.fileNames = randomPlayerState.fileNames;
        randomPlayer.gain = randomPlayerState.gain;
        randomPlayer.is3D = randomPlayerState.is3D;
        randomPlayer.isDirective = randomPlayerState.isDirective; //modif mathias 06-17-2021
        randomPlayer.attenuation = randomPlayerState.attenuation;
        randomPlayer.omniBalance = randomPlayerState.omniBalance;
        //randomPlayer.state = randomPlayerState; 

        if (shouldSave)
        {
            //At_AudioEngineUtils.SaveRandomPlayerStateWithName(randomPlayer.name); modif mathias 30-06-202
            At_AudioEngineUtils.SaveAllState();
        }

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