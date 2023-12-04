#include "At_HapticEngine.h"


/*****************************************************************/
At_HapticEngine::At_HapticEngine() {
#ifdef DEBUGLOG
	FILE* pConsole;
	AllocConsole();
	freopen_s(&pConsole, "CONOUT$", "wb", stdout);

	std::cout << "Spat Engine created !\n";
#endif
}

At_HapticEngine::~At_HapticEngine() {

	for (int i = 0; i < m_pHapticMixerList.size(); i++) {
		delete& m_pHapticMixerList[i];
	}

	m_pHapticMixerList.clear();
}

void At_HapticEngine::DestroyAllMixer() {
	
	// CRASH !!
	//m_pHapticMixerList.clear();
	
	m_mixerIncrementalUniqueID = 0;
}
void At_HapticEngine::CreateMixer(int* mixerId, int sampleRate, int bufferLength, int outChannelCount) {


	At_HapticMixer* hm = new At_HapticMixer();	
	hm->InitializeOutput(m_mixerIncrementalUniqueID, sampleRate, bufferLength, outChannelCount);
	m_pHapticMixerList.push_back(*hm);
	*mixerId = m_mixerIncrementalUniqueID;

	m_mixerIncrementalUniqueID++;
}

void At_HapticEngine::DestroyMixer(int mixerId) {

	for (int i = 0; i < m_pHapticMixerList.size(); i++) {
		if (m_pHapticMixerList[i].m_id == mixerId) {
			m_pHapticMixerList.erase(m_pHapticMixerList.begin() + i);
			break;
		}
	}

}
/*****************************************************************/


void At_HapticEngine::AddSource(int mixerId, int* sourceId) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->AddSource(sourceId);
	}
}

void At_HapticEngine::RemoveSource(int mixerId, int sourceId) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->RemoveSource(sourceId);
	}
}

void At_HapticEngine::RemoveAllSources(int mixerId) {

	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->RemoveAllSources();
	}
}



float At_HapticEngine::GetMixingBufferSampleForChannelAndZero(int mixerId, int indexSample, int indexChannel) {
	float value;
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		value = hm->GetMixingBufferSampleForChannelAndZero(indexSample, indexChannel);
	}
	return value;
	
}

void At_HapticEngine::SetListenerPosition(int mixerId, float* position) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetListenerPosition(position);
	}

}
/*****************************************************************/

void At_HapticEngine::Process(int mixerId, int sourceId, float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume) {

	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->Process(sourceId, inBuffer, bufferLength, offset, inChannelCount, volume);
	}
}

void At_HapticEngine::SetSourcePosition(int mixerId, int sourceId, float* position) {

	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetSourcePosition(sourceId, position);
	}
}



void At_HapticEngine::SetSourceAttenuation(int mixerId, int sourceId, float attenuation) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetSourceAttenuation(sourceId, attenuation);
	}
}

void At_HapticEngine::SetSourceMinDistance(int mixerId, int sourceId, float minDistance) {
	At_HapticMixer* hm = findMixerWithID(sourceId);
	if (hm != NULL) {
		hm->SetSourceMinDistance(sourceId, minDistance);
	}
}

void At_HapticEngine::SetSourceLowPassFc(int mixerId, int sourceId, double fc) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetSourceLowPassFc(sourceId, fc);
	}
}
void At_HapticEngine::SetSourceHighPassFc(int mixerId, int sourceId, double fc) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetSourceHighPassFc(sourceId, fc);
	}
}
void At_HapticEngine::SetSourceLowPassGain(int mixerId, int sourceId, double gain) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetSourceLowPassGain(sourceId, gain);
	}
}
void At_HapticEngine::SetSourceHighPassGain(int mixerId, int sourceId, double gain) {
	At_HapticMixer* hm = findMixerWithID(mixerId);
	if (hm != NULL) {
		hm->SetSourceHighPassGain(sourceId, gain);
	}
}


At_HapticMixer* At_HapticEngine::findMixerWithID(int mixerId) {

	At_HapticMixer* hm;

	for (int i = 0; i < m_pHapticMixerList.size(); i++) {
		if (m_pHapticMixerList[i].m_id == mixerId) {
			hm = &m_pHapticMixerList[i];
			return hm;
		}
	}
	return NULL;
}