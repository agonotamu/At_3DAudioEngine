#include "At_WfsSpatializer.h"

#include <iostream>

#include "AudioPluginUtil.h"

#include "At_SpatializationEngine.h"

namespace Spatializer
{

    void At_WfsSpatializer::applyWfsGainDelay(int virtualMicIdx, int bufferLength, bool isDirective) { //modif mathias 06-17-2021


        float reverse_delay = m_maxDelay + m_minDelay - m_pWfsDelay[virtualMicIdx];
        float delay = m_timeReversal * reverse_delay + (1 - m_timeReversal) * m_pWfsDelay[virtualMicIdx];

        float delaySample = m_sampleRate * delay / 1000.0f;

        int delayBufferSize = sizeof(m_pDelayBuffer) / sizeof(*m_pDelayBuffer);
       
        if (virtualMicIdx == 0) {

#ifdef DEBUGLOG
            std::cout << spatID << ": minDistance =" << m_minDistance << " - delaySample[0] : " << delaySample << " - volume[0] : " << m_pWfsVolume[virtualMicIdx] << "\n";
#endif
            //std::cout << "Mic : " << virtualMicIdx << " - Channel1 : " << m_ChannelWeight[virtualMicIdx][0][0] << " - Weight1 : " << m_ChannelWeight[virtualMicIdx][0][1] << " - Channel2 : " << (int)m_ChannelWeight[virtualMicIdx][1][0] << " - Weight2 : " << m_ChannelWeight[virtualMicIdx][1][1] << " \n";
            //std::cout << "Mic : " << virtualMicIdx << " - Channel1 : " << indexChannel1 << " - Weight1 : " << weight1 << " - Channel2 : " << indexChannel1 << " - Weight2 : " << weight1 << " \n";

        }

        for (int i = 0; i < bufferLength; i++)
        {
            int idx = delayBufferSize - bufferLength - (int)delaySample + i;

            if (idx >= 0 && idx < delayBufferSize) {
                //modif mathias 06-17-2021
                if (isDirective == true) {
                    // on copie dans m_pTmpMonoBuffer_in la somme des deux canaux pondérés et retardés et atténué
                    m_pTmpMonoBuffer_in[i] = m_pWfsVolume[virtualMicIdx] *
                        (m_ChannelWeight[virtualMicIdx][0][1] * m_pDelayMultiChannelBuffer[(int)m_ChannelWeight[virtualMicIdx][0][0]][idx]
                            + m_ChannelWeight[virtualMicIdx][1][1] * m_pDelayMultiChannelBuffer[(int)m_ChannelWeight[virtualMicIdx][1][0]][idx]);
                }
                else {
                    m_pTmpMonoBuffer_in[i] = m_pWfsVolume[virtualMicIdx] * m_pDelayBuffer[idx];
                }         
            }
            else {    
                    m_pTmpMonoBuffer_in[i] = 0;
            }
        }
    }

    /*
    // 11/03/2021 - BUG CORRECTION- we do not use azimuth and elevation any more, but forward vector only.
    void At_WfsSpatializer::getWfsVolumeAndDelay(int virtualMicIndex, float virtualMicDistance, float* wfsVolume, float* wfsDelay) {
        
        float virtualMicAzimuth = m_pAzimuth[virtualMicIndex];
        float virtualMicElevation = m_pElevation[virtualMicIndex];
        
        float rolloff;
        if (virtualMicDistance < At_WfsSpatializer::m_virtualMicMinDistance) {
            rolloff = 1;
        }
        else {
            rolloff = 1.0f / pow((virtualMicDistance - At_WfsSpatializer::m_virtualMicMinDistance) + 1, 2.0f);
        }

        float azimuthCardioidSens = 0.5f * (1 + cos(kPI * virtualMicAzimuth / 180.0f));
        float elevationCardioidSens = 0.5f * (1 + cos(kPI * virtualMicElevation / 180.0f));

        //***************************
        // ATTENTION : IL FAUT PRENDRE EN COMPTE L'ORIENTATION DU MICROPHONE VIRTUEL POUR LA DIRECTIVITE CARDIOIDE !!!!
        //***************************
        *wfsVolume = azimuthCardioidSens * elevationCardioidSens * rolloff;
        if (virtualMicIndex == 0)

#ifdef DEBUGLOG
            //std::cout << "volume # " << virtualMicIndex << " : " << *wfsVolume << " \n";
#endif
        *wfsDelay = (virtualMicDistance / 340.0f) * 1000.0f; // time in milliseconds     
#ifdef DEBUGLOG
        //std::cout << "delay # " << virtualMicIndex << " : " << *wfsDelay << " \n";
#endif
    }
    */

    /*
    // 11/03/2021 - BUG CORRECTION- we do not use azimuth and elevation any more, but forward vector only.
    float At_WfsSpatializer::getAzimuth(int virtualMicIdx, float *direction) {

        static const float kRad2Deg = 180.0f / kPI;
        direction[0] = m_sourcePosition[0] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][0];
        direction[1] = m_sourcePosition[1] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][1];
        direction[2] = m_sourcePosition[2] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][2];

        float azimut = (fabsf(direction[2]) < 0.001f) ? 0.0f : atan2f(direction[0], direction[2]);
        if (azimut < 0.0f)
            azimut += 2.0f * kPI;
        azimut = FastClip(azimut * kRad2Deg, 0.0f, 360.0f);

        azimut -= m_pVirtualMicRotations[virtualMicIdx][1];
#ifdef DEBUGLOG
        //std::cout << "Azimut # "<< virtualMicIdx<< " : " << azimut <<" \n";
        
        if (virtualMicIdx == 0) {                    

            std::cout << "Azimut #" << virtualMicIdx << " : " << azimut << " \n";

        }
        
#endif
        return azimut;
    }
    */

    /*
    // 11/03/2021 - BUG CORRECTION- we do not use azimuth and elevation any more, but forward vector only.
    float At_WfsSpatializer::getElevation(int virtualMicIdx, float* direction) {

        static const float kRad2Deg = 180.0f / kPI;
        direction[0] = m_sourcePosition[0] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][0];
        direction[1] = m_sourcePosition[1] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][1];
        direction[2] = m_sourcePosition[2] - At_WfsSpatializer::m_pVirtualMicPositions[virtualMicIdx][2];

        float elevation = (fabsf(direction[2]) < 0.001f) ? 0.0f : atan2f(direction[1], direction[2]);
        
        if (elevation < 0.0f)
            elevation += 2.0f * kPI;
        elevation = FastClip(elevation * kRad2Deg, 0.0f, 360.0f);
        
        elevation -= m_pVirtualMicRotations[virtualMicIdx][0];
#ifdef DEBUGLOG
        //std::cout << "Elevation # "<< virtualMicIdx<< " : " << elevation <<" \n";
        
        if (virtualMicIdx == 0) {
            //std::cout << "Direction #" << virtualMicIdx << " : " << direction[0] <<" " << direction[1] << " " << direction[2] << " \n";
            std::cout << "Elevation #" << virtualMicIdx << " : " << elevation << " \n";
            //std::cout << "Virtual Mic rot #" << virtualMicIdx << " : " << m_pVirtualMicRotations[virtualMicIdx][0] << " \n";

        }
        
#endif
        return elevation;
    }
*/
    //modif mathias 06-17-2021
    void At_WfsSpatializer::setIs3DIsDirective(bool is3D, bool isDirective) {
        m_is3D = is3D;
        m_isDirective = isDirective;
    }

    void At_WfsSpatializer::forceMonoInput(float* inBuffer, int bufferLength, int inchannels) {

        int count = 0;
        for (int i = 0; i < bufferLength * inchannels; i += inchannels) {
            m_pTmpMonoBuffer_in[count] = inBuffer[i];
            count++;
        }       

    }

    // modif Mathias 06-14-2021
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

        // TODO ---------------------------
        // a partir de la position de la source, de son vectore forward (ou sa rotation/vector forward) et de la position des Virtual Mic
        // donner l'index des deux canaux à sommer ainsi que la pondération....
        // NB : modfier AT_SPAT_WFS_setSourcePosition(int id, float* position) pour avoir le vecteur forward

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

                indexChannel2 = indexChannel1 + 1;

                tmp = (int)indexChannels;
                if (tmp == indexChannels) {
                    weight1 = 1;
                }
                else { 
                    weight1 = 1 - (indexChannels - indexChannel1);
                }

                weight2 = 1 - weight1;

                if (indexChannel2 >= inChannelCount) {
                    indexChannel2 = 0;
                }

                m_ChannelWeight[virtualMicIdx][0][0] = indexChannel1;
                m_ChannelWeight[virtualMicIdx][0][1] = weight1;
                m_ChannelWeight[virtualMicIdx][1][0] = indexChannel2;
                m_ChannelWeight[virtualMicIdx][1][1] = weight2;

                if (virtualMicIdx == 0) {
                    
                    //std::cout << "Mic : " << virtualMicIdx << " - Channel1 : " << m_ChannelWeight[virtualMicIdx][0][0] << " - Weight1 : " << m_ChannelWeight[virtualMicIdx][0][1] << " - Channel2 : " << (int)m_ChannelWeight[virtualMicIdx][1][0] << " - Weight2 : " << m_ChannelWeight[virtualMicIdx][1][1] << " \n";
                    //std::cout << "Mic : " << virtualMicIdx << " - Channel1 : " << indexChannel1 << " - Weight1 : " << weight1 << " - Channel2 : " << indexChannel1 << " - Weight2 : " << weight1 << " \n";
                   
                }
            }
    
            
        // --------------------------------

    }

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



            //*wfsDelay = (virtualMicDistance / 340.0f) * 1000.0f; // time in milliseconds
            m_pWfsDelay[virtualMicIdx] = (virtualMicDistance / 340.0f) * 1000.0f; // time in milliseconds;

            float rolloff;
            //if (virtualMicDistance < At_WfsSpatializer::m_virtualMicMinDistance || m_attenuation == 0) {
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

            //*wfsVolume = cardioidSens * rolloff;
            m_pWfsVolume[virtualMicIdx] = cardioidSens * rolloff;


#ifdef DEBUGLOG
            /*
            if (virtualMicIdx == 0)
            {
                std::cout << "volume # " << virtualMicIdx << " : " << m_pWfsVolume[virtualMicIdx] << " \n";
                std::cout << "delay # " << virtualMicIdx << " : " << m_pWfsDelay[virtualMicIdx] << " \n";
            }
            */
#endif
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
    //modif mathias 06-17-2021
    void At_WfsSpatializer::updateMultichannelDelayBuffer(float* inBuffer, int bufferLength, int inChannelCount) {

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
    }
    
    void At_WfsSpatializer::updateDelayBuffer(int bufferLength) {

        int arrayLength = sizeof(m_pDelayBuffer) / sizeof(*m_pDelayBuffer);
        int numLengthInDelBuf = arrayLength / bufferLength;

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




    int At_WfsSpatializer::process(float* inBuffer, float* outBuffer, int bufferLength, int inChannelCount, int outChannelCount) {

        
        //std::cout << "-------------------------------------- PROCESS -------------------------------- " << "\n";
        float direction[3];

        m_virtualMicCount = outChannelCount;

        updateWfsVolumeAndDelay();
        updateMixMaxDelay();

        // modif Mathias 06-14-2021
        // si isDirectionnalSource == true
        if (m_isDirective == true) {
            updateMixedDirectiveChannel(m_virtualMicCount, inChannelCount); 
            updateMultichannelDelayBuffer(inBuffer, bufferLength, inChannelCount);

        }
        // si isDirectionnalSource == false
        else if (m_isDirective == false) {
            forceMonoInput(inBuffer, bufferLength, inChannelCount);
            updateDelayBuffer(bufferLength);
        }     

        float* inBufferInit = inBuffer;

        float volumeSum = 0;
        // FOR WFS PANNING - GAIN AND DELAY
        float wfsVolume, wfsDelay;

        for (int virtualMicIndex = 0; virtualMicIndex < m_virtualMicCount; virtualMicIndex++) {

            // APPLY WFS PANNING - GAIN AND DELAY
            applyWfsGainDelay(virtualMicIndex, bufferLength, m_isDirective); //modif mathias 06-17-2021
            

            // m_virtualMicCount are supposed to be equal to outChannelCount !!!!! Why 2 differents variables !!!
            for (int sampleIndex = 0; sampleIndex < bufferLength; sampleIndex++) {
                outBuffer[m_virtualMicCount * sampleIndex + virtualMicIndex] = m_pTmpMonoBuffer_in[sampleIndex];
                //outBuffer[sample * m_virtualMicCount + virtualMicIndex] = m_pTmpMonoBuffer_in[sample];
            }
        }

        return 0;
    }

    int At_WfsSpatializer::setSourcePosition(float* position, float* rotation, float* forward) { //modif mathias 06-14-2021
#ifdef DEBUGLOG
        //std::cout << "Set position Position !\n";
        //std::cout << "Set source position Position (x) : "<< position[0] << " " << position[1] << " " << position[2] << " " <<" \n";
#endif
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
        
        //  std::cout << "Set Virtual Mic Position !\n";
        if (positions != NULL && rotations != NULL)
        {
            //m_virtualMicCount = virtualMicCount;
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
        else {
            //std::cout << "Array NULL !\n";
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
}
