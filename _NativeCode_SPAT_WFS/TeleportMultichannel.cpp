#include "TeleportMultichannel.h"


#include "TeleportLib.cpp"
#include <iostream>

namespace Spatializer
{
  
    int TeleportMultichannel::FeedTeleportBuffer(float* buffer, int indexStream, int length) {

        Teleport::SharedMemoryHandle& shared = Teleport::GetSharedMemory();
        Teleport::Stream& stream = shared->streams[indexStream];
        
        for (unsigned int n = 0; n < length; n++)
        {
            stream.Feed(buffer[n]);
        }

        
        return 0;
    }

    int TeleportMultichannel:: ReadTeleportBuffer(float* buffer, int indexStream, int length) {

        Teleport::SharedMemoryHandle& shared = Teleport::GetSharedMemory();
        Teleport::Stream& stream = shared->streams[indexStream];

        for (unsigned int n = 0; n < length; n++)
        {
            float x = 0.0f;
            stream.Read(x);
            buffer[n] = x;
        }

        return 0;
    }
}
// ---