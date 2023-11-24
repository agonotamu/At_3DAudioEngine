#include "AudioPluginUtil.h"
#include <iostream>

#if PLATFORM_OSX | PLATFORM_LINUX | PLATFORM_WIN

#include "At_HapticEngine.h"

/**********************************************************/
// For the Haptic Engine 
/**********************************************************/
extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_CREATE_MIXER(int* mixerId, int sampleRate, int bufferLength, int outChannelCount) {
    At_HapticEngine::getInstance().CreateMixer(mixerId, sampleRate, bufferLength, outChannelCount);
}

extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_DESTROY_MIXER(int mixerId) {
    At_HapticEngine::getInstance().DestroyMixer(mixerId);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_DESTROY_ALL_MIXER() {
    At_HapticEngine::getInstance().DestroyAllMixer();
}


/**********************************************************/
// For a Mixer of the Haptic Engine  
/**********************************************************/
extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_ADD_SOURCE_TO_MIXER(int mixerId, int* sourceId) {
    At_HapticEngine::getInstance().AddSource(mixerId, sourceId);
}

extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_REMOVE_SOURCE_FROM_MIXER(int mixerId, int sourceId) {
    At_HapticEngine::getInstance().RemoveSource(mixerId, sourceId);
}

extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_REMOVE_ALL_SOURCE(int mixerId) {
    At_HapticEngine::getInstance().RemoveAllSources(mixerId);
}


extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_GET_MIX_SAMPLE(int mixerId, int indexSample, int indexChannel) {
    At_HapticEngine::getInstance().GetMixingBufferSampleForChannelAndZero(mixerId, indexSample, indexChannel);
}

extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_SET_LISTENER_POSITION(int mixerId, float* position) {
    At_HapticEngine::getInstance().SetListenerPosition(mixerId, position);
}

/**********************************************************/
// For a player of a Mixer of the Haptic Engine  
/**********************************************************/

extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_PROCESS(int mixerId, int sourceId, float* inBuffer, int bufferLength, int offset, int inChannelCount, float volume) {
    At_HapticEngine::getInstance().Process(mixerId, sourceId, inBuffer, bufferLength, offset, inChannelCount, volume);
}

extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_SET_SOURCE_POSITION(int mixerId, int sourceId, float* position) {
    At_HapticEngine::getInstance().SetSourcePosition(mixerId, sourceId, position);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_SET_SOURCE_ATTENUATION(int mixerId, int sourceId, float attenuation) {
    At_HapticEngine::getInstance().SetSourceAttenuation(mixerId,sourceId,  attenuation);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void HAPTIC_ENGINE_SET_SOURCE_MIN_DISTANCE(int mixerId, int sourceId, float minDistance) {
    At_HapticEngine::getInstance().SetSourceMinDistance(mixerId, sourceId, minDistance);
}

#endif
