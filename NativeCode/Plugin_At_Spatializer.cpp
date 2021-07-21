#include "AudioPluginUtil.h"
#include <iostream>

#if PLATFORM_OSX | PLATFORM_LINUX | PLATFORM_WIN

#include "At_WfsSpatializer.h"
#include "At_SpatializationEngine.h"

using namespace Spatializer; 


//One for each Spatializer
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_CreateWfsSpatializer(int* id, bool is3D, bool isDirective) //modif mathias 06-17-2021
{
    At_SpatializationEngine::getInstance().CreateWfsSpatializer(id, is3D, isDirective); //modif mathias 06-17-2021
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setSourcePosition(int id, float* position, float* rotation, float* forward) //modif mathias 06-14-2021
{
    At_SpatializationEngine::getInstance().WFS_setSourcePosition(id, position, rotation, forward); //modif mathias 06-14-2021
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setSourceAttenuation(int id, float attenuation)
{
    At_SpatializationEngine::getInstance().WFS_setAttenuation(id, attenuation);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setSourceOmniBalance(int id, float omniBalance)
{
    At_SpatializationEngine::getInstance().WFS_setSourceOmniBalance(id, omniBalance);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setTimeReversal(int id, float timeReversal)
{
    At_SpatializationEngine::getInstance().WFS_setTimeReversal(id, timeReversal);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setMinDistance(int id, float minDistance)
{
    At_SpatializationEngine::getInstance().WFS_setMinDistance(id, minDistance);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_process(int id, float* inBuffer, float* outBuffer, int bufferLength, int inChannelCount, int outChannelCount)
{
    At_SpatializationEngine::getInstance().WFS_process(id, inBuffer, outBuffer, bufferLength, inChannelCount, outChannelCount);
}

// One for all Spatializer
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setListenerPosition(float* position, float* rotation, float* forward)
{
    At_SpatializationEngine::getInstance().WFS_setListenerPosition(position, rotation);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_setVirtualMicPosition(int speakerCount, float virtualMicMinDistance, float* positions, float* rotations, float* forwards)
{
    At_SpatializationEngine::getInstance().WFS_setVirtualMicPosition(speakerCount, virtualMicMinDistance, positions, rotations, forwards);
}
extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_WFS_destroyAllSpatializer()
{
    At_SpatializationEngine::getInstance().WFS_destroyAllSpatializer();
}

extern "C" UNITY_AUDIODSP_EXPORT_API void AT_SPAT_setSampleRate(float sampleRate)
{
    Spatializer::At_SpatializationEngine::getInstance().WFS_setSampleRate(sampleRate);
}







#endif
