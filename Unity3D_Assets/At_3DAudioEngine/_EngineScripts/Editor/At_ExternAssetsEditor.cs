using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SFB;

[CanEditMultipleObjects]
[CustomEditor(typeof(At_ExternAssets))]
public class At_ExternAssetsEditor : Editor
{
    // A GUIStyle used for drawing lines between main part in th inspector
    GUIStyle horizontalLine;

    // reference to the instance of the At_ExternAssets which has been added 
    At_ExternAssets externAssets;
    string[] externAssetsPaths;

    // Class use to save/load the state of the player
    At_ExternAssetsState externAssetsState;

    // Called when the GameObject with the At_ExternAssets component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {

        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;


        externAssetsState = At_AudioEngineUtils.getExternalAssetsState();


        // get a reference to the At_Player isntance (core engine of the player)
        externAssets = (At_ExternAssets)target;
        if (externAssetsState!= null)
        {
            externAssets.externAssetsPath_audio = externAssetsState.externAssetsPath_audio;
            externAssets.externAssetsPath_state = externAssetsState.externAssetsPath_state;

            externAssets.externAssetsPath_audio_standalone = externAssetsState.externAssetsPath_audio_standalone;
            externAssets.externAssetsPath_state_standalone = externAssetsState.externAssetsPath_state_standalone;
        }
        



    }

    public void OnDisable()
    {
    }

    //============================================================================================================================
    //                                                       DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            //if (GUILayout.Button("Audio Extern Asset Folder (Editor)"))
            if (GUILayout.Button("Audio Folder"))
            {
                externAssetsPaths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
                externAssetsState.externAssetsPath_audio = externAssetsPaths[0];
                externAssetsState.externAssetsPath_audio_standalone = externAssetsState.externAssetsPath_audio;
                //PlayerPrefs.SetString("externAssetsPath_audio", externAssetsPaths[0]);
                //Debug.Log("set externAssetsPath_audio : " + externAssetsPaths[0]);
                //PlayerPrefs.Save();
            }
        }
        using (new GUILayout.HorizontalScope())
        {
            if (externAssetsState != null && externAssetsState.externAssetsPath_audio != null && externAssetsState.externAssetsPath_audio.Length > 0)
            {
                GUILayout.TextArea(externAssetsState.externAssetsPath_audio);
            }            
        }

        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("States Folder"))
            {
                externAssetsPaths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
                externAssetsState.externAssetsPath_state = externAssetsPaths[0];
                externAssetsState.externAssetsPath_state_standalone = externAssetsState.externAssetsPath_state;
                //PlayerPrefs.SetString("externAssetsPath_state", externAssetsPaths[0]);
                //Debug.Log("set externAssetsPath_state : " + externAssetsPaths[0]);
                //PlayerPrefs.Save();
            }
        }
        using (new GUILayout.HorizontalScope())
        {
            if (externAssetsState != null && externAssetsState.externAssetsPath_state != null && externAssetsState.externAssetsPath_state.Length > 0)
            {
                GUILayout.TextArea(externAssetsState.externAssetsPath_state);
            }
        }
        /*
        HorizontalLine(Color.grey);

        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("Audio Extern Asset Folder (Standalone)"))
            {
                externAssetsPaths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
                externAssetsState.externAssetsPath_audio_standalone = externAssetsPaths[0];

            }
        }
        using (new GUILayout.HorizontalScope())
        {
            if (externAssetsState != null && externAssetsState.externAssetsPath_audio != null && externAssetsState.externAssetsPath_audio.Length > 0)
            {
                GUILayout.TextArea(externAssetsState.externAssetsPath_audio_standalone);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("States Extern Asset Folder (Standalone)"))
            {
                externAssetsPaths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
                externAssetsState.externAssetsPath_state_standalone = externAssetsPaths[0];
                
            }
        }
        using (new GUILayout.HorizontalScope())
        {
            if (externAssetsState != null && externAssetsState.externAssetsPath_state != null && externAssetsState.externAssetsPath_state.Length > 0)
            {
                GUILayout.TextArea(externAssetsState.externAssetsPath_state_standalone);
            }
        }

        */
        if (externAssetsState != null)
        {
            externAssets.externAssetsPath_audio = externAssetsState.externAssetsPath_audio;
            externAssets.externAssetsPath_state = externAssetsState.externAssetsPath_state;
            externAssets.externAssetsPath_audio_standalone = externAssetsState.externAssetsPath_audio_standalone;
            externAssets.externAssetsPath_state_standalone = externAssetsState.externAssetsPath_state_standalone;

            At_AudioEngineUtils.saveExternAssetsState();
        }

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

