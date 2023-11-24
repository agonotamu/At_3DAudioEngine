#include "At_HapticMixer.h"



At_HapticMixer::At_HapticMixer() {

}

At_HapticMixer::~At_HapticMixer() {
	
	// CRASH !!!???
	/*
	for (int i = 0; i < m_pHapticSourceList.size(); i++) {
		delete& m_pHapticSourceList[i];
	}	

	delete m_pMixingBuffer;

	m_pHapticSourceList.clear();
	
	*/

}

bool At_HapticMixer::AddSource(int* sourceId) {

	if (m_pMixingBuffer != NULL) {

		At_HapticSource* hs = new At_HapticSource();
		
		hs->m_pMixingBuffer = m_pMixingBuffer;		
		hs->m_bufferLength = m_bufferLength;
		hs->m_outChannelCount = m_outChannelCount;

		hs->m_id = m_sourceIncrementalUniqueID;
		
		m_pHapticSourceList.push_back(*hs);
		*sourceId = m_sourceIncrementalUniqueID;

		m_sourceIncrementalUniqueID++;
		return true;
	}
	else {
		return false;
	}

}
void At_HapticMixer::RemoveSource(int sourceId) {

	for (int i = 0; i < m_pHapticSourceList.size(); i++) {
		if (m_pHapticSourceList[i].m_id == sourceId) {
			m_pHapticSourceList.erase(m_pHapticSourceList.begin() + i);
			break;
		}
	}


}

void At_HapticMixer::RemoveAllSources() {
	m_pHapticSourceList.clear();
	m_sourceIncrementalUniqueID = 0;
}

void At_HapticMixer::InitializeOutput(int mixerId, int sampleRate, int bufferLength, int outChannelCount) {
	
	m_id = mixerId;
	m_pMixingBuffer = new float[bufferLength * outChannelCount];
	for (int i = 0; i < bufferLength * outChannelCount; i++) {
		m_pMixingBuffer[i] = 0;
	}	
	m_bufferLength = bufferLength;
	m_outChannelCount = outChannelCount;
	m_sampleRate = sampleRate;
}

void At_HapticMixer::Process(int sourceId, float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume) {

	At_HapticSource* hs = findSourceWithID(sourceId);
	if (hs != NULL) {
		hs->Process(inBuffer, bufferLength, offset, inChannelCount, volume);
	}
}

float At_HapticMixer::GetMixingBufferSampleForChannelAndZero(int indexSample, int indexChannel) {

	// get the value in the mixing buffer
	float value = m_pMixingBuffer[indexSample * m_outChannelCount + indexChannel];	
	// set the value to zero in the buffer for the next frame
	m_pMixingBuffer[indexSample * m_outChannelCount + indexChannel] = 0;
	return value;
}


void At_HapticMixer::SetSourcePosition(int sourceId, float* position) {

	At_HapticSource* hs = findSourceWithID(sourceId);
	if (hs != NULL) {
		hs->SetSourcePosition(position);
	}
}

void At_HapticMixer::SetListenerPosition(float* position) {
	for (int id = 0; id < m_pHapticSourceList.size(); id++) {
		m_pHapticSourceList[id].SetListenerPosition(position);
	}
}

void At_HapticMixer::SetSourceAttenuation(int sourceId, float attenuation) {
	At_HapticSource* hs = findSourceWithID(sourceId);
	if (hs != NULL) {
		hs->SetSourceAttenuation(attenuation);
	}
}

void At_HapticMixer::SetSourceMinDistance(int sourceId, float minDistance) {
	At_HapticSource* hs = findSourceWithID(sourceId);
	if (hs != NULL) {
		hs->setSourceMinDistance(minDistance);
	}
}

At_HapticSource* At_HapticMixer::findSourceWithID(int sourceId) {

	At_HapticSource* hs;

	for (int i = 0; i < m_pHapticSourceList.size(); i++) {
		if (m_pHapticSourceList[i].m_id == sourceId) {
			hs = &m_pHapticSourceList[i];
			return hs;
		}
	}
	return NULL;
}