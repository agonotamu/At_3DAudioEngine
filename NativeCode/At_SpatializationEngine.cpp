

#include "At_SpatializationEngine.h"
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
		std::cout << "Clear all Spatializer !\n";
#endif
		for (int i = 0; i < m_pWfsSpatializerList.size(); i++) {
			delete &m_pWfsSpatializerList[i];
		}
		m_pWfsSpatializerList.clear(); 
	}
	 
	void At_SpatializationEngine::WFS_destroyAllSpatializer() {
#ifdef DEBUGLOG
		std::cout << "Clear all Spatializer !\n";
#endif
		m_pWfsSpatializerList.clear();
	}

	void At_SpatializationEngine::CreateWfsSpatializer(int* id, bool is3D, bool isDirective) { //modif mathias 06-17-2021

		At_WfsSpatializer *s = new At_WfsSpatializer();	
		s->m_is3D = is3D; //modif mathias 06-17-2021
		s->m_isDirective = isDirective; //modif mathias 06-17-2021
		m_pWfsSpatializerList.push_back(*s);
		*id = m_pWfsSpatializerList.size() - 1;



#ifdef DEBUGLOG
		std::cout << "adding spatializer with index "<< (m_pWfsSpatializerList.size() - 1) <<"\n";
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

	// One for each Spatializer ---------------------------------------------------------------------------------------
	void At_SpatializationEngine::WFS_setSourcePosition(int id, float* position, float* rotation, float* forward) { //modif mathias 06-14-2021
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setSourcePosition(position, rotation, forward); //modif mathias 06-14-2021
		}
	}

	void At_SpatializationEngine::WFS_setAttenuation(int id, float attenuation) {
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setSourceAttenuation(attenuation);
		}
	}
	
	void At_SpatializationEngine::WFS_setSourceOmniBalance(int id, float omniBalance) {
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setSourceOmniBalance(omniBalance);
		}
	}

	void At_SpatializationEngine::WFS_setTimeReversal(int id, float timeReversal) {
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setTimeReversal(timeReversal);
		}
	}	
	void At_SpatializationEngine::WFS_setMinDistance(int id, float minDistance) {
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].setMinDistance(minDistance);
		}
	}


	void At_SpatializationEngine::WFS_process(int id, float* inBuffer, float* outBuffer, int bufferLength, int inChannelCount, int outChannelCount) {
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].process(inBuffer, outBuffer, bufferLength, inChannelCount, outChannelCount);
		}
	}
	
}