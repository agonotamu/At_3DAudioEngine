/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 21/11/2023
 * 
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using SFB;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;
using System.Globalization;


[ExecuteInEditMode]
[CanEditMultipleObjects]
[CustomEditor(typeof(At_HapticListenerOutput))]
public class At_HapticListenerOutputEditor : Editor
{

    // reference to the instance of the At_Player which has been added 
    At_HapticListenerOutput hapticListenerOutput;
    // name of the GameObject the At_Player script is attached to (used as a unique identifier !!!) 
    string gameObjectName;

    
    // Class use to save/load the state of the player
    At_HapticListenerOutputState hapticListenerOutputState;

    // A GUIStyle used for drawing lines between main part in th inspector
    GUIStyle horizontalLine;

    bool shouldSave = false;

    private bool mRunningInEditor;

    bool previousIsEditor;
    bool previousIsPlaying;

    string[] outputConfigSelection = { "select", "OMNI (1 Channel)", "QUAD (4 Channels)"};
    int selectSpeakerConfig = 0;

    //----------------------------------------------------
    // To be serialize for prefab
    /// name of the audio file to play (Supposed to be in "Assets\Streaming Asset")

    SerializedProperty serialized_gain;
    SerializedProperty serialized_outputChannelCount;


   
    bool isSceneLoading = false;

    // ----- texture for metering ------
    private Texture meterOn;
    private Texture meterOff;
    public float[] meters;
    
    

    // load ressources for the GUI (textures, etc.)
    private void AffirmResources()
    {
        if (meterOn == null)
        {
            meterOn = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeter.png", typeof(Texture));
            meterOff = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeterOff.png", typeof(Texture));
        }
    }


    // Called when the GameObject with the At_Player component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // get the serialized property (in case the At_Player has been added in a prefab)
        serialized_gain = serializedObject.FindProperty("gain");
        serialized_outputChannelCount = serializedObject.FindProperty("outputChannelCount");

        previousIsEditor = Application.isEditor;
        previousIsPlaying = Application.isPlaying;

        // get a reference to the At_Player isntance (core engine of the player)
        hapticListenerOutput = (At_HapticListenerOutput)target;

        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;

        // get the name of the GameObject 
        gameObjectName = hapticListenerOutput.gameObject.name;

        // Get the state of the hapticListenerOutput
        // - If the GameObject name is found just return the instance in the At_HapticListenerOutputState List
        // - If the GameObject name is not found, it try loud read a json file for this name.
        //      - if the file is found, it's read, an instance is created and added to the list and is returned
        //      - if the file is not found, create a new "empty" At_HapticListenerOutputState"

        // TODO
        //hapticListenerOutputState = At_AudioEngineUtils.getPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, player.guid, gameObjectName);
        hapticListenerOutputState = At_AudioEngineUtils.getHapticListenerOutputStateWithGuidAndName(SceneManager.GetActiveScene().name, hapticListenerOutput.guid, gameObjectName);


        if (hapticListenerOutputState == null)
        {
            // TODO
            //hapticListenerOutputState = At_AudioEngineUtils.createNewPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, player.guid, gameObjectName);
            hapticListenerOutputState = At_AudioEngineUtils.createNewHapticListenerOutputStateWithGuidAndName(SceneManager.GetActiveScene().name, hapticListenerOutput.guid, gameObjectName);

            hapticListenerOutputState.gain = serialized_gain.floatValue;
            hapticListenerOutputState.outputChannelCount = serialized_outputChannelCount.intValue;

        }
        else
        {
            selectSpeakerConfig = hapticListenerOutputState.selectSpeakerConfig;
        }

        // set all the parameter of the At_HapticListenerOutput Component from the loaded At_HapticListenerOutputState (or the newly created one)
        hapticListenerOutput.gain = hapticListenerOutputState.gain;
        hapticListenerOutput.outputChannelCount = hapticListenerOutputState.outputChannelCount;



        //At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);
    }
    public void OnDisable()
    {
        if (hapticListenerOutput != null && hapticListenerOutput.name != hapticListenerOutput.name)
        {
            if (hapticListenerOutput != null)
            {
                // TODO
                // At_AudioEngineUtils.changePlayerName(SceneManager.GetActiveScene().name, playerState.name, player.name);
                At_AudioEngineUtils.changeHapticListernerOutputName(SceneManager.GetActiveScene().name, hapticListenerOutputState.name, hapticListenerOutput.name);
            }
        }
        // SAVE ALL THE PROPERTIES OF THE PLAYER WHEN THE GUI IS DISABLED (i.e. the GameObject is not in focus anymore : quit, play, another object selected...)
        if (shouldSave)
        {
            At_AudioEngineUtils.SaveAllState(SceneManager.GetActiveScene().name);
            shouldSave = false;
        }
        
        At_AudioEngineUtils.CleanAllStates(SceneManager.GetActiveScene().name);

    }


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isSceneLoading = true;
    }

    void OnDestroy()
    {

        if (Application.isEditor == previousIsEditor && previousIsPlaying == Application.isPlaying)
        {
            if (hapticListenerOutput == null)
            {
                if (!isSceneLoading)
                {
                    // TODO
                    //At_AudioEngineUtils.removePlayerWithGuid(SceneManager.GetActiveScene().name, player.guid);
                    At_AudioEngineUtils.removeHapticListenerOutputWithGuid(SceneManager.GetActiveScene().name, hapticListenerOutput.guid);
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

    public override void OnInspectorGUI()
    {

        AffirmResources();


        if (hapticListenerOutput.name != hapticListenerOutputState.name)
        {
            At_AudioEngineUtils.changeHapticListernerOutputName(SceneManager.GetActiveScene().name, hapticListenerOutputState.name, hapticListenerOutput.name);
        }

        GUILayout.Label("Configuration");
        selectSpeakerConfig = EditorGUILayout.Popup(selectSpeakerConfig, outputConfigSelection);
        if (selectSpeakerConfig != 0)
        {
            hapticListenerOutputState.selectSpeakerConfig = selectSpeakerConfig;
            if (selectSpeakerConfig == 1)
                hapticListenerOutputState.outputChannelCount = 1;
            else if (selectSpeakerConfig == 2)
                hapticListenerOutputState.outputChannelCount = 4;
           
            
            meters = new float[hapticListenerOutputState.outputChannelCount];

            shouldSave = true;
        }

        //-----------------------------------------------------------------
        // MASTER METERING AND GAIN
        //----------------------------------------------------------------


        // TODO
        //GUILayout.Label("Haptic Listener Channels Output : [N - M]" );

        using (new GUILayout.HorizontalScope())
        {
            
            if (hapticListenerOutput.meters != null)
            {
                meters = hapticListenerOutput.meters;
            }
            float baseX = 0;
            if (meters != null)
                baseX = DisplayMetering(meters, hapticListenerOutputState.outputChannelCount);
            float sliederHeight = 84;
            

            float g = GUI.VerticalSlider(new Rect(baseX - 20, 45, 80, sliederHeight), hapticListenerOutputState.gain, 10f, -80f);
            //float g = GUILayout.HorizontalSlider(hapticListenerOutputState.gain, 10f, -80f);
            if (g != hapticListenerOutputState.gain)
            {
                hapticListenerOutputState.gain = g;
                shouldSave = true;
            }

            GUI.Label(new Rect(baseX - 30, 100, 80, sliederHeight), ((int)hapticListenerOutputState.gain).ToString() + " dB");
            //GUILayout.Label(((int)hapticListenerOutputState.gain).ToString() + " dB");


        }

        GUILayout.Label("");

        // update the parameters in the At_HapticListenerOutput component
        hapticListenerOutput.gain = hapticListenerOutputState.gain;
        hapticListenerOutput.outputChannelCount = hapticListenerOutputState.outputChannelCount;

        // serialize the parameters for use in Prefab
        serialized_gain.floatValue = hapticListenerOutputState.gain;
        serialized_outputChannelCount.intValue = hapticListenerOutputState.outputChannelCount;
       
        // save the Player State to be always updated !!
        serializedObject.ApplyModifiedProperties();
    }

    void HorizontalLine(Color color)
    {
        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLine);
        GUI.color = c;
    }
    
    // ------------------------------------- DRAWING UTILITY --------------------------------------------
    float DisplayMetering(float[] metering, int numChannel)
    {
        const int MeterCountMaximum = 48;
        int meterHeight = 86;
        int meterWidth = (int)((128 / (float)meterOff.height) * meterOff.width);

        int minimumWidth = meterWidth * MeterCountMaximum;

        Rect fullRect = GUILayoutUtility.GetRect(minimumWidth / 4.0f, meterHeight,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        float baseX = fullRect.x + (fullRect.width - (meterWidth * metering.Length)) / 2;

        for (int i = 0; i < numChannel; i++)
        {
            Rect meterRect = new Rect(baseX + meterWidth * i, fullRect.y, meterWidth, fullRect.height);

            GUI.DrawTexture(meterRect, meterOff);
            float db = -86;
            if (i < metering.Length)
            {
                db = 20.0f * Mathf.Log10(metering[i] * Mathf.Sqrt(2.0f));
                db = Mathf.Clamp(db, -80.0f, 10.0f);
            }

            float visible = 0;
            int[] segmentPixels = new int[] { 0, 18, 38, 60, 89, 130, 187, 244, 300 };
            float[] segmentDB = new float[] { -80.0f, -60.0f, -50.0f, -40.0f, -30.0f, -20.0f, -10.0f, 0, 10.0f };
            int segment = 1;
            while (segmentDB[segment] < db)
            {
                segment++;
            }
            visible = segmentPixels[segment - 1] + ((db - segmentDB[segment - 1]) / (segmentDB[segment] - segmentDB[segment - 1])) * (segmentPixels[segment] - segmentPixels[segment - 1]);

            visible *= fullRect.height / (float)meterOff.height;

            Rect levelPosRect = new Rect(meterRect.x, fullRect.height - visible + meterRect.y, meterWidth, visible);
            Rect levelUVRect = new Rect(0, 0, 1.0f, visible / fullRect.height);
            
            GUI.DrawTextureWithTexCoords(levelPosRect, meterOn, levelUVRect);
        }

        return baseX;


    }
    
}
