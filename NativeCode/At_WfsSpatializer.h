

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

#define MAX_BUFFER_SIZE 2048
#define MAX_OUTPUT_CHANNEL 48


namespace Spatializer
{

    class At_WfsSpatializer
    {

    public:

        /*
        static At_WfsSpatializer& getInstance()
        {
            static At_WfsSpatializer instance; // Guaranteed to be destroyed.
            // Instantiated on first use.
            return instance;
        }
        */

        int process(float* inBuffer, float* outBuffer, int bufferLength, int inChannel, int outChannel);
        int setSourcePosition(float* position);
        int setSourceAttenuation(float attenuation);
        int setSourceOmniBalance(float omniBalance);
        int setTimeReversal(float timeReversal);

        int setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards);
        int setListenerPosition(float* position, float* rotation);
        
        void setSampleRate(float sampleRate);

    private:
        void forceMonoInput(float* inBuffer, int bufferLength, int inchannels);
        void updateDelayBuffer(int bufferLength);
        // 11/03/2021 - BUG CORRECTION- we do not use azimuth and elevation any more, but forward vector only.
        /*
        float getElevation(int virtualMicIdx, float* direction);
        float getAzimuth(int virtualMicIdx, float* direction);        
        void getWfsVolumeAndDelay(int virtualMicIndex, float virtualMicDistance, float* wfsVolume, float* wfsDelay);
        */
        void updateWfsVolumeAndDelay();
        void updateMixMaxDelay();
        void applyWfsGainDelay(int virtualMicIndex, int bufferLength);
       

        float m_pTmpMonoBuffer_in[MAX_BUFFER_SIZE];
        float m_pDelayBuffer[MAX_BUFFER_SIZE * 4];

        float m_sourcePosition[3];
        float m_attenuation;
        float m_omniBalance;
        float m_timeReversal;
        float m_minDelay;
        float m_maxDelay;

        float m_pAzimuth[MAX_OUTPUT_CHANNEL];
        float m_pElevation[MAX_OUTPUT_CHANNEL];

        float m_pWfsVolume[MAX_OUTPUT_CHANNEL];
        float m_pWfsDelay[MAX_OUTPUT_CHANNEL];

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
