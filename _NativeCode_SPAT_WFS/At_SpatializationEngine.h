#pragma once

#include "AudioPluginUtil.h"
#include "At_WfsSpatializer.h"
#include "Biquad.h"
#include <vector>
#include <iostream>
using namespace std;

//#define DEBUGLOG
// 
//#define RING_BUFFER
//#define DIRECTIVE_PLAYER




namespace Spatializer
{


	class At_SpatializationEngine
	{

	At_SpatializationEngine();
	~At_SpatializationEngine();

	public:
		static At_SpatializationEngine& getInstance()
		{
			static At_SpatializationEngine instance; // Guaranteed to be destroyed.
			// Instantiated on first use.
			return instance;
		}

		vector<At_WfsSpatializer> m_pWfsSpatializerList;	
		
		// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
		bool CreateWfsSpatializer(int* id, bool is3D, bool isDirective, float maxDistanceForDelay); //modif mathias 06-17-2021
		void DestroyWfsSpatializer(int id);

		// One for all Spatializer ----------------------------------------------------------------------------------------
		// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
		void WFS_initializeOutput(int sampleRate, int bufferLength, int outChannelCount, int subwooferOutputChannelCount, float subwooferCutoff);
		void WFS_getDemultiplexMixingBuffer(float* demultiplexMixingBuffer, int indexChannel);
		void WFS_fillOutputWithOneChannelOfMixingChannel(float *outBufferOneChannel, int indexChannel, int maxOutputChannel);
		float WFS_getMixingBufferSampleForChannelAndZero(int indexSample, int indexChannel, bool isHighPassFiltered);
		float WFS_getLowPasMixingBufferForChannel(int indexSample, int indexChannel);

		void WFS_setSampleRate(float sampleRatte);
		void WFS_setListenerPosition(float* position, float* rotation);
		void WFS_setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards);
		void WFS_destroyAllSpatializer();
		void WFS_setSubwooferCutoff(float subwooferCutoff);
		// One for each Spatializer ---------------------------------------------------------------------------------------
		
		void WFS_setSourcePosition(int id, float* position, float* rotation, float* forward); //modif mathias 06-14-2021
		void WFS_setAttenuation(int id, float attenuation);
		void WFS_setSourceOmniBalance(int id, float omniBalance);
		void WFS_setTimeReversal(int id, float timeReversal);
		void WFS_setMinDistance(int id, float minDistance);

		void WFS_setLowPassFc(int id, float fc);
		void WFS_setHighPassFc(int id, float fc);
		void WFS_setLowPassGain(int id, float gain);
		void WFS_setHighPassGain(int id, float gain);

		void WFS_setSpeakerMask(int id, float* activationSpeakerVolume, int outChannelCount); // Modif Rougerie 29/06/2022
		

		void WFS_process(int id, float* inBuffer, float* outBuffer, int bufferLength, int offset, int inChannelCount, int outChannelCount);
		void WFS_getDelay(int id, float* delay, int arraySize);
		void WFS_getVolume(int id, float* volume, int arraySize);
		void WFS_cleanDelayBuffer(int id);



	private:
		At_WfsSpatializer *findSpatializerWithSpatID(int id);

	public:
		int incrementalUniqueID = 0;
		// Modif Gonot 28/03/2023 - [optim] Add Mixing Buffer
		float* m_pMixingBuffer;
		float* m_pTmpMixingBuffer_hp;
		float* m_pTmpMixingBufferSub_lp;

		int m_bufferLength;
		int m_outChannelCount;
		int m_subwooferOutputChannelCount;
		float m_subwooferCutoff;

		Biquad* m_pSubwooferLowpass;
		Biquad* m_pSubwooferHighpass;
		float m_sampleRate;
	};
}