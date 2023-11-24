

#pragma once

#include "AudioPluginUtil.h"
#include <vector>
#include <iostream>
#include "At_HapticSource.h"

using namespace std;


class At_HapticMixer
{
	
public:

	At_HapticMixer();
	~At_HapticMixer();

	vector<At_HapticSource> m_pHapticSourceList;

	bool AddSource(int* id);
	void RemoveSource(int id);
	void RemoveAllSources();

	void InitializeOutput(int m_mixerId, int sampleRate, int bufferLength, int outChannelCount);

	
	void Process(int sourceId, float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume);	

	float GetMixingBufferSampleForChannelAndZero(int indexSample, int indexChannel);

	void SetSourcePosition(int sourceId, float* position);
	void SetListenerPosition(float *position);

	void SetSourceAttenuation(int sourceId, float attenuation);
	void SetSourceMinDistance(int sourceId, float minDistance);

private:
	At_HapticSource* findSourceWithID(int id);

public:
	int m_id = 0;

	int m_sourceIncrementalUniqueID = 0;
	
	float* m_pMixingBuffer;

	int m_bufferLength;
	int m_outChannelCount;

	float m_sampleRate;
};




