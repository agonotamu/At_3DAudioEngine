#pragma once

#include <vector>
#include <iostream>
using namespace std;


static class At_Effect
{
public:
	static void AT_process_varySpeed(float speed, float previousSpeed, float* inBuffer, float* outBuffer, int bufferLength, int channelCount);
};

