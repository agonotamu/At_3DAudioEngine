

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
		//delete& m_pWfsSpatializerList;
		incrementalUniqueID = 0;
	}

	void At_SpatializationEngine::CreateWfsSpatializer(int* id, bool is3D, bool isDirective) { //modif mathias 06-17-2021

		At_WfsSpatializer *s = new At_WfsSpatializer();	
		s->m_is3D = is3D; //modif mathias 06-17-2021
		s->m_isDirective = isDirective; //modif mathias 06-17-2021
		//s->spatID = m_pWfsSpatializerList.size() - 1;
		s->spatID = incrementalUniqueID;
		s->initDelayBuffer();
		m_pWfsSpatializerList.push_back(*s);
		*id = incrementalUniqueID;

		incrementalUniqueID++;



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
		std::cout << "m_pWfsSpatializerList is size : " << m_pWfsSpatializerList.size() << "\n";
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
	// One for each Spatializer ---------------------------------------------------------------------------------------
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


	void At_SpatializationEngine::WFS_process(int id, float* inBuffer, float* outBuffer, int bufferLength, int inChannelCount, int outChannelCount) {
		
		At_WfsSpatializer* ws = findSpatializerWithSpatID(id);
		if (ws != NULL) {
			ws->process(inBuffer, outBuffer, bufferLength, inChannelCount, outChannelCount);
		}
		/*
		if (id < m_pWfsSpatializerList.size()) {
			m_pWfsSpatializerList[id].process(inBuffer, outBuffer, bufferLength, inChannelCount, outChannelCount);
		}
		*/
	}
	
}