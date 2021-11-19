#include "At_WfsSpatializer.h"

#include <iostream>

#include "AudioPluginUtil.h"

#include "At_SpatializationEngine.h"

//#define DEBUG_LOAD

namespace Spatializer
{

    At_WfsSpatializer::~At_WfsSpatializer() {

        std::cout << "Spatializer " << spatID << " destroyed !" << " \n";
        //delete[] m_pDelayBuffer;
        //m_pDelayBuffer = NULL;
    }

    /****************************************************************************
    *
    *           GET THE FIRST CHANNEL OHF THE MULTICHANNEL INPUT BUFFER              
    *                       (FOR NON-DIRECTIVE SOURCE ONLY)
    *
    *****************************************************************************/
    void At_WfsSpatializer::forceMonoInput(float* inBuffer, int bufferLength, int inchannels) {

        int count = 0;
        for (int i = 0; i < bufferLength * inchannels; i += inchannels) {
            m_pTmpMonoBuffer_in[count] = inBuffer[i];
            count++;
        }

    }

    /****************************************************************************
    *
    *           UDPATE THE 2 CHANNELS USED FOR GETTING INPUT MONO INPUT
    *                   AND THE WEIGHT TO APPLYIED FOR SUMMING
    *                       (FOR DIRECTIVE SOURCE ONLY)
    *
    *****************************************************************************/
    void At_WfsSpatializer::updateMixedDirectiveChannel(int virtualMicCount, int inChannelCount) {

        float sourcedirection[3];
        float sourceforward[3];
        float upVector[3];
        float crossProduct[3];

        int tmp;

        float theta;
        double indexChannels;

        int indexChannel1, indexChannel2;
        float weight1, weight2;

        upVector[0] = 0;
        upVector[1] = 1;
        upVector[2] = 0;

        sourceforward[0] = m_sourceForward[0];
        sourceforward[1] = m_sourceForward[1];
        sourceforward[2] = m_sourceForward[2];
        float virtualSourceDistance = sqrtf(pow(sourceforward[0], 2) + pow(sourceforward[1], 2) + pow(sourceforward[2], 2));

        for (int virtualMicIdx = 0; virtualMicIdx < virtualMicCount; virtualMicIdx++) {

            sourcedirection[0] = m_sourcePosition[0] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][0];
            sourcedirection[1] = m_sourcePosition[1] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][1];
            sourcedirection[2] = m_sourcePosition[2] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][2];
            float virtualMicDistance = sqrtf(pow(sourcedirection[0], 2) + pow(sourcedirection[1], 2) + pow(sourcedirection[2], 2));

            float dotproduct = sourceforward[0] * sourcedirection[0] + sourceforward[1] * sourcedirection[1] + sourceforward[2] * sourcedirection[2];

            crossProduct[0] = sourcedirection[1] * upVector[2] - sourcedirection[2] * upVector[1];
            crossProduct[1] = -sourcedirection[0] * upVector[2] + sourcedirection[2] * upVector[0];
            crossProduct[2] = sourcedirection[0] * upVector[1] - sourcedirection[1] * upVector[0];

            float dotproductbis = sourceforward[0] * crossProduct[0] + sourceforward[1] * crossProduct[1] + sourceforward[2] * crossProduct[2];

            if (dotproductbis <= 0) {
                theta = acosf((dotproduct) / (virtualSourceDistance * virtualMicDistance));
            }
            else {
                theta = -acosf(dotproduct / (virtualSourceDistance * virtualMicDistance)) + 2 * kPI;
            }

            indexChannels = theta / ((2 * kPI) / inChannelCount);
            indexChannel1 = indexChannels;

            if (indexChannel1 < 0) {
                indexChannel1 = 0;
            }

            if (indexChannel1 == inChannelCount) {
                indexChannel1 = 0;
            }

            if (indexChannel1 >= inChannelCount - 1) {
                indexChannel2 = 0;
            }
            else {
                indexChannel2 = indexChannel1 + 1;
            }

            tmp = (int)indexChannels;
            if (tmp == indexChannels) {
                weight1 = 1;
            }
            else { 
                weight1 = 1 - (indexChannels - indexChannel1);
            }

            weight2 = 1 - weight1;

            m_ChannelWeight[virtualMicIdx][0][0] = indexChannel1;
            m_ChannelWeight[virtualMicIdx][0][1] = weight1;
            m_ChannelWeight[virtualMicIdx][1][0] = indexChannel2;
            m_ChannelWeight[virtualMicIdx][1][1] = weight2;

            //std::cout << "Mic : " << virtualMicIdx << " - Channel1 : " << m_ChannelWeight[virtualMicIdx][0][0] << " - Weight1 : " << m_ChannelWeight[virtualMicIdx][0][1] << " - Channel2 : " << (int)m_ChannelWeight[virtualMicIdx][1][0] << " - Weight2 : " << m_ChannelWeight[virtualMicIdx][1][1] << " \n";  
        }
    
    }

    /****************************************************************************
    *
    *           UDPATE VOLUME AND DELAY FOR EACH FOR EACH OUTPUT CHANNEL
    *
    *****************************************************************************/
    void At_WfsSpatializer::updateWfsVolumeAndDelay() {


        for (int virtualMicIdx = 0; virtualMicIdx < m_virtualMicCount; virtualMicIdx++) {

            float direction[3], normalizedDirection[3];

            static const float kRad2Deg = 180.0f / kPI;

            direction[0] = m_sourcePosition[0] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][0];
            direction[1] = m_sourcePosition[1] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][1];
            direction[2] = m_sourcePosition[2] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][2];
            float virtualMicDistance = sqrtf(pow(direction[0], 2) + pow(direction[1], 2) + pow(direction[2], 2));

            normalizedDirection[0] = direction[0] / virtualMicDistance;
            normalizedDirection[1] = direction[1] / virtualMicDistance;
            normalizedDirection[2] = direction[2] / virtualMicDistance;

            m_pWfsDelay[virtualMicIdx] = (virtualMicDistance / 340.0f) * 1000.0f; // time in milliseconds;
            
            float rolloff;
            
            if (virtualMicDistance < m_minDistance || m_attenuation == 0) {
                rolloff = 1;
            }
            else {
                rolloff = 1.0f / pow((virtualMicDistance - m_minDistance) + 1, m_attenuation);
            }

            float forwardProj = normalizedDirection[0] * m_pVirtualMicForwards[virtualMicIdx][0]
                + normalizedDirection[1] * m_pVirtualMicForwards[virtualMicIdx][1]
                + normalizedDirection[2] * m_pVirtualMicForwards[virtualMicIdx][2];

            float cardioidSens = m_omniBalance + (1 - m_omniBalance) * (0.5f * (1 + forwardProj));
            
            m_pWfsVolume[virtualMicIdx] = cardioidSens * rolloff;

            //std::cout << "Mic : " << virtualMicIdx << " - volume : " << m_pWfsVolume[virtualMicIdx] << " \n";
        }


    }

    void At_WfsSpatializer::updateMixMaxDelay() {
        for (int virtualMicIdx = 1; virtualMicIdx < m_virtualMicCount; virtualMicIdx++) {
            
            if (m_pWfsDelay[virtualMicIdx] >= m_pWfsDelay[virtualMicIdx - 1]) {
                m_maxDelay = m_pWfsDelay[virtualMicIdx];
                m_minDelay = m_pWfsDelay[virtualMicIdx - 1];
            }
            else {
                m_maxDelay = m_pWfsDelay[virtualMicIdx-1];
                m_minDelay = m_pWfsDelay[virtualMicIdx];
            }
        }
    }
    
    /****************************************************************************
    *
    *                            DELAY BUFFERS UPDATE (EACH FRAME)
    *
    *****************************************************************************/
    
    void At_WfsSpatializer::cleanDelayBuffer() {
        for (int i = 0; i < m_delayBufferSize; i++) {
            m_pDelayBuffer[i] = 0;
        }
    }

    void At_WfsSpatializer::initDelayBuffer() {

        // 
#ifdef DEBUG_LOAD        
#else
        float maxDistanceForOneBuffer = 340 * MAX_BUFFER_SIZE / m_sampleRate;

        //std::cout << "maxDistanceForOneBuffer = " << maxDistanceForOneBuffer << " \n";

        int numBufferRequiredForUserMaxDistance = (int)(m_maxDistanceForDelay / maxDistanceForOneBuffer)+1;

        m_delayBufferSize = (int)(MAX_BUFFER_SIZE * numBufferRequiredForUserMaxDistance);

        m_pDelayBuffer = new float[m_delayBufferSize];

        //std::cout << "Init Delay Buffer with " << m_delayBufferSize << " samples for distance = " << m_maxDistanceForDelay << " \n";

        //for (int i = 0; i < MAX_BUFFER_SIZE * NUM_BUFFER_IN_DELAY; i++) {
        for (int i = 0; i < m_delayBufferSize; i++) {
            m_pDelayBuffer[i] = 0;
        }
#endif
        //std::cout << "last sample of delay buffer is : " << m_pDelayBuffer[m_delayBufferSize-1] << " \n";
    }

    // Accummulation of the input buffers to a "mono delay buffer" used to apply delay of the WFS algotithm
    void At_WfsSpatializer::updateDelayBuffer(int bufferLength) {

        // now i dynamicaly instanciated
        //int arrayLength = sizeof(m_pDelayBuffer) / sizeof(*m_pDelayBuffer);
        
        //std::cout << "delay buffer lenght is :" << arrayLength << "\n";

        // int numLengthInDelBuf = arrayLength / bufferLength;
        int numLengthInDelBuf = m_delayBufferSize / bufferLength;
        //std::cout << "numLengthInDelBuf =" << numLengthInDelBuf << "\n";

        
        for (int count = 1; count < numLengthInDelBuf; count++) {
            int startTo = (count - 1) * bufferLength;
            int startFrom = count * bufferLength;
            for (int sample = 0; sample < bufferLength; sample++) {
                m_pDelayBuffer[startTo + sample] = m_pDelayBuffer[startFrom + sample];
            }
        }
        
        
        for (int sample = 0; sample < bufferLength; sample++) {
            m_pDelayBuffer[(numLengthInDelBuf - 1) * bufferLength + sample] = m_pTmpMonoBuffer_in[sample];
        }  

    }

    // Accummulation of the input buffers to a "multichannel delay buffer" used to apply delay of the WFS algotithm when the source is "Directive"
    void At_WfsSpatializer::updateMultichannelDelayBuffer(float* inBuffer, int bufferLength, int inChannelCount) {
        // NO DIRECTIVE SOURCE
        /*
        int arrayLength = sizeof(m_pDelayBuffer) / sizeof(*m_pDelayBuffer);
        int numLengthInDelBuf = arrayLength / bufferLength;

        for (int InputChannel = 0; InputChannel < inChannelCount; InputChannel++) {
            for (int count = 1; count < numLengthInDelBuf; count++) {
                int startTo = (count - 1) * bufferLength;
                int startFrom = count * bufferLength;
                for (int sample = 0; sample < bufferLength; sample++) {
                    m_pDelayMultiChannelBuffer[InputChannel][startTo + sample] = m_pDelayMultiChannelBuffer[InputChannel][startFrom + sample];
                }
            }
            for (int sample = 0; sample < bufferLength; sample++) {
                m_pDelayMultiChannelBuffer[InputChannel][(numLengthInDelBuf - 1) * bufferLength + sample] = inBuffer[inChannelCount * sample + InputChannel];
            }
        }
        */
    }

    /****************************************************************************
    *
    *                 FOR EACH OUTPUT CHANNEL, APPLY A SMOOTH DELAY 
    *                          AND A VOLUME TO A MONO BUFFER 
    *                                 (CORE OF THE WFS)
    *
    *****************************************************************************/

    void At_WfsSpatializer::applyWfsGainDelay(int virtualMicIdx, int m_virtualMicCount, int bufferLength, bool isDirective) { //modif mathias 06-17-2021

        // Delay for focus WFS source
        float reverse_delay = m_maxDelay + m_minDelay - m_pWfsDelay[virtualMicIdx];
        float reverse_delay_prevFrame = m_maxDelay_prevFrame + m_minDelay_prevFrame - m_pWfsDelay_prevFrame[virtualMicIdx];

        // Delay for normal WFS source
        float delay = m_timeReversal * reverse_delay + (1 - m_timeReversal) * m_pWfsDelay[virtualMicIdx];
        float delay_prevFrame = m_timeReversal * reverse_delay_prevFrame + (1 - m_timeReversal) * m_pWfsDelay_prevFrame[virtualMicIdx];

        // Convert delay unity from milliseconds to sample
        float delaySample = m_sampleRate * delay / 1000.0f;
        float delaySample_prevFrame = m_sampleRate * delay_prevFrame / 1000.0f;

        //int delayBufferSize = sizeof(m_pDelayBuffer) / sizeof(*m_pDelayBuffer);

        for (int i = 0; i < bufferLength; i++)
        {
            // Get the current index of the sample in the delay buffer
            //int idx = delayBufferSize - bufferLength - (int)delaySample + i;
            int idx = m_delayBufferSize - bufferLength - (int)delaySample + i;
            
            // Get the current index of the sample in the delay buffer corresponding to the previous value
            //int idx_prevFrame = delayBufferSize - bufferLength - (int)delaySample_prevFrame + i;
            int idx_prevFrame = m_delayBufferSize - bufferLength - (int)delaySample_prevFrame + i;

            //if (idx >= 0 && idx < delayBufferSize && idx_prevFrame >= 0 && idx_prevFrame < delayBufferSize) {
            if (idx >= 0 && idx < m_delayBufferSize && idx_prevFrame >= 0 && idx_prevFrame < m_delayBufferSize) {

                // Value of the fade
                float fadeOut = ((float)bufferLength - (float)i) / (float)bufferLength;
                float sample_prevFrame=0, sample_currFrame=0;
                
                // if the source is "Directive", we get a weighted sum of the two choosen input channel and apply smooth volume and delay
                if (isDirective == true) {
                    // NO DIRECTIVE SOURCE
                    /*
                    sample_prevFrame = m_pWfsVolume_prevFrame[virtualMicIdx] * (m_ChannelWeight[virtualMicIdx][0][1] * m_pDelayMultiChannelBuffer[(int)m_ChannelWeight[virtualMicIdx][0][0]][idx_prevFrame]
                        + m_ChannelWeight[virtualMicIdx][1][1] * m_pDelayMultiChannelBuffer[(int)m_ChannelWeight[virtualMicIdx][1][0]][idx_prevFrame]);
                    sample_currFrame = m_pWfsVolume[virtualMicIdx] * (m_ChannelWeight[virtualMicIdx][0][1] * m_pDelayMultiChannelBuffer[(int)m_ChannelWeight[virtualMicIdx][0][0]][idx]
                        + m_ChannelWeight[virtualMicIdx][1][1] * m_pDelayMultiChannelBuffer[(int)m_ChannelWeight[virtualMicIdx][1][0]][idx]);
                    */
                }
                // if the source is "Non Directive", we use directly the first channel of the input and apply smooth volume and delay
                else {
                    // Crossfade value from current and previous parameter
                    sample_prevFrame = m_pWfsVolume_prevFrame[virtualMicIdx] * m_pDelayBuffer[idx_prevFrame];
                    sample_currFrame = m_pWfsVolume[virtualMicIdx] * m_pDelayBuffer[idx];
                }

                // Apply cross-fading between sample with previous and current parameter to avoid clicks (smooth parameter changes)
                m_pTmpMonoBuffer_in[i] = fadeOut * sample_prevFrame + (1 - fadeOut) * sample_currFrame;
            }
            else {
                m_pTmpMonoBuffer_in[i] = 0;
            }
        }

    }

    /****************************************************************************
    * 
    *                            MAIN PROCESS METHOD    
    * 
    *****************************************************************************/

    int At_WfsSpatializer::process(float* inBuffer, float* outBuffer, int bufferLength, int inChannelCount, int outChannelCount) {

#ifdef DEBUG_LOAD

        for (int virtualMicIndex = 0; virtualMicIndex < outChannelCount; virtualMicIndex++) {
            for (int sampleIndex = 0; sampleIndex < bufferLength; sampleIndex++) {
                outBuffer[outChannelCount * sampleIndex + virtualMicIndex] = inBuffer[inChannelCount * sampleIndex];
            }
        }
#else 

        // NO DIRECTIVE SOURCE
        // force no directive source
        m_isDirective == false;
        // -------------------

        float direction[3];

        m_virtualMicCount = outChannelCount;

        updateWfsVolumeAndDelay();
        updateMixMaxDelay();

        if (m_isDirective == true) {
            // NO DIRECTIVE SOURCE
            /*
            updateMixedDirectiveChannel(m_virtualMicCount, inChannelCount); 
            updateMultichannelDelayBuffer(inBuffer, bufferLength, inChannelCount);
            */
        }
        else if (m_isDirective == false) {
            forceMonoInput(inBuffer, bufferLength, inChannelCount);            
            updateDelayBuffer(bufferLength);
        }     


        float* inBufferInit = inBuffer;
        float volumeSum = 0;
        float wfsVolume, wfsDelay;

        for (int virtualMicIndex = 0; virtualMicIndex < m_virtualMicCount; virtualMicIndex++) {

            // APPLY WFS PANNING - GAIN AND DELAY
            applyWfsGainDelay(virtualMicIndex, m_virtualMicCount, bufferLength, m_isDirective); //modif mathias 06-17-2021

            // m_virtualMicCount are supposed to be equal to outChannelCount !!!!! Why 2 differents variables !!!
            for (int sampleIndex = 0; sampleIndex < bufferLength; sampleIndex++) {
                outBuffer[m_virtualMicCount * sampleIndex + virtualMicIndex] = m_pTmpMonoBuffer_in[sampleIndex];                
            }

            // save spatialization parameter for the next frame
            m_pWfsVolume_prevFrame[virtualMicIndex] = m_pWfsVolume[virtualMicIndex];
            m_pWfsDelay_prevFrame[virtualMicIndex] = m_pWfsDelay[virtualMicIndex];
        }
        m_minDelay_prevFrame = m_minDelay;
        m_maxDelay_prevFrame = m_maxDelay;
 
#endif

        return 0;
    }


    /****************************************************************************
    *
    *                            PARAMETERS SETTING 
    *
    *****************************************************************************/

    int At_WfsSpatializer::setSourcePosition(float* position, float* rotation, float* forward) { //modif mathias 06-14-2021

        m_sourcePosition[0] = position[0];
        m_sourcePosition[1] = position[1];
        m_sourcePosition[2] = position[2];
        
        //modif mathias 06-14-2021
        m_sourceRotation[0] = rotation[0];
        m_sourceRotation[1] = rotation[1];
        m_sourceRotation[2] = rotation[2];
        
        //modif mathias 06-14-2021
        m_sourceForward[0] = forward[0];
        m_sourceForward[1] = forward[1];
        m_sourceForward[2] = forward[2];
        return 0;
    }

    int At_WfsSpatializer::setSourceAttenuation(float attenuation) {
        m_attenuation = attenuation;
        return 0;

    }
    int At_WfsSpatializer::setSourceOmniBalance(float omniBalance)
    {
        m_omniBalance = omniBalance;
        return 0;
    }
    int At_WfsSpatializer::setTimeReversal(float timeReversal) {

        m_timeReversal = timeReversal;
        return 0;
    }
    int At_WfsSpatializer::setMinDistance(float minDistance) {

        m_minDistance = minDistance;
        return 0;
    }


    int At_WfsSpatializer::setVirtualMicPosition(int virtualMicCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards) {

        if (positions != NULL && rotations != NULL)
        {
            m_virtualMicMinDistance = virtualMicMinDistance;
            for (int i = 0; i < virtualMicCount; i++) {
                m_pVirtualMicPositions[i][0] = positions[i*3];
                m_pVirtualMicPositions[i][1] = positions[i*3+1];
                m_pVirtualMicPositions[i][2] = positions[i*3+2];
                
                m_pVirtualMicRotations[i][0] = rotations[i*3];
                m_pVirtualMicRotations[i][1] = rotations[i*3+1];
                m_pVirtualMicRotations[i][2] = rotations[i*3+2];

                m_pVirtualMicForwards[i][0] = forwards[i * 3];
                m_pVirtualMicForwards[i][1] = forwards[i * 3 + 1];
                m_pVirtualMicForwards[i][2] = forwards[i * 3 + 2];
            }
        }        
        return 0;
    }

    int At_WfsSpatializer::setListenerPosition(float* position, float* rotation) {
        m_listenerPosition[0] = position[0];
        m_listenerPosition[1] = position[1];
        m_listenerPosition[2] = position[2];
        m_listenerRotation[0] = position[0];
        m_listenerRotation[1] = position[1];
        m_listenerRotation[2] = position[2];
        return 0;
    }
    void At_WfsSpatializer::setSampleRate(float sampleRate) {
        m_sampleRate = sampleRate;
    }   

    void At_WfsSpatializer::setIs3DIsDirective(bool is3D, bool isDirective) {
        m_is3D = is3D;
        m_isDirective = isDirective;
    }

}
