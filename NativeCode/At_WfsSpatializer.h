

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
#define MAX_BUFFER_SIZE 2048
#define NUM_BUFFER_IN_DELAY 32
#define MAX_OUTPUT_CHANNEL 48
#define MAX_INPUT_CHANNEL 16

#define RING_BUFFER

namespace Spatializer
{
    typedef struct directiveData DirectiveData;

    class At_WfsSpatializer
    {

    public:
        ~At_WfsSpatializer(); // destructor

        int process(float* inBuffer, float* outBuffer, int bufferLength, int offset, int inChannel, int outChannel);
        int WFS_getDelay(float* delay, int arraySize);
        int WFS_getVolume(float* volume, int arraySize);
        
        int setSourcePosition(float* position, float* rotation, float* forward); 
        int setSourceAttenuation(float attenuation);
        int setSourceOmniBalance(float omniBalance);
        int setTimeReversal(float timeReversal);
        int setMinDistance(float minDistance);
        int setSpeakerMask(float* activationSpeakerVolume, int outChannelCount); // Modif Rougerie 29/06/2022

        int setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards);
        int setListenerPosition(float* position, float* rotation);
        
        void setSampleRate(float sampleRate);

        void cleanDelayBuffer();

        void initDelayBuffer();

        int spatID = 0;

        bool m_is3D; 
        bool m_isDirective; 
        float  m_maxDistanceForDelay;

    private:
        void setIs3DIsDirective(bool is3D, bool isDirective); //modif mathias 06-17-2021
        void forceMonoInput(float* inBuffer, int bufferLength, int offset, int inchannels);
        void updateMultichannelDelayBuffer(float* inBuffer, int bufferLength, int inChannelCount);

        
        
#ifdef RING_BUFFER
        void updateDelayRingBuffer(int bufferLength);
#else
        void updateDelayBuffer(int bufferLength);
#endif        
        void updateWfsVolumeAndDelay();
        void updateMixMaxDelay();
        void applyWfsGainDelay(int virtualMicIndex, int m_virtualMicCount, int bufferLength, bool isDirective); //modif mathias 06-17-2021
       
        
        void updateMixedDirectiveChannel(int virtualMicIdx, int inChannelCount);


        // This should be also dynamically allocated !!!
        float m_pTmpMonoBuffer_in[MAX_BUFFER_SIZE];       
        
        // This is now dynamically allocated
        //float m_pDelayBuffer[MAX_BUFFER_SIZE * NUM_BUFFER_IN_DELAY];
 
        int m_delayBufferSize;
 #ifdef RING_BUFFER
        float* m_pDelayRingBuffer;
#else
        float* m_pDelayBuffer;
#endif

        // NO DIRECTIVE SOURCE
        //float m_pDelayMultiChannelBuffer[MAX_INPUT_CHANNEL][MAX_BUFFER_SIZE * NUM_BUFFER_IN_DELAY];

        // index of the current position to write in the circular buffer
        int mDelayRingBuffeWriteIndex = 0;

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
        float m_pWfsSpeakerMask[MAX_OUTPUT_CHANNEL]; // Modif Rougerie 29/06/2022
        
        float m_pProcess_delay[MAX_OUTPUT_CHANNEL];        

        float m_pWfsVolume_prevFrame[MAX_OUTPUT_CHANNEL];
        float m_pWfsDelay_prevFrame[MAX_OUTPUT_CHANNEL];
        float m_pWfsSpeakerMask_prevFrame[MAX_OUTPUT_CHANNEL]; // Modif Rougerie 29/06/2022

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
