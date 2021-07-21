﻿/*
 * Author : Antoine Gonot
 * Company : Art-Tone
 * First Creation : 19/01/2021
 * 
 * 
 */

using System.Collections;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioAsioPatchBay;
using SFB;

[CanEditMultipleObjects]
[CustomEditor(typeof(At_MasterOutput))]
public class At_MasterOutputEditor : Editor
{
    int selectedDeviceIndex = 0;
    string selectedDevicename = "";
    List<string> deviceList;
    string[] devices;
    private AsioOut asioOut;

    At_OutputState outputState = new At_OutputState();
    At_MasterOutput masterOutput;
    string[] outputConfigSelection = { "1D [2]", "1D [4]", "1D [8]", "1D [10]", "1D [12]", "1D [14]", "1D [16]", "2D [4]", "2D [6]", "2D [8]", "2D [10]", "2D [12]", "2D [24]"};
    int selectSpeakerConfig = 0;
    int outputConfigDimension = 1;
    public GameObject[] virtualMic;
    public GameObject[] speakers;

    string[] samplingRateConfigSelection = { "44100", "48000" };
    int selectedSamplingRate = 0;
    int samplingRate = 44100;
    // create your style
    GUIStyle horizontalLine;

    float mapScale_3dUnitByMeter = 1;
    float virtualMicRigSize = 3;    
    float speakerRigSize = 10;



    private GUIContent spkButtonContent;

    bool isEngineStarted = false;
    // DRAWING
    // ----- texture for metering ------
    private Texture meterOn;
    private Texture meterOff;
    public float[] meters;

    bool isStartingEngineOnAwake;

    bool shouldSave = false;

    // load ressources for the GUI (textures, etc.)
    private void AffirmResources()
    {
        if (meterOn == null)
        {
            meterOn = EditorGUIUtility.Load("/At_3DAudioEngine/LevelMeter.png") as Texture;
            meterOff = EditorGUIUtility.Load("/At_3DAudioEngine/LevelMeterOff.png") as Texture;
        }
    }

    

    // Called when the GameObject with the At_Player component is selected (Inspector is displayed) or when the component is added
    public void OnEnable()
    {
        
        //SceneView.duringSceneGui += OnSceneGUI;

        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;               

        masterOutput = (At_MasterOutput)target;

        if (masterOutput.gameObject.GetComponent<At_Mixer>() == null)
        {
            masterOutput.gameObject.AddComponent<At_Mixer>();
        }

        if (masterOutput.gameObject.GetComponent<At_ExternAssets>() == null)
        {
            masterOutput.gameObject.AddComponent<At_ExternAssets>();
        }

        outputState = At_AudioEngineUtils.getOutputState();

        if (outputState == null)
        {
            outputState = new At_OutputState();
            outputState.audioDeviceName = "";
            outputState.outputChannelCount = 2;
            outputState.selectSpeakerConfig = 0;
            outputState.outputConfigDimension = 1;
            outputState.selectedSamplingRate = 0;
            outputState.samplingRate = 44100;
            outputState.isStartingEngineOnAwake = true;
            outputState.virtualMicRigSize = 3.0f;
            isStartingEngineOnAwake = true;
            samplingRate = 44100;
            //At_AudioEngineUtils.getState().setOutputState(outputState);
            outputConfigDimension = 1;
        }
        else
        {
            masterOutput.audioDeviceName = outputState.audioDeviceName;
            masterOutput.outputChannelCount = outputState.outputChannelCount;            
            masterOutput.outputConfigDimension = outputState.outputConfigDimension;
            masterOutput.virtualMicRigSize = outputState.virtualMicRigSize;
            meters = new float[masterOutput.outputChannelCount];
            selectSpeakerConfig = outputState.selectSpeakerConfig;
            outputConfigDimension = outputState.outputConfigDimension;
            masterOutput.samplingRate = outputState.samplingRate;
            selectedSamplingRate = outputState.selectedSamplingRate;
            samplingRate = (selectedSamplingRate == 0 ? 44100 : 48000);
            isStartingEngineOnAwake = outputState.isStartingEngineOnAwake;
            masterOutput.virtualMicRigSize = outputState.virtualMicRigSize;

        }
        At_Player[] players = GameObject.FindObjectsOfType<At_Player>();
        /*
        foreach(At_Player p in players)
        {
            p.outputChannelCount = outputState.outputChannelCount;
        }
        */

        deviceList = new List<string>();
        deviceList.Add("none");
        foreach (var device in AsioOut.GetDriverNames())
        {
            deviceList.Add(device);
        }
        devices = new string[deviceList.Count];
        for (int i = 0; i< deviceList.Count; i++)
        {
            devices[i] = deviceList[i];

        }
    }
    
    public void OnDisable()
    {
        //Debug.Log("save output state on disable");
        //At_AudioEngineUtils.SaveState();

        if (shouldSave == true) {
            // At_AudioEngineUtils.SaveOutputState(); modif mathias 30-06-202
            At_AudioEngineUtils.SaveAllState();
            shouldSave = false;
        }


    }

    

    public override void OnInspectorGUI()
    {
        // laod ressources if needed
        AffirmResources();

        //bool shouldSave = false;
        bool shouldInitOutputMatrix = false;

        bool speakerConfigHasChanged = false;

        //-----------------------------------------------------------------
        // ASIO DEVICE SELECTION
        //----------------------------------------------------------------

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("ASIO device :");


            if (devices != null && devices.Length != 0)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i] == outputState.audioDeviceName)
                    {
                        selectedDeviceIndex = i;
                        break;
                    }

                }
                int d = EditorGUILayout.Popup(selectedDeviceIndex, devices);
                if (d != selectedDeviceIndex)
                {
                    selectedDeviceIndex = d;
                    shouldSave = true;
                }


                outputState.audioDeviceName = devices[selectedDeviceIndex];
                //At_AudioEngineUtils.getState().addAudioDeviceName(outputState.audioDeviceName);

            }
        }
        HorizontalLine(Color.grey);
        //-----------------------------------------------------------------
        // MASTER OUTPUT BUS CONFIGURATION
        //----------------------------------------------------------------
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Sampling Rate");
            selectedSamplingRate = EditorGUILayout.Popup(selectedSamplingRate, samplingRateConfigSelection);
            samplingRate = (selectedSamplingRate == 0 ? 44100 : 48000);
            if (samplingRate != outputState.samplingRate)
            {
                shouldSave = true;
                outputState.samplingRate = samplingRate;
                outputState.selectedSamplingRate = selectedSamplingRate;
                //At_AudioEngineUtils.getState().addSelectedSamplingRate(selectedSamplingRate);
                //At_AudioEngineUtils.getState().addSamplingRate(samplingRate);
            }
        }
        HorizontalLine(Color.grey);
        //-----------------------------------------------------------------
        // MASTER OUTPUT BUS CONFIGURATION
        //----------------------------------------------------------------

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Configuration");
            selectSpeakerConfig = EditorGUILayout.Popup(selectSpeakerConfig, outputConfigSelection);
            int indexStartChannelCount = outputConfigSelection[selectSpeakerConfig].IndexOf("[");
            int indexendChannelCount = outputConfigSelection[selectSpeakerConfig].IndexOf("]");
            int channelCount = int.Parse(outputConfigSelection[selectSpeakerConfig].Substring(indexStartChannelCount + 1, indexendChannelCount - indexStartChannelCount - 1));
            outputConfigDimension = int.Parse(outputConfigSelection[selectSpeakerConfig].Substring(0, 1));
            if (channelCount != outputState.outputChannelCount || outputConfigDimension != outputState.outputConfigDimension)
            {
                speakerConfigHasChanged = true;
                shouldSave = true;
                outputState.outputConfigDimension = outputConfigDimension;
                outputState.outputChannelCount = channelCount;
                At_Player[] players = GameObject.FindObjectsOfType<At_Player>();
                foreach(At_Player p in players)
                {
                    p.outputChannelCount = channelCount;
                }

                outputState.selectSpeakerConfig = selectSpeakerConfig;
                meters = new float[outputState.outputChannelCount];
                shouldInitOutputMatrix = true;
                /*
                At_AudioEngineUtils.getState().addOutputChannelCount(channelCount);
                At_AudioEngineUtils.getState().addOutputSelectedSpeakerConfig(selectSpeakerConfig);
                At_AudioEngineUtils.getState().addOutputSelectedOutputDimension(outputConfigDimension);
                */
            }
        }


        //-----------------------------------------------------------------
        // MASTER METERING AND GAIN
        //----------------------------------------------------------------

        using (new GUILayout.HorizontalScope())
        {
            if (masterOutput.running && masterOutput.meters != null)
            {
                meters = masterOutput.meters;
            }
            float baseX = 0;
            if (meters != null)
                baseX = DisplayMetering(meters, masterOutput.running);
            float sliederHeight = 84;


            float g = GUI.VerticalSlider(new Rect(baseX - 20, 79, 80, sliederHeight), outputState.gain, 10f, -80f);
            if (g != outputState.gain)
            {
                outputState.gain = g;
                shouldSave = true;
            }


            GUI.Label(new Rect(baseX - 30, 130  , 80, sliederHeight), ((int)outputState.gain).ToString() + " dB");

            
        }
        GUILayout.Label("");
        //-----------------------------------------------------------------
        // START/STOP ENGINE BUTTONS
        //----------------------------------------------------------------

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("START ENGINE"))
            {
                isEngineStarted = true;
                masterOutput.StartEngine();
            }
            if (GUILayout.Button("STOP ENGINE"))
            {
                isEngineStarted = false;
                masterOutput.StopEngine();
            }
        }
        using (new GUILayout.HorizontalScope())
        {
            isStartingEngineOnAwake = GUILayout.Toggle(isStartingEngineOnAwake, "Start Engine On Awake");

            if (isStartingEngineOnAwake != outputState.isStartingEngineOnAwake)
            {
                outputState.isStartingEngineOnAwake = isStartingEngineOnAwake;
                //At_AudioEngineUtils.getState().addIsStartingEngineOnAwake(isStartingEngineOnAwake);
                shouldSave = true;
            }
        }

        HorizontalLine(Color.grey);
        
        using (new GUILayout.HorizontalScope())
        {            
            //-----------------------------------------------------------------
            // VIRTUAL SPEAKERS/MICS ADD TO SCENE BUTON
            //----------------------------------------------------------------

            GUILayout.Label("                 ");
            if (spkButtonContent == null)
                spkButtonContent = new GUIContent((Texture)Resources.Load("At_3DAudioEngine/Prefabs/SpeakerIcon")); // file name in the resources folder without the (.png) extension

            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Initialize\nSpeaker config");
                //GUILayout.BeginHorizontal();
                //GUILayout.Label("--------------------------|");
                if (speakerConfigHasChanged || GUILayout.Button(spkButtonContent,GUILayout.Width(85), GUILayout.Height(85)))
                {
                    speakerConfigHasChanged = false;
                    At_VirtualMic[] vms;
                    vms = GameObject.FindObjectsOfType<At_VirtualMic>();
                    GameObject parent = null;
                    if (vms != null && vms.Length != 0) parent = vms[0].transform.parent.gameObject;
                    foreach (At_VirtualMic vm in vms) DestroyImmediate(vm.gameObject);
                    if (parent != null) DestroyImmediate(parent);

                    At_VirtualSpeaker[] vss;
                    vss = GameObject.FindObjectsOfType<At_VirtualSpeaker>();
                    if (vss != null && vss.Length != 0) parent = vss[0].transform.parent.gameObject;
                    foreach (At_VirtualSpeaker vs in vss) DestroyImmediate(vs.gameObject);
                    if (parent != null) DestroyImmediate(parent);

                    GameObject virtualMicParent = new GameObject("VirtualMics");
                    virtualMicParent.transform.parent = masterOutput.gameObject.transform;
                    GameObject virtualSpkParent = new GameObject("VirtualSpeakers");
                    virtualSpkParent.transform.parent = masterOutput.gameObject.transform;
                    At_SpeakerConfig.addSpeakerConfigToScene(ref virtualMic, outputState.virtualMicRigSize, ref speakers, speakerRigSize,
                        outputState.outputChannelCount, outputState.outputConfigDimension, virtualMicParent, virtualSpkParent);

                    for (int i = 0; i < virtualMic.Length; i++)
                    {
                        //UnityEditor.PrefabUtility.UnpackPrefabInstance(virtualMic[i], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        //UnityEditor.PrefabUtility.UnpackPrefabInstance(speakers[i], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }

                }
            }
        }


        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("");
        }
        using (new GUILayout.HorizontalScope())
        {

            GUILayout.Label("Mic Rig Size");

            float size = GUILayout.HorizontalSlider(outputState.virtualMicRigSize, 0, 20);

            if (size != outputState.virtualMicRigSize)
            {
                Debug.Log(size);
                outputState.virtualMicRigSize = size;
                masterOutput.virtualMicRigSize = size;
                shouldSave = true;
            }
            //GUILayout.Label("Size");

        }


        GUILayout.TextField((outputState.virtualMicRigSize).ToString("00.0"));

        HorizontalLine(Color.grey);

        GUILayout.Label("");
        string audioFilePath = "";
        
        



        masterOutput.audioDeviceName = outputState.audioDeviceName;
        masterOutput.outputChannelCount = outputState.outputChannelCount;
        masterOutput.gain = outputState.gain;
        masterOutput.samplingRate = outputState.samplingRate;
        masterOutput.isStartingEngineOnAwake = outputState.isStartingEngineOnAwake;
        masterOutput.outputConfigDimension = outputState.outputConfigDimension;
        masterOutput.virtualMicRigSize = outputState.virtualMicRigSize;
    }

    // utility method
    void HorizontalLine(Color color)
    {
        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLine);
        GUI.color = c;
    }

    // ------------------------------------- DRAWING UTILITY --------------------------------------------
    float DisplayMetering(float[] metering, bool isEngineStarted)
    {
        const int MeterCountMaximum = 48;
        int meterHeight = 86;
        int meterWidth = (int)((128 / (float)meterOff.height) * meterOff.width);

        int minimumWidth = meterWidth * MeterCountMaximum;

        Rect fullRect = GUILayoutUtility.GetRect(minimumWidth/4.0f, meterHeight,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        float baseX = fullRect.x + (fullRect.width - (meterWidth * metering.Length)) / 2;

        for (int i = 0; i < outputState.outputChannelCount; i++)
        {
            Rect meterRect = new Rect(baseX + meterWidth * i, fullRect.y, meterWidth, fullRect.height);

            GUI.DrawTexture(meterRect, meterOff);
            float db = -86;
            if (i< metering.Length)
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
            if (isEngineStarted)
                GUI.DrawTextureWithTexCoords(levelPosRect, meterOn, levelUVRect);
        }

        return baseX;


    }

}

