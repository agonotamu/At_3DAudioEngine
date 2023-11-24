#include "At_HapticSource.h"
#include <iostream>
#include "AudioPluginUtil.h"

#include "At_HapticEngine.h"

int At_HapticSource::m_numInstance = 0;
int At_HapticSource::m_numInstanceProcessed = 0;

At_HapticSource::At_HapticSource() {
    m_numInstance++;
}

At_HapticSource::~At_HapticSource() {
    m_numInstance--;
}


void At_HapticSource::SetSourcePosition(float* position) {
    m_pSourcePosition[0] = position[0];
    m_pSourcePosition[1] = position[1];
    m_pSourcePosition[2] = position[2];
}

void At_HapticSource::SetListenerPosition(float* position) {
    m_pListenerPosition[0] = position[0];
    m_pListenerPosition[1] = position[1];
    m_pListenerPosition[2] = position[2];
}


void At_HapticSource::SetSourceAttenuation(float attenuation) {
    m_attenuation = attenuation;
}

void At_HapticSource::setSourceMinDistance(float minDistance) {
    m_minDistance = minDistance;

}

void At_HapticSource::forceMonoInputAndApplyRolloff(float* inBuffer, int bufferLength, int offset, int inchannels) {

    float rolloff;
    float direction[3];
    direction[0] = m_pSourcePosition[0] - m_pListenerPosition[0];
    direction[1] = m_pSourcePosition[1] - m_pListenerPosition[1];
    direction[2] = m_pSourcePosition[2] - m_pListenerPosition[2];


    float sourceListenerDistance = sqrtf(pow(direction[0], 2) + pow(direction[1], 2) + pow(direction[2], 2));
    if (sourceListenerDistance < m_minDistance || m_attenuation == 0) {
        rolloff = 1;
    }
    else {
        rolloff = 1.0f / pow((sourceListenerDistance - m_minDistance) + 1, m_attenuation);
    }




    int count = 0;
    for (int i = 0; i < bufferLength * inchannels; i += inchannels) {
        m_pTmpMonoBuffer_in[count] = rolloff * inBuffer[i + offset];
        count++;
    }

}

/****************************************************************************
*
*                            MAIN PROCESS METHOD
*
*****************************************************************************/

void At_HapticSource::Process(float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume) {

    
    forceMonoInputAndApplyRolloff(inBuffer, bufferLength, offset, inChannelCount);
    
    for (int sample = 0; sample < bufferLength; sample ++) {
        for (int channel = 0; channel < m_outChannelCount; channel++) {
            //outBuffer[sample * outChannelCount + channel] = m_pTmpMonoBuffer_in[sample];

            m_pMixingBuffer[sample * m_outChannelCount + channel] += volume * m_pTmpMonoBuffer_in[sample];
#ifdef DEBUGLOG
            std::cout << "mixing buffer :" << m_pMixingBuffer[sample * outChannelCount + channel] << "," "\n";
#endif

        }
    }
    
    
}
