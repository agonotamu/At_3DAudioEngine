

#include "At_SpatializationEngine.h"
#include <limits.h>

namespace Spatializer
{
	At_SpatializationEngine::At_SpatializationEngine() {

#ifdef DEBUGLOG
		FILE* pConsole;
		AllocConsole();
		freopen_s(&pConsole, "CONOUT$", "wb", stdout);

		std::cout << "Spat Engine created !\n";
#endif

	}
	
	At_SpatializationEngine::~At_SpatializationEngine() {

#ifdef DEBUGLOG
		std::cout << "Clear all Spatializer from destructor !\n";
		std::cout << "m_pWfsSpatializerList is size : " << m_pWfsSpatializerList.size() << "\n";
#endif
		for (int i = 0; i < m_pWfsSpatializerList.size(); i++) {
			delete &m_pWfsSpatializerList[i];
		}
		// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
		delete m_pMixingBuffer;
		delete m_pTmpMixingBuffer_hp;
		delete m_pTmpMixingBufferSub_lp;
		delete m_pSubwooferHighpass;
		delete m_pSubwooferHighpass;

		m_pWfsSpatializerList.clear(); 
	}
	
	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	void At_SpatializationEngine::WFS_initializeOutput(int sampleRate, int bufferLength, int outChannelCount, int subwooferOutputChannelCount, float subwooferCutoff) {

		m_pMixingBuffer = new float[bufferLength * outChannelCount];
		for (int i = 0; i < bufferLength * outChannelCount; i++) {
			m_pMixingBuffer[i] = 0;
		}
		m_pTmpMixingBuffer_hp = new float[bufferLength * outChannelCount];
		m_pTmpMixingBufferSub_lp = new float[bufferLength * outChannelCount];

		m_pSubwooferHighpass = new Biquad[outChannelCount];
		for (int i = 0; i <  outChannelCount; i++) {			
			m_pSubwooferHighpass[i].setHigPassCoefficient_LinkwitzRiley(m_subwooferCutoff, sampleRate);
		}
		m_pSubwooferLowpass = new Biquad[outChannelCount];
		for (int i = 0; i < outChannelCount; i++) {
			m_pSubwooferLowpass[i].setLowPassCoefficient_LinkwitzRiley(m_subwooferCutoff, sampleRate);
		}

		

		m_bufferLength = bufferLength;
		m_outChannelCount = outChannelCount;
		m_subwooferOutputChannelCount = subwooferOutputChannelCount;
		m_subwooferCutoff = subwooferCutoff;
		m_sampleRate = sampleRate;


	}

	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	void At_SpatializationEngine::WFS_getDemultiplexMixingBuffer(float* demultiplexMixingBuffer, int indexChannel) {
		if (demultiplexMixingBuffer != NULL) {

			for (int i = 0; i < m_bufferLength; i++) {
				demultiplexMixingBuffer[i] = m_pMixingBuffer[i * m_outChannelCount + indexChannel];
				// clear mixing buffer for the next frame
				m_pMixingBuffer[i * m_outChannelCount + indexChannel] = 0;
			}
		}
	}
	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	void At_SpatializationEngine::WFS_fillOutputWithOneChannelOfMixingChannel(float* outBufferOneChannel, int indexChannel, int maxOutputChannel) {

		for (int indexSample = 0; indexSample < m_outChannelCount; indexSample++) {
			if (indexSample < maxOutputChannel) outBufferOneChannel[indexSample] = m_pMixingBuffer[indexSample * m_outChannelCount + indexChannel];
		}

	}
	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	float At_SpatializationEngine::WFS_getMixingBufferSampleForChannelAndZero(int indexSample, int indexChannel, bool isHighPassFiltered) {
		
		// get the value in the mixing buffer
		float value; 
		if (isHighPassFiltered) {
			value = m_pSubwooferHighpass[indexChannel].filter(m_pTmpMixingBuffer_hp[indexSample * m_outChannelCount + indexChannel]);
		}
		else {
			value = m_pMixingBuffer[indexSample * m_outChannelCount + indexChannel];
		}
		// set the value to zero in the buffer for the next frame
		m_pMixingBuffer[indexSample * m_outChannelCount + indexChannel] = 0;
		return value;
	}
	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	float At_SpatializationEngine::WFS_getLowPasMixingBufferForChannel(int indexSample, int indexChannel) {
		
		return m_pSubwooferLowpass[indexChannel].filter(m_pTmpMixingBufferSub_lp[indexSample * m_outChannelCount + indexChannel]);
	}


	void At_SpatializationEngine::WFS_destroyAllSpatializer() {
#ifdef DEBUGLOG
		std::cout << "Clear all Spatializer from destroy all !\n";
		std::cout << "m_pWfsSpatializerList is size : " << m_pWfsSpatializerList.size() << "\n";
#endif

		m_pWfsSpatializerList.clear();
		//delete& m_pWfsSpatializerList;
		incrementalUniqueID = 0;
	}

	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	bool At_SpatializationEngine::CreateWfsSpatializer(int* id, bool is3D, bool isDirective, float maxDistanceForDelay) { //modif mathias 06-17-2021

		if (m_pMixingBuffer != NULL) {
			// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
			//At_WfsSpatializer* s = new At_WfsSpatializer(this);
			At_WfsSpatializer* s = new At_WfsSpatializer();			
			s->m_pEngineMixingBuffer = m_pMixingBuffer;
			s->m_pTmpEngineMixingBuffer_hp = m_pTmpMixingBuffer_hp;
			s->m_pTmpEngineMixingBufferSub_lp = m_pTmpMixingBufferSub_lp;
			s->m_bufferLength = m_bufferLength;
			s->m_outChannelCount = m_outChannelCount;
			s->m_subwooferOutputChannelCount = m_subwooferOutputChannelCount;


			s->m_maxDistanceForDelay = maxDistanceForDelay;
			s->m_is3D = is3D; //modif mathias 06-17-2021
			s->m_isDirective = isDirective; //modif mathias 06-17-2021
			//s->spatID = m_pWfsSpatializerList.size() - 1;
			s->spatID = incrementalUniqueID;
			s->initDelayBuffer();
			m_pWfsSpatializerList.push_back(*s);
			*id = incrementalUniqueID;

			incrementalUniqueID++;
			return true;
		}
		else {
			return false;
		}
	
#ifdef DEBUGLOG
		//std::cout << "adding spatializer with spatID "<< s->spatID <<"\n";
		
#endif

	}

	void At_SpatializationEngine::DestroyWfsSpatializer(int id) {

#ifdef DEBUGLOG
		//std::cout << "destroy spatializer with index " << id << "\n";
#endif

		for (int i = 0; i < m_pWfsSpatializerList.size(); i++) {
			if (m_pWfsSpatializerList[i].spatID == id) {
				m_pWfsSpatializerList.erase(m_pWfsSpatializerList.begin() + i);
				break;
			}
		}
		

#ifdef DEBUGLOG
		//std::cout << "destroy spatializer with spatID "<< id<< "\n";
		//std::cout << "m_pWfsSpatializerList is size : " << m_pWfsSpatializerList.size() << "\n";
#endif

	}

	// One for all Spatializer ----------------------------------------------------------------------------------------
	void At_SpatializationEngine::WFS_setSampleRate(float sampleRatte) {
		
		for (int id = 0; id < m_pWfsSpatializerList.size(); id++) {
			m_pWfsSpatializerList[id].setSampleRate(sampleRatte);
		}
	}

	void At_SpatializationEngine::WFS_setListenerPosition(float* position, float* rotation) {
		for (int id = 0; id < m_pWfsSpatializerList.size();id++) {
			m_pWfsSpatializerList[id].setListenerPosition(position, rotation);
		}
	}

	void At_SpatializationEngine::WFS_setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards) {
		for (int id = 0; id < m_pWfsSpatializerList.size(); id++) {
			m_pWfsSpatializerList[id].setVirtualMicPosition(speakerCount, virtualMicMinDistance, positions, rotations, forwards);
		}
	}

	At_WfsSpatializer *At_SpatializationEngine::findSpatializerWithSpatID(int id) {

		At_WfsSpatializer *ws;

		for (int i = 0; i < m_pWfsSpatializerList.size(); i++) {
			if (m_pWfsSpatializerList[i].spatID == id) {
				ws = &m_pWfsSpatializerList[i];
				return ws;
			}
		}
		return NULL;
	}

	// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
	void At_SpatializationEngine::WFS_setSubwooferCutoff(float subwooferCutoff) {
		
		for (int i = 0; i < m_outChannelCount; i++) {
			m_pSubwooferHighpass[i].setHigPassCoefficient_LinkwitzRiley(m_subwooferCutoff, m_sampleRate);
		}
		
		for (int i = 0; i < m_outChannelCount; i++) {
			m_pSubwooferHighpass[i].setLowPassCoefficient_LinkwitzRiley(m_subwooferCutoff, m_sampleRate);
		}

	}

	// One for each Spatializer ---------------------------------------------------------------------------------------
	
	void At_SpatializationEngine::WFS_cleanDelayBuffer(int id) { 

		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->cleanDelayBuffer();
		}
		
	}
	

	void At_SpatializationEngine::WFS_setSourcePosition(int id, float* position, float* rotation, float* forward) { //modif mathias 06-14-2021
		
		At_WfsSpatializer *ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->setSourcePosition(position, rotation, forward); //modif mathias 06-14-2021
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setSourcePosition(position, rotation, forward); //modif mathias 06-14-2021
		}
		*/
	}

	void At_SpatializationEngine::WFS_setAttenuation(int id, float attenuation) {

		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->setSourceAttenuation(attenuation);
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setSourceAttenuation(attenuation);
		}
		*/
	}
	
	void At_SpatializationEngine::WFS_setSourceOmniBalance(int id, float omniBalance) {
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->setSourceOmniBalance(omniBalance);
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setSourceOmniBalance(omniBalance);
		}
		*/
	}

	void At_SpatializationEngine::WFS_setTimeReversal(int id, float timeReversal) {

		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->setTimeReversal(timeReversal);
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setTimeReversal(timeReversal);
		}
		*/
	}	
	void At_SpatializationEngine::WFS_setMinDistance(int id, float minDistance) {
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->setMinDistance(minDistance);
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setMinDistance(minDistance);
		}
		*/
	}
	// Modif Rougerie 29/06/2022
	void At_SpatializationEngine::WFS_setSpeakerMask(int id, float* activationSpeakerVolume, int outChannelCount) {
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->setSpeakerMask(activationSpeakerVolume, outChannelCount);
		}
	}


	void At_SpatializationEngine::WFS_process(int id, float* inBuffer, float* outBuffer, int bufferLength, int offset, int inChannelCount, int outChannelCount) {
		
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->process(inBuffer, outBuffer, bufferLength, offset, inChannelCount, outChannelCount);
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].process(inBuffer, outBuffer, bufferLength, inChannelCount, outChannelCount);
		}
		*/
	}

	void At_SpatializationEngine::WFS_getDelay(int id, float* delay, int arraySize) {
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->WFS_getDelay(delay, arraySize);
		}
	}
	void At_SpatializationEngine::WFS_getVolume(int id, float* volume, int arraySize) {
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->WFS_getVolume(volume, arraySize);
		}
	}


	

	
}