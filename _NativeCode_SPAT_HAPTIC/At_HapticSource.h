#pragma once

#include <windows.h>
#include <iostream>
#include "AudioPluginUtil.h"

#define MAX_BUFFER_SIZE 2048
#define MAX_OUTPUT_CHANNEL 9
#include "Biquad.h"

class At_HapticSource
{
public:

    At_HapticSource(); // constructor
    ~At_HapticSource(); // destructor

    void Process(float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume);
    void forceMonoInputAndApplyRolloff(float* inBuffer, int bufferLength, int offset, int inchannels);

    void SetSourcePosition(float* position);    
    void SetListenerPosition(float* position);
    
    void SetSourceAttenuation(float attenuation);
    void setSourceMinDistance(float minDistance);
    
    void SetSourceLowPassFc(double fc);    
    void SetSourceHighPassFc(double fc);

    void SetSourceLowPassGain(double gain);
    void SetSourceHighPassGain(double gain);

    float m_pTmpMonoBuffer_in[MAX_BUFFER_SIZE];

    int m_id = 0;

    float* m_pMixingBuffer;

    int m_bufferLength;
    int m_outChannelCount;

    static int m_numInstance;
    static int m_numInstanceProcessed;

    float m_sampleRate = 48000.0f;

    float m_pListenerPosition[3];
    float m_pSourcePosition[3];
    // rolloff factor
    float m_attenuation = 0;

    float m_minDistance = 0;

    // Effect applied to the input mono source
    // 1) Filters 
    Biquad *m_pLowPass;    
    Biquad *m_pHighPass;
    




};

