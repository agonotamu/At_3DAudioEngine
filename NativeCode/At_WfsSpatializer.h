

#pragma once

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64)
#define UNITY_WIN 1
#elif defined(__MACH__) || defined(__APPLE__)
#define UNITY_OSX 1
#elif defined(__ANDROID__)
#define UNITY_ANDROID 1
#elif defined(__linux__)
#define UNITY_LINUX 1
#endif

#include <stdio.h>
#include <memory> //for std::unique_ptr
//#include "At_SpatializationEngine.h"





#if UNITY_OSX | UNITY_LINUX
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <errno.h>
#include <unistd.h>
#include <string.h>
#elif UNITY_WIN
#include <windows.h>
#include <iostream>
#endif
#include "AudioPluginUtil.h"
#define MAX_BUFFER_SIZE 1024
#define NUM_BUFFER_IN_DELAY 2
#define MAX_OUTPUT_CHANNEL 24
#define MAX_INPUT_CHANNEL 16


namespace Spatializer
{
    typedef struct directiveData DirectiveData;

    class At_WfsSpatializer
    {

    public:

        int process(float* inBuffer, float* outBuffer, int bufferLength, int inChannel, int outChannel);
        int setSourcePosition(float* position, float* rotation, float* forward); 
        int setSourceAttenuation(float attenuation);
        int setSourceOmniBalance(float omniBalance);
        int setTimeReversal(float timeReversal);
        int setMinDistance(float minDistance);

        int setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards);
        int setListenerPosition(float* position, float* rotation);
        
        void setSampleRate(float sampleRate);

        void initDelayBuffer();

        int spatID = 0;

        bool m_is3D; 
        bool m_isDirective; 

    private:
        void setIs3DIsDirective(bool is3D, bool isDirective); //modif mathias 06-17-2021
        void forceMonoInput(float* inBuffer, int bufferLength, int inchannels);
        void updateMultichannelDelayBuffer(float* inBuffer, int bufferLength, int inChannelCount);

        
        void updateDelayBuffer(int bufferLength);
        
        void updateWfsVolumeAndDelay();
        void updateMixMaxDelay();
        void applyWfsGainDelay(int virtualMicIndex, int m_virtualMicCount, int bufferLength, bool isDirective); //modif mathias 06-17-2021
       
        
        void updateMixedDirectiveChannel(int virtualMicIdx, int inChannelCount);

        float m_pTmpMonoBuffer_in[MAX_BUFFER_SIZE];
        float m_pDelayBuffer[MAX_BUFFER_SIZE * NUM_BUFFER_IN_DELAY];

        float m_pDelayMultiChannelBuffer[MAX_INPUT_CHANNEL][MAX_BUFFER_SIZE * NUM_BUFFER_IN_DELAY];


        float m_sourcePosition[3];
        float m_sourceRotation[3]; 
        float m_sourceForward[3]; 
        float m_attenuation;
        float m_omniBalance;
        float m_timeReversal;
        float m_minDelay, m_minDelay_prevFrame;
        float m_maxDelay, m_maxDelay_prevFrame;
        
        float m_minDistance;

        float m_ChannelWeight[MAX_OUTPUT_CHANNEL][2][2];
        
        float m_pAzimuth[MAX_OUTPUT_CHANNEL];
        float m_pElevation[MAX_OUTPUT_CHANNEL];

        float m_pWfsVolume[MAX_OUTPUT_CHANNEL];
        float m_pWfsDelay[MAX_OUTPUT_CHANNEL];
        
        float m_pWfsVolume_prevFrame[MAX_OUTPUT_CHANNEL];
        float m_pWfsDelay_prevFrame[MAX_OUTPUT_CHANNEL];

        // common variables for each instance 
        float m_sampleRate = 48000.0f;
        float m_listenerPosition[3];
        float m_listenerRotation[3];
        
        float m_virtualMicMinDistance = 1;
        int m_virtualMicCount;
        float m_pVirtualMicPositions[MAX_OUTPUT_CHANNEL][3];
        float m_pVirtualMicRotations[MAX_OUTPUT_CHANNEL][3];
        float m_pVirtualMicForwards[MAX_OUTPUT_CHANNEL][3];
    };

}
