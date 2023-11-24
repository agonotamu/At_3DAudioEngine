

#pragma once

#include "AudioPluginUtil.h"
#include <vector>
#include <iostream>
#include "At_HapticMixer.h"

using namespace std;


//#define DEBUGLOG


class At_HapticEngine
{
	

	At_HapticEngine();
	~At_HapticEngine();

public:
	static At_HapticEngine& getInstance()
	{
		static At_HapticEngine instance; // Guaranteed to be destroyed.
		// Instantiated on first use.
		return instance;
	}


	vector<At_HapticMixer> m_pHapticMixerList;

	void CreateMixer(int* mixerId, int sampleRate, int bufferLength, int outChannelCount);
	void DestroyAllMixer();		
	void DestroyMixer(int mixerId);


	void AddSource(int mixerId, int* sourceId);
	void RemoveSource(int mixerId, int sourceId);
	void RemoveAllSources(int mixerId);

	float GetMixingBufferSampleForChannelAndZero(int mixerId, int indexSample, int indexChannel);
	void SetListenerPosition(int mixerId, float* position);


	void Process(int mixerId, int sourceId, float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume);
	void SetSourcePosition(int mixerId, int sourceId, float* position);	
	void SetSourceAttenuation(int mixerId, int sourceId, float attenuation);
	void SetSourceMinDistance(int mixerId, int sourceId, float minDistance);

	int m_mixerIncrementalUniqueID = 0;

private:
	At_HapticMixer* findMixerWithID(int mixerId);

};




