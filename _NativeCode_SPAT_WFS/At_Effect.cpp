#include "At_Effect.h"


void At_Effect::AT_process_varySpeed(float speed,float previousSpeed, float* inBuffer, float* outBuffer, int bufferLength, int channelCount) {

	// size of inBuffer is (size of outbuffer * pitch)
	for (int sampleIndex = 0; sampleIndex < bufferLength; sampleIndex++)
		for (int channelIndex = 0; channelIndex < channelCount; channelIndex++) {
		{
			float fadeOut = ((float)bufferLength - (float)sampleIndex) / (float)bufferLength;
			float prevSample = inBuffer[channelCount * (int)(sampleIndex * previousSpeed) + channelIndex];
			float currSample = inBuffer[channelCount * (int)(sampleIndex * speed) + channelIndex];
			outBuffer[channelCount * sampleIndex + channelIndex] = fadeOut * prevSample + (1 - fadeOut) * currSample;
		}
	}
}