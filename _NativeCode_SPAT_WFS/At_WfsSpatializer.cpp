#include "At_WfsSpatializer.h"

#include <iostream>

#include "AudioPluginUtil.h"

#include "At_SpatializationEngine.h"

//#define DEBUG_LOAD

namespace Spatializer
{

    int At_WfsSpatializer::m_numInstance = 0;
    int At_WfsSpatializer::m_numInstanceProcessed = 0;
    
    At_WfsSpatializer::At_WfsSpatializer(int sampleRate) {        
        m_numInstance++;
        std::cout << "Spatializer " << spatID << " created !" << " \n";
        std::cout << "Num Instance : " << m_numInstance << " \n";        
        m_pLowPass = new Biquad(bq_type_lowpass, 20000, 0.707, 0, sampleRate);
        m_pHighPass = new Biquad(bq_type_highpass, 20, 0.707, 0, sampleRate);
    }

    

    At_WfsSpatializer::~At_WfsSpatializer() {
        m_numInstance--;
        
        std::cout << "Spatializer " << spatID << " destroyed !" << " \n";
        std::cout << "Num Instance : " << m_numInstance << " \n";

        delete m_pLowPass;
        delete m_pHighPass;

        //delete[] m_pDelayBuffer;
        //m_pDelayBuffer = NULL;
    }

    /****************************************************************************
    *
    *           GET THE FIRST CHANNEL OHF THE MULTICHANNEL INPUT BUFFER              
    *                       (FOR NON-DIRECTIVE SOURCE ONLY)
    *
    *****************************************************************************/
    void At_WfsSpatializer::forceMonoInput(float* inBuffer, int bufferLength, int offset, int inchannels) {

        int count = 0;
        for (int i = 0; i < bufferLength * inchannels; i += inchannels) {
            m_pTmpMonoBuffer_in[count] = inBuffer[i+ offset];
            count++;
        }

    }

    // Modif Gonot - 13/03/2023
    // Now, we apply the roll-off curve on the input buffer
    void At_WfsSpatializer::forceMonoInputAndApplyRolloff(float* inBuffer, int bufferLength, int offset, int inchannels) {

        float rolloff;
        float direction[3];
        direction[0] = m_sourcePosition[0] - m_listenerPosition[0];
        direction[1] = m_sourcePosition[1] - m_listenerPosition[1];
        direction[2] = m_sourcePosition[2] - m_listenerPosition[2];

        float virtualMicDistance = sqrtf(pow(direction[0], 2) + pow(direction[1], 2) + pow(direction[2], 2));
        if (virtualMicDistance < m_minDistance || m_attenuation == 0) {
            rolloff = 1;
        }
        else {
            rolloff = 1.0f / pow((virtualMicDistance - m_minDistance) + 1, m_attenuation);
        }

        int count = 0;
        for (int i = 0; i < bufferLength * inchannels; i += inchannels) {
            //m_pTmpMonoBuffer_in[count] = (float)m_pLowPass->process(m_pHighPass->process(rolloff * inBuffer[i + offset]));
            //std::cout << "Before Low pass " << rolloff * inBuffer[i + offset] << " \n";
            m_pTmpMonoBuffer_in[count] = (float)m_pLowPass->process(rolloff * inBuffer[i + offset]);
            //std::cout << "After Low pass " << m_pTmpMonoBuffer_in[count] << " \n";
            
           // m_pTmpMonoBuffer_in[count] = rolloff * inBuffer[i + offset];
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
            
            // Modif Gonot - 13/03/2023
            // rolloff curve is applyied on the source itself (input buffer), not on the secondary source.
            /*
            float rolloff;            
            if (virtualMicDistance < m_minDistance || m_attenuation == 0) {
                rolloff = 1;
            }
            else {
                rolloff = 1.0f / pow((virtualMicDistance - m_minDistance) + 1, m_attenuation);
            }
            */


            // Modif Gonot - 13/03/2023
            // virtual mics are always cardiods.... But there directions switches when the source is inside the speaker setup
            float fwd[3];
            if (m_timeReversal == 0) {
                fwd[0] = m_pVirtualMicForwards[virtualMicIdx][0];
                fwd[1] = m_pVirtualMicForwards[virtualMicIdx][1];
                fwd[2] = m_pVirtualMicForwards[virtualMicIdx][2];
            }
            else {
                fwd[0] = -m_pVirtualMicForwards[virtualMicIdx][0];
                fwd[1] = -m_pVirtualMicForwards[virtualMicIdx][1];
                fwd[2] = -m_pVirtualMicForwards[virtualMicIdx][2];
            }
            float forwardProj = normalizedDirection[0] * fwd[0]
                + normalizedDirection[1] * fwd[1]
                + normalizedDirection[2] * fwd[2];
            /*
            float forwardProj = normalizedDirection[0] * m_pVirtualMicForwards[virtualMicIdx][0]
                + normalizedDirection[1] * m_pVirtualMicForwards[virtualMicIdx][1]
                + normalizedDirection[2] * m_pVirtualMicForwards[virtualMicIdx][2];
            */

            
            // Modif Gonot - 13/03/2023
            // 1) virtual mics are always cardiods.... But there directions switches when the source is inside the speaker setup (focused source)
            //float cardioidSens = m_omniBalance + (1 - m_omniBalance) * (0.5f * (1 + forwardProj));            
            
            // 2) rolloff is not applyied for each virtual speakers but a the source itself - TODO !!!!!!!
            //m_pWfsVolume[virtualMicIdx] = cardioidSens * rolloff;
            
            m_pWfsVolume[virtualMicIdx] = 0.5f * (1 + forwardProj);



            //std::cout << "Mic : " << virtualMicIdx << " - volume : " << m_pWfsVolume[virtualMicIdx] << " \n";
        }


    }

    void At_WfsSpatializer::updateMixMaxDelay() {
        m_maxDelay = m_pWfsDelay[0];
        m_minDelay = m_pWfsDelay[0];
        for (int virtualMicIdx = 1; virtualMicIdx < m_virtualMicCount; virtualMicIdx++) {
            if (m_pWfsDelay[virtualMicIdx] >= m_maxDelay) {
                m_maxDelay = m_pWfsDelay[virtualMicIdx];
            }
            if (m_pWfsDelay[virtualMicIdx] <= m_minDelay) {
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
            
#ifdef RING_BUFFER
            m_pDelayRingBuffer[i] = 0;
#else
            m_pDelayBuffer[i] = 0;
#endif
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

        
#ifdef RING_BUFFER
        m_pDelayRingBuffer = new float[m_delayBufferSize];
#else
        m_pDelayBuffer = new float[m_delayBufferSize];
#endif
        //std::cout << "Init Delay Buffer with " << m_delayBufferSize << " samples for distance = " << m_maxDistanceForDelay << " \n";

        //for (int i = 0; i < MAX_BUFFER_SIZE * NUM_BUFFER_IN_DELAY; i++) {
        for (int i = 0; i < m_delayBufferSize; i++) {

#ifdef RING_BUFFER
            m_pDelayRingBuffer[i] = 0;
#else
            m_pDelayBuffer[i] = 0;
#endif
        }
#endif
        //std::cout << "last sample of delay buffer is : " << m_pDelayBuffer[m_delayBufferSize-1] << " \n";
    }
#ifdef RING_BUFFER
    void At_WfsSpatializer::updateDelayRingBuffer(int bufferLength) {  
        
        for (int i = 0; i < bufferLength; i ++)
        {
            m_pDelayRingBuffer[mDelayRingBuffeWriteIndex] = m_pTmpMonoBuffer_in[i];
            mDelayRingBuffeWriteIndex++;
            if (mDelayRingBuffeWriteIndex == m_delayBufferSize)
                mDelayRingBuffeWriteIndex = 0;
        }
        
    }
#endif

#ifndef RING_BUFFER
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
#endif

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
        m_pProcess_delay[virtualMicIdx] = m_timeReversal * reverse_delay + (1 - m_timeReversal) * m_pWfsDelay[virtualMicIdx];
        float delay_prevFrame = m_timeReversal * reverse_delay_prevFrame + (1 - m_timeReversal) * m_pWfsDelay_prevFrame[virtualMicIdx];

        //std::cout << " delay  " << virtualMicIdx << " = " << m_pProcess_delay[virtualMicIdx] << "\n";
        /*
        std::cout << " max delay  " << m_maxDelay << "\n";
        std::cout << " min delay  " << m_minDelay << "\n";
        */

        // Convert delay unity from milliseconds to sample
        float delaySample = m_sampleRate * m_pProcess_delay[virtualMicIdx] / 1000.0f;
        float delaySample_prevFrame = m_sampleRate * delay_prevFrame / 1000.0f;

        // clip the value of the delay (avoiding access outside circular buffer capcity)
        if (delaySample > m_delayBufferSize - bufferLength)
            delaySample = m_delayBufferSize - bufferLength;

#ifdef RING_BUFFER
        // get the index to read in the circular buffer
        int delayRingBuffeReadIndex = mDelayRingBuffeWriteIndex - delaySample - bufferLength;
        int delayRingBuffeReadIndex_prevFrame = mDelayRingBuffeWriteIndex - delaySample_prevFrame - bufferLength;

        // apply "modulo" to the read index
        if (delayRingBuffeReadIndex < 0)
            delayRingBuffeReadIndex = m_delayBufferSize + delayRingBuffeReadIndex;

        // apply "modulo" to the read index
        if (delayRingBuffeReadIndex_prevFrame < 0)
            delayRingBuffeReadIndex_prevFrame = m_delayBufferSize + delayRingBuffeReadIndex_prevFrame;
#endif

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
                    
#ifdef RING_BUFFER
                    
                    // Modif Rougerie 29/06/2022
                    sample_prevFrame = m_pWfsSpeakerMask_prevFrame[virtualMicIdx] * m_pWfsVolume_prevFrame[virtualMicIdx] * m_pDelayRingBuffer[delayRingBuffeReadIndex_prevFrame];
                    sample_currFrame = m_pWfsSpeakerMask[virtualMicIdx] * m_pWfsVolume[virtualMicIdx] * m_pDelayRingBuffer[delayRingBuffeReadIndex];
                    

                    delayRingBuffeReadIndex_prevFrame++;
                    delayRingBuffeReadIndex++;

                    
                    if (delayRingBuffeReadIndex == m_delayBufferSize)
                        delayRingBuffeReadIndex = 0;
                    if (delayRingBuffeReadIndex_prevFrame == m_delayBufferSize)
                        delayRingBuffeReadIndex_prevFrame = 0;

#else
                    sample_prevFrame = m_pWfsVolume_prevFrame[virtualMicIdx] * m_pDelayBuffer[idx_prevFrame];
                    sample_currFrame = m_pWfsVolume[virtualMicIdx] * m_pDelayBuffer[idx];
#endif
                    /*
                    std::cout << "prev :" << delayRingBuffeReadIndex << " \n";
                    std::cout << "cur :" << delayRingBuffeReadIndex_prevFrame << " \n";
                  */

                }

                // Apply cross-fading between sample with previous and current parameter to avoid clicks (smooth parameter changes)
                m_pTmpMonoBuffer_in[i] = fadeOut * sample_prevFrame + (1 - fadeOut) * sample_currFrame;
                // if the sample is NaN
                if (m_pTmpMonoBuffer_in[i] != m_pTmpMonoBuffer_in[i]) {
                    m_pTmpMonoBuffer_in[i] = 0;
                }
               
               
            }
            else {
                m_pTmpMonoBuffer_in[i] = 0;
            }
        }

    }

    int At_WfsSpatializer::WFS_getDelay(float* delay, int arraySize) {
        if (arraySize <= MAX_OUTPUT_CHANNEL) {
            for (int i = 0; i < arraySize; i++) {  
                
                //delay[i] = m_pProcess_delay[i];
                // should be :
                delay[i] = m_pProcess_delay[i] * m_pWfsSpeakerMask[i];
            }
        }
        return 0;
    }

    int At_WfsSpatializer::WFS_getVolume(float* volume, int arraySize) {
        if (arraySize <= MAX_OUTPUT_CHANNEL) {
            for (int i = 0; i < arraySize; i++) {
                //volume[i] = m_pWfsVolume[i];
                // should be : 
                volume[i] = m_pWfsVolume[i]* m_pWfsSpeakerMask[i];
            }
        }
        return 0;
    }

    /****************************************************************************
    * 
    *                            MAIN PROCESS METHOD    
    * 
    *****************************************************************************/

    int At_WfsSpatializer::process(float* inBuffer, float* outBuffer, int bufferLength, int offset, int inChannelCount, int outChannelCount) {

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
            // Modif Gonot - 13/03/2023
            // Now, we apply the roll-off curve on the input buffer
            forceMonoInputAndApplyRolloff(inBuffer, bufferLength, offset, inChannelCount);
            //forceMonoInput(inBuffer, bufferLength, offset, inChannelCount);
            
#ifdef RING_BUFFER
            updateDelayRingBuffer(bufferLength);
#else
            updateDelayBuffer(bufferLength);
#endif
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
                
                // Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
                if (m_pEngineMixingBuffer != NULL)
                    m_pEngineMixingBuffer[m_virtualMicCount * sampleIndex + virtualMicIndex] += m_pTmpMonoBuffer_in[sampleIndex];
                if (m_pTmpEngineMixingBuffer_hp != NULL)
                    m_pTmpEngineMixingBuffer_hp[m_virtualMicCount * sampleIndex + virtualMicIndex] = m_pEngineMixingBuffer[m_virtualMicCount * sampleIndex + virtualMicIndex];
                if(m_pTmpEngineMixingBufferSub_lp != NULL)
                    m_pTmpEngineMixingBufferSub_lp[m_virtualMicCount * sampleIndex + virtualMicIndex] = m_pEngineMixingBuffer[m_virtualMicCount * sampleIndex + virtualMicIndex];
            }

            // save spatialization parameter for the next frame
            m_pWfsVolume_prevFrame[virtualMicIndex] = m_pWfsVolume[virtualMicIndex];
            m_pWfsDelay_prevFrame[virtualMicIndex] = m_pWfsDelay[virtualMicIndex];
            m_pWfsSpeakerMask_prevFrame[virtualMicIndex] = m_pWfsSpeakerMask[virtualMicIndex]; // Modif Rougerie 29/06/2022

        }
        m_minDelay_prevFrame = m_minDelay;
        m_maxDelay_prevFrame = m_maxDelay;
        
        /*
        m_numInstanceProcessed++;
        std::cout << "Num Instance processed : " << m_numInstanceProcessed << " \n";
        std::cout << "Num Instance : " << m_numInstance << " \n";
        if (m_numInstanceProcessed >= m_numInstance) {
            m_numInstanceProcessed = 0;
            std::cout << "Zero Mixing Buffer : \n";
            for (int i = 0; i < m_bufferLength*m_outChannelCount+m_subwooferOutputChannelCount ; i++) m_pEngineMixingBuffer[i] = 0;
        }
        */
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

    void At_WfsSpatializer::setLowPassFc(float fc) {
        if (m_pLowPass != NULL)
            m_pLowPass->setFc(fc);
    }
    void At_WfsSpatializer::setHighPassFc(float fc) {
        if (m_pHighPass != NULL)
            m_pHighPass->setFc(fc);
    }
    void At_WfsSpatializer::setLowPassGain(float gain) {
        if (m_pLowPass != NULL)
            m_pLowPass->setPeakGain(gain);
    }
    void At_WfsSpatializer::setHighPassGain(float gain) {
        if (m_pHighPass != NULL)
            m_pHighPass->setPeakGain(gain);
    }

    // Modif Rougerie 29/06/2022
    int At_WfsSpatializer::setSpeakerMask(float* activationSpeakerVolume, int outChannelCount)
    {
        for (int i = 0; i < outChannelCount; i++)
        {
            m_pWfsSpeakerMask[i] = activationSpeakerVolume[i];
        }
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
