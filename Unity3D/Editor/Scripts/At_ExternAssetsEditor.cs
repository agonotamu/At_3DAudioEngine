using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SFB;

[CanEditMultipleObjects]
[CustomEditor(typeof(At_ExternAssets))]
public class At_ExternAssetsEditor : Editor
{

    // reference to the instance of the At_ExternAssets which has been added 
    At_ExternAssets externAssets;
    string[] externAssetsPaths;

    // Called when the GameObject with the At_ExternAssets component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {
        

        // get a reference to the At_Player isntance (core engine of the player)
        externAssets = (At_ExternAssets)target;
        externAssets.externAssetsPath_audio = PlayerPrefs.GetString("externAssetsPath_audio");
        externAssets.externAssetsPath_state = PlayerPrefs.GetString("externAssetsPath_state");

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
            if (GUILayout.Button("Audio Extern Asset Folder"))
            {
                externAssetsPaths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
                externAssets.externAssetsPath_audio = externAssetsPaths[0];
                PlayerPrefs.SetString("externAssetsPath_audio", externAssetsPaths[0]);
                PlayerPrefs.Save();
            }
        }
        using (new GUILayout.HorizontalScope())
        {
            if (externAssets.externAssetsPath_audio != null && externAssets.externAssetsPath_audio.Length > 0)
            {
                GUILayout.TextArea(externAssets.externAssetsPath_audio);
            }            
        }

        using (new GUILayout.HorizontalScope())
        {
            // Display and test if the Button "Open" has been clicked
            if (GUILayout.Button("States Extern Asset Folder"))
            {
                externAssetsPaths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
                externAssets.externAssetsPath_state = externAssetsPaths[0];
                PlayerPrefs.SetString("externAssetsPath_state", externAssetsPaths[0]);
                PlayerPrefs.Save();
            }
        }
        using (new GUILayout.HorizontalScope())
        {
            if (externAssets.externAssetsPath_state != null && externAssets.externAssetsPath_state.Length > 0)
            {
                GUILayout.TextArea(externAssets.externAssetsPath_state);
            }
        }
    }
}

