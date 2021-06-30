﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class At_RuntimeParamControlGUI : MonoBehaviour
{
    public OSC osc;

    public Slider gainSlider;
    public Text gainText;
    public Toggle isRemoteToogle;

    RuntimePlayerState voicePlayerState;

    At_Player[] players;

    At_OutputState outputState;
    List<At_PlayerState> playersState;
    bool isSliderRemote = true;

    public At_DynamicRandomPlayer randomPlayer;

    // Start is called before the first frame update
    void Start()
    {
        osc = GetComponent<OSC>();

        players = GameObject.FindObjectsOfType<At_Player>();

        playersState = new List<At_PlayerState>();

        
        outputState = At_AudioEngineUtils.getOutputState();
        foreach(At_Player p in players)
        {
            At_PlayerState ps = At_AudioEngineUtils.getPlayerStateWithName(p.name);
            playersState.Add(ps);
        }
        At_PlayerState voice_state = At_AudioEngineUtils.getPlayerStateWithName("3DSourceVoice");
        gainSlider.value = voice_state.gain;
        osc.SetAddressHandler("/3DAudioEngine/Player/3DSourceVoice", OnReceiveGainslider);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnRemote()
    {
        isSliderRemote = isRemoteToogle.isOn;
    }

    void OnReceiveGainslider(OscMessage message)
    {
        gainSlider.value = message.GetFloat(0);
    }

    public void OnGainslider(string sourceName)
    {
        gainText.text = "Gain \"Voice\" : " + gainSlider.value.ToString("0.0") + "dB";
        
        At_PlayerState ps = At_AudioEngineUtils.getPlayerStateWithName(sourceName);
        ps.gain = gainSlider.value;
        At_AudioEngineUtils.SavePlayerStateWithName(sourceName);
        GameObject.Find(sourceName).GetComponent<At_Player>().gain = gainSlider.value;
    }

    public void OnPlay()
    { 
        foreach(At_Player p in players)
        {
            p.StartPlaying();
        }
    }

    public void OnStop()
    {
        foreach (At_Player p in players)
        {
            p.StopPlaying();
        }
    }

    public void OnRandomPlay()
    {
        randomPlayer.AddOneShotInstanceAndRandomPlay();
    }

    

}
