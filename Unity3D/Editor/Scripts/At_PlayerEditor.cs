/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * DESCRIPTION : class used to defined and draw the extended GUI of the PlayerState Inspector
 * - Add a Player State in the list when then At_Player is added 
 * - Update the parameters of an existing Player States in the list when it is changed (path, gain, etc.)
 * - Display the peak level (rms value for buffer size in dB full scale) for each channel of the audiofile
 * -----------------------------------------------------------------------------------------------------------------------
 * DEBUG NOTE: PB REMOVE !!
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


[CanEditMultipleObjects]
[CustomEditor(typeof(At_Player))]
public class At_PlayerEditor : Editor
{
    // reference to the instance of the At_Player which has been added 
    At_Player player;
    // name of the GameObject the At_Player script is attached to (used as a unique identifier !!!) 
    string gameObjectName;

    // DRAWING
    // ----- texture for metering ------
    private Texture meterOn;
    private Texture meterOff;

    // Class use to save/load the state of the player
    At_PlayerState playerState;

    // A GUIStyle used for drawing lines between main part in th inspector
    GUIStyle horizontalLine;

    string[] attenuationType = { "None", "Lin", "Log" };
    
    bool shouldSave = false;



    // Called when the GameObject with the At_Player component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        // get a reference to the At_Player isntance (core engine of the player)
        player = (At_Player)target;

        if (!player.isDynamicInstance)
        {
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            // get the name of the GameObject 
            gameObjectName = player.gameObject.name;

            // Get the state of the player
            // - If the GameObject name is found just return the instance in the At_PlayerState List
            // - If the GameObject name is not found, it try loud read a json file for this name.
            //      - if the file is found, it's read, an instance is created and added to the list and is returned
            //      - if the file is not found, create a new "empty" At_PlayerState"
            playerState = At_AudioEngineUtils.getPlayerStateWithName(gameObjectName);

            // set all the

            player.fileName = playerState.fileName;
            player.gain = playerState.gain;
            player.is3D = playerState.is3D;
            player.isDirective = playerState.isDirective; //modif mathias 06-17-2021
            player.isLooping = playerState.isLooping;
            player.isPlayingOnAwake = playerState.isPlayingOnAwake;
            player.attenuation = playerState.attenuation;
            player.omniBalance = playerState.omniBalance;
            player.timeReversal = playerState.timeReversal;
            player.minDistance = playerState.minDistance;
            //player.state = playerState;
        }



    }


    public void OnDisable()
    {

        if (shouldSave)
        {
            //At_AudioEngineUtils.SavePlayerStateWithName(playerState.name); modif mathias 30-06-202
            At_AudioEngineUtils.SaveAllState();
            shouldSave = false;
        }

        if (player == null && !Application.isPlaying)
        {
            // BUG - THIS IS CALL WHEN PLAY MODE IS ACTIVE !!!
            /*
            Debug.Log("remove State !");
            // the component has been removed or the GameObject has been destroyed
            // so remove the PlayerState for this object
            At_AudioEngineUtils.getPlayerState().removePlayerState(gameObjectName);
            // save the Player State to be always updated !!
            At_AudioEngineUtils.SavePlayerState();
            */
        }
        //Debug.Log("save players state on disable");
        //At_AudioEngineUtils.SaveState("players");
    }


    public void Start()
    {
        Debug.Log("Start Game");
    }

    // load ressources for the GUI (textures, etc.)
    private void AffirmResources()
    {
        if (meterOn == null)
        {
            meterOn = EditorGUIUtility.Load("/At_3DAudioEngine/LevelMeter.png") as Texture;
            meterOff = EditorGUIUtility.Load("/At_3DAudioEngine/LevelMeterOff.png") as Texture;
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        const float numStepDrawCircle = 20;
        float angle = 2 * Mathf.PI / numStepDrawCircle;

        for (int i = 0; i < numStepDrawCircle; i++)
        {            
            Vector3 pos1 = player.gameObject.transform.position + new Vector3(playerState.minDistance * Mathf.Cos(i*angle), 0, playerState.minDistance * Mathf.Sin(i * angle)) ;
            Vector3 pos2 = player.gameObject.transform.position + new Vector3(playerState.minDistance * Mathf.Cos((i+1) * angle), 0, playerState.minDistance * Mathf.Sin((i+1) * angle)); ;
            Debug.DrawLine(pos1, pos2, Color.green);
        }
        
    }
    //============================================================================================================================
    //                                                       DRAWING
    //============================================================================================================================
    public override void OnInspectorGUI()
    {
        if (!player.isDynamicInstance)
        {
            //bool shouldSave = false;

            // laod ressources if needed
            AffirmResources();

            string audioFilePath = "";
            using (new GUILayout.HorizontalScope())
            {
                // Display and test if the Button "Open" has been clicked
                if (GUILayout.Button("Open"))
                {
                    string[] filter = { "Audio File", "wav,aiff, mp3, aac, mp4" };
                    // if it is, open the panel for choosing an audio file
                    audioFilePath = EditorUtility.OpenFilePanelWithFilters("Open Audio File", Application.dataPath + "/StreamingAssets/", filter);

                    string s = audioFilePath.Replace(Application.dataPath + "/StreamingAssets/", "");
                    if (s != playerState.fileName)
                    {
                        // Get the filename without complete path
                        playerState.fileName = s;
                        shouldSave = true;
                    }
                    // update the Player State in the list with the file name
                    //At_AudioEngineUtils.getState().addFileNameToPlayerState(gameObjectName, playerState.fileName);
                    // update the Player Engine the file name


                    player.initMeters();
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (playerState.fileName != "")
                {
                    GUILayout.TextArea(player.fileName);
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                bool b = GUILayout.Toggle(playerState.is3D, "3D");
                if (b != playerState.is3D)
                {
                    shouldSave = true;
                    playerState.is3D = b;

                }

                b = GUILayout.Toggle(playerState.isPlayingOnAwake, "Play On Awake");
                if (b != playerState.isPlayingOnAwake)
                {
                    shouldSave = true;
                    playerState.isPlayingOnAwake = b;

                }
                b = GUILayout.Toggle(playerState.isLooping, "Loop");
                if (b != playerState.isLooping)
                {
                    shouldSave = true;
                    playerState.isLooping = b;

                }
            }
            //modif mathias 06-17-2021
            if (playerState.is3D == true)
            {
                using (new GUILayout.HorizontalScope())
                {
                    bool b = GUILayout.Toggle(playerState.isDirective, "Directive");
                    if (b != playerState.isDirective)
                    {
                        shouldSave = true;
                        playerState.isDirective = b;

                    }
                }
            }

            HorizontalLine(Color.grey);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Distance attenuation");
                int selected = EditorGUILayout.Popup(playerState.selectedAttenuation, attenuationType);

                if (selected != playerState.selectedAttenuation)
                {
                    shouldSave = true;
                    if (selected == 0) playerState.attenuation = 0f;
                    if (selected == 1) playerState.attenuation = 1f;
                    if (selected == 2) playerState.attenuation = 2f;

                    playerState.selectedAttenuation = selected;
                }
            }
            HorizontalLine(Color.grey);

            using (new GUILayout.VerticalScope())
            {               
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Omni");

                    float bal = GUILayout.HorizontalSlider(playerState.omniBalance, 1f, 0);

                    if (bal != playerState.omniBalance)
                    {
                        playerState.omniBalance = bal;
                        shouldSave = true;
                    }
                    GUILayout.Label("Cardioid");

                }


                GUILayout.TextField((playerState.omniBalance).ToString("0.00"));

                HorizontalLine(Color.grey);
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Min Distance");

                    float dist = GUILayout.HorizontalSlider(playerState.minDistance, 0f, 20);

                    if (dist != playerState.minDistance)
                    {
                        playerState.minDistance = dist;
                        shouldSave = true;
                    }
                    //GUILayout.Label("meter");

                }


                GUILayout.TextField((playerState.minDistance).ToString("0.00"));

                HorizontalLine(Color.grey);
            }
            

            using (new GUILayout.VerticalScope())
            {

                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label("Time Reversal");

                    float timeReversal = GUILayout.HorizontalSlider(playerState.timeReversal, 1f, 0);

                    if (timeReversal != playerState.omniBalance)
                    {
                        playerState.timeReversal = timeReversal;
                        shouldSave = true;
                    }
                    GUILayout.Label("None");

                }


                GUILayout.TextField((playerState.timeReversal).ToString("0.00"));

                HorizontalLine(Color.grey);
            }


            if (playerState.fileName != "")
            {
                using (new GUILayout.HorizontalScope())
                {
                    // Display metering of each channel of the 
                    float[] XYstart = new float[2];
                    if (player.meters != null)
                    {
                        XYstart = DisplayMetering(player.meters, player.GetIsPlaying());


                        float sliederHeight = 86;
                        float g = GUI.VerticalSlider(new Rect(XYstart[0] - 20, XYstart[1], 40, sliederHeight), playerState.gain, 10f, -80f);


                        if (g != playerState.gain)
                        {
                            playerState.gain = g;
                            shouldSave = true;
                        }

                        GUI.Label(new Rect(XYstart[0] - 30, XYstart[1]+sliederHeight/2f + 10, 80, sliederHeight), ((int)playerState.gain).ToString() + " dB");
                    }
                }
                GUILayout.Label("");
                //GUILayout.Label("");
            }



            //int numChannel = 4;
            /*
            string[] channelRouting = new string[player.outputChannelCount];
            for (int i = 0; i < channelRouting.Length; i++)
            {
                channelRouting[i] = i.ToString();
            }
            int [] selectedChannelRouting = new int[player.numChannelsInAudioFile];
            for (int i = 0; i < selectedChannelRouting.Length; i++)
            {
                selectedChannelRouting[i] = i;
            }

            for (int c = 0; c < player.numChannelsInAudioFile; c++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("channel " + c);
                    int select = EditorGUILayout.Popup(selectedChannelRouting[c], channelRouting);
                }

            }
            */
            



            player.gain = playerState.gain;
            player.is3D = playerState.is3D;
            player.isDirective = playerState.isDirective; //modif mathias 06-17-2021
            player.isPlayingOnAwake = playerState.isPlayingOnAwake;
            player.fileName = playerState.fileName;
            player.isLooping = playerState.isLooping;
            player.attenuation = playerState.attenuation;
            player.omniBalance = playerState.omniBalance;
            player.timeReversal = playerState.timeReversal;
            // save the Player State to be always updated !!
           
        }

    }

    // ------------------------------------- DRAWING UTILITY --------------------------------------------
    float[] DisplayMetering(float[] metering, bool isPlaying)
    {
        float[] XY_start = new float[2];

        const int MeterCountMaximum = 48;
        int meterHeight = 86;
        int meterWidth = (int)((128 / (float)meterOff.height) * meterOff.width);
            
        int minimumWidth = meterWidth * MeterCountMaximum;

        Rect fullRect = GUILayoutUtility.GetRect(minimumWidth/4.0f, meterHeight,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        float baseX = fullRect.x + (fullRect.width - (meterWidth * metering.Length)) / 2;

        XY_start[0] = baseX;
        XY_start[1] = fullRect.y;
        for (int i = 0; i < player.numChannelsInAudioFile; i++)
        {
            Rect meterRect = new Rect(baseX + meterWidth * i, fullRect.y, meterWidth, fullRect.height);

            GUI.DrawTexture(meterRect, meterOff);

            float db = 20.0f * Mathf.Log10(metering[i] * Mathf.Sqrt(2.0f));
            db = Mathf.Clamp(db, -80.0f, 10.0f);
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
            if (isPlaying)
                GUI.DrawTextureWithTexCoords(levelPosRect, meterOn, levelUVRect);
        }

        return XY_start;


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