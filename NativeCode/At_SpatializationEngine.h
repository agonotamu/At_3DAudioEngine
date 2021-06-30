#pragma once

#include "AudioPluginUtil.h"
#include "At_WfsSpatializer.h"
#include <vector>
#include <iostream>
using namespace std;

#define DEBUGLOG

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
		void CreateWfsSpatializer(int* id, bool is3D, bool isDirective); //modif mathias 06-17-2021

		// One for all Spatializer ----------------------------------------------------------------------------------------
		void WFS_setSampleRate(float sampleRatte);
		void WFS_setListenerPosition(float* position, float* rotation);
		void WFS_setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards);
		void WFS_destroyAllSpatializer();

		// One for each Spatializer ---------------------------------------------------------------------------------------
		void WFS_setSourcePosition(int id, float* position, float* rotation, float* forward); //modif mathias 06-14-2021
		void WFS_setAttenuation(int id, float attenuation);
		void WFS_setSourceOmniBalance(int id, float omniBalance);
		void WFS_setTimeReversal(int id, float timeReversal);
		void WFS_process(int id, float* inBuffer, float* outBuffer, int bufferLength, int inChannelCount, int outChannelCount);

	};
}