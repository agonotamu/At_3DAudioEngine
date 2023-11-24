using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using SFB;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

[ExecuteInEditMode]
[CanEditMultipleObjects]
[CustomEditor(typeof(At_HapticPlayer))]
public class At_HapticPlayerEditor : Editor
{

    // reference to the instance of the At_HapticPlayer which has been added 
    At_HapticPlayer hapticPlayer;

    // name of the GameObject the At_HapticPlayer script is attached to (used as a unique identifier !!!) 
    string gameObjectName;

    // Class use to save/load the state of the player
    At_HapticPlayerState hapticPlayerState;

    //----------------------------------------------------
    // To be serialize for prefab

    // SerializedProperty serialized_crossoverFilterFrequency;

    SerializedProperty serialized_listenerOutputSendGains;

    SerializedProperty serialized_atPlayer_guid;
    SerializedProperty serialized_listenerOutputSendGuids;

    ///type of distance attenuation in the spatialize : 0 = none, 1 = linera, 2 = square
    SerializedProperty serialized_attenuation;
    /// minimum distance above which the sound produced by the source is attenuated
    SerializedProperty serialized_minDistance;

    At_Player atPlayerInput;

    bool shouldSave = false;
    bool previousIsEditor;
    bool previousIsPlaying;
    bool isSceneLoading = false;

    // DRAWING
    // ----- texture for metering ------
    private Texture meterOn;
    private Texture meterOff;


    // A GUIStyle used for drawing lines between main part in th inspector
    GUIStyle horizontalLine;


    At_HapticListenerOutput[] hapticListenerOutputs;

    string[] attenuationType = { "None", "Lin", "Log" };

    // Called when the GameObject with the At_Player component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;


        //serialized_crossoverFilterFrequency = serializedObject.FindProperty("crossoverFilterFrequency");
        
        serialized_atPlayer_guid = serializedObject.FindProperty("atPlayer_guid");
        serialized_listenerOutputSendGains = serializedObject.FindProperty("listenerOutputSendGains");
        serialized_listenerOutputSendGuids = serializedObject.FindProperty("listenerOutputSendGuids");
        serialized_attenuation = serializedObject.FindProperty("attenuation");        
        serialized_minDistance = serializedObject.FindProperty("minDistance");

        // get a reference to the At_Player isntance (core engine of the player)
        hapticPlayer = (At_HapticPlayer)target;


        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;


        // get the name of the GameObject 
        gameObjectName = hapticPlayer.gameObject.name;

        // Get the state of the player
        // - If the GameObject name is found just return the instance in the At_PlayerState List
        // - If the GameObject name is not found, it try loud read a json file for this name.
        //      - if the file is found, it's read, an instance is created and added to the list and is returned
        //      - if the file is not found, create a new "empty" At_PlayerState"
        hapticPlayerState = At_AudioEngineUtils.getHapticPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, hapticPlayer.guid, gameObjectName);

        if (hapticPlayerState == null)
        {

            hapticPlayerState = At_AudioEngineUtils.createNewHapticPlayerStateWithGuidAndName(SceneManager.GetActiveScene().name, hapticPlayer.guid, gameObjectName);
            //hapticState.crossoverFilterFrequency = serialized_crossoverFilterFrequency.floatValue;
            hapticPlayerState.atPlayer_guid = serialized_atPlayer_guid.stringValue;

            hapticPlayerState.attenuation = serialized_attenuation.floatValue;                        
            hapticPlayerState.minDistance = serialized_minDistance.floatValue;

            hapticPlayerState.listenerOutputSendGains = new float[serialized_listenerOutputSendGains.arraySize];
            hapticPlayerState.listenerOutputSendGuids = new string[serialized_listenerOutputSendGuids.arraySize];

            for (int i = 0; i < serialized_listenerOutputSendGains.arraySize; i++)
            {
                SerializedProperty property1 = serialized_listenerOutputSendGains.GetArrayElementAtIndex(i);
                hapticPlayerState.listenerOutputSendGains[i] = property1.floatValue ; // seems to be impossible

                SerializedProperty property2 = serialized_listenerOutputSendGuids.GetArrayElementAtIndex(i);
                hapticPlayerState.listenerOutputSendGuids[i] = property2.stringValue; // seems to be impossible

            }

        }

        hapticListenerOutputs = GameObject.FindObjectsOfType<At_HapticListenerOutput>();


        atPlayerInput = At_AudioEngineUtils.getAtPlayerWithGuid(hapticPlayerState.atPlayer_guid);

        // set all the parameter of the At_HapticPlayer Component from the loaded hapticState (or the newly created one)
        //haptic.crossoverFilterFrequency = hapticState.crossoverFilterFrequency;
        
        hapticPlayer.atPlayer_guid = hapticPlayerState.atPlayer_guid;
        hapticPlayer.atPlayerInput = atPlayerInput;
        hapticPlayer.attenuation = hapticPlayerState.attenuation;        
        hapticPlayer.minDistance = hapticPlayerState.minDistance;


    }


    public void OnDisable()
    {
        if (hapticPlayer != null && hapticPlayer.name != hapticPlayerState.name)
        {
            if (hapticPlayer != null)
            {
                At_AudioEngineUtils.changeHapticPlayerName(SceneManager.GetActiveScene().name, hapticPlayerState.name, hapticPlayerState.name);
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
            if (hapticPlayer == null)
            {
                if (!isSceneLoading)
                {
                    At_AudioEngineUtils.removeHapticPlayerWithGuid(SceneManager.GetActiveScene().name, hapticPlayer.guid);
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

    // load ressources for the GUI (textures, etc.)
    private void AffirmResources()
    {
        if (meterOn == null)
        {
            meterOn = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeter.png", typeof(Texture));
            meterOff = (Texture)AssetDatabase.LoadAssetAtPath("Assets/At_3DAudioEngine/Other/Resources/At_3DAudioEngine/LevelMeterOff.png", typeof(Texture));

        }
    }
    //============================================================================================================================
    //                                                    GUI  DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {

        if (hapticPlayer.name != hapticPlayerState.name)
        {
            At_AudioEngineUtils.changeHapticPlayerName(SceneManager.GetActiveScene().name, hapticPlayerState.name, hapticPlayer.name);
        }

        using (new GUILayout.HorizontalScope())
        {

            atPlayerInput = (At_Player)EditorGUILayout.ObjectField("Audio Receive From", atPlayerInput, typeof(At_Player), true);
            if (atPlayerInput != null)
            {
                hapticPlayerState.atPlayer_guid = atPlayerInput.guid;
                shouldSave = true;
            }

        }
        using (new GUILayout.HorizontalScope())
        {

            GUILayout.Label("GUID");
            if (atPlayerInput != null)
            {
                GUILayout.TextField(atPlayerInput.guid);
            }
            else
            {
                GUILayout.TextField("None");
            }
            
        }

        if (hapticListenerOutputs.Length != 0)
        {
            HorizontalLine(Color.grey);
            for (int i = 0; i < hapticListenerOutputs.Length; i++)
            {
               
                if (hapticPlayerState.listenerOutputSendGains == null 
                    || hapticPlayerState.listenerOutputSendGains.Length == 0
                    || hapticPlayerState.listenerOutputSendGains.Length != hapticListenerOutputs.Length)
                {
                    hapticPlayerState.listenerOutputSendGains = new float[hapticListenerOutputs.Length];
                    hapticPlayerState.listenerOutputSendGuids = new string[hapticListenerOutputs.Length];
                }

                using (new GUILayout.HorizontalScope())
                {
                    hapticPlayerState.listenerOutputSendGuids[i] = hapticListenerOutputs[i].guid;
                    GUILayout.Label("Send  to ");
                    GUILayout.TextField(hapticListenerOutputs[i].gameObject.name);                                                            
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(hapticListenerOutputs[i].guid + ")");
                }
                using (new GUILayout.HorizontalScope())
                {
                    hapticPlayerState.listenerOutputSendGains[i] = GUILayout.HorizontalSlider(hapticPlayerState.listenerOutputSendGains[i], -80f, 10f);
                    GUILayout.TextField(hapticPlayerState.listenerOutputSendGains[i].ToString("0.00") + " dB");
                }
                HorizontalLine(Color.grey);



                shouldSave = true;
               
            }
        }
        else
        {
            GUILayout.TextField("No send available. Add an object with an ''At_HapticListenerOutput'' Component");
        }

        HorizontalLine(Color.grey);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Distance attenuation");
            int selected = EditorGUILayout.Popup(hapticPlayerState.selectedAttenuation, attenuationType);

            if (selected != hapticPlayerState.selectedAttenuation)
            {
                shouldSave = true;
                if (selected == 0) hapticPlayerState.attenuation = 0f;
                if (selected == 1) hapticPlayerState.attenuation = 1f;
                if (selected == 2) hapticPlayerState.attenuation = 2f;

                hapticPlayerState.selectedAttenuation = selected;
            }
        }

        HorizontalLine(Color.grey);

        using (new GUILayout.VerticalScope())
        {
            using (new GUILayout.HorizontalScope())
            {

                GUILayout.Label("Min Distance");

                float dist = GUILayout.HorizontalSlider(hapticPlayerState.minDistance, 0f, 20);

                if (dist != hapticPlayerState.minDistance)
                {
                    hapticPlayerState.minDistance = dist;
                    shouldSave = true;

                    SceneView.RepaintAll();
                }


            }


            GUILayout.TextField((hapticPlayerState.minDistance).ToString("0.00"));

            HorizontalLine(Color.grey);
        }

        // update the parameters in the At_Player component
        //haptic.crossoverFilterFrequency = hapticState.crossoverFilterFrequency;

        hapticPlayer.atPlayer_guid = hapticPlayerState.atPlayer_guid;
        hapticPlayer.listenerOutputSendGains = hapticPlayerState.listenerOutputSendGains;
        hapticPlayer.listenerOutputSendGuids = hapticPlayerState.listenerOutputSendGuids;
        hapticPlayer.attenuation = hapticPlayerState.attenuation;
        hapticPlayer.minDistance = hapticPlayerState.minDistance;

        // serialize the parameters for use in Prefab
        //serialized_crossoverFilterFrequency.floatValue = hapticState.crossoverFilterFrequency ;        
        serialized_atPlayer_guid.stringValue = hapticPlayerState.atPlayer_guid;

        serialized_attenuation.floatValue = hapticPlayer.attenuation;
        serialized_minDistance.floatValue = hapticPlayer.minDistance;

        serialized_listenerOutputSendGains.ClearArray();
        serialized_listenerOutputSendGuids.ClearArray();

        for (int i = 0; i < hapticPlayerState.listenerOutputSendGains.Length; i++)
        {
            
            serialized_listenerOutputSendGains.InsertArrayElementAtIndex(i);
            SerializedProperty property1 = serialized_listenerOutputSendGains.GetArrayElementAtIndex(i);
            property1.floatValue = hapticPlayerState.listenerOutputSendGains[i];
            
            serialized_listenerOutputSendGuids.InsertArrayElementAtIndex(i);
            SerializedProperty property2 = serialized_listenerOutputSendGuids.GetArrayElementAtIndex(i);
            property2.stringValue = hapticPlayerState.listenerOutputSendGuids[i];
            

        }

        // save the Player State to be always updated !!
        serializedObject.ApplyModifiedProperties();
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
