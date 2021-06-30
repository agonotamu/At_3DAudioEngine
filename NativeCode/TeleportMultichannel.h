
#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64)
#define UNITY_WIN 1
#elif defined(__MACH__) || defined(__APPLE__)
#define UNITY_OSX 1
#elif defined(__ANDROID__)
#define UNITY_ANDROID 1
#elif defined(__linux__)
#define UNITY_LINUX 1
#endif

#include <stdio.h>
#include <memory> //for std::unique_ptr






#if UNITY_OSX | UNITY_LINUX
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <errno.h>
#include <unistd.h>
#include <string.h>
#elif UNITY_WIN
#include <windows.h>
#endif



namespace Spatializer
{

    static class TeleportMultichannel
    {

    public:

        static int FeedTeleportBuffer(float* buffer, int indexStream, int length);
        static int ReadTeleportBuffer(float* buffer, int indexStream, int length);

    private:
       
    };

} // !namespace Spatializer