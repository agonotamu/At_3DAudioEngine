using System;
using NAudio.Wave;
using NAudio.Wave.Asio;

namespace NAudioAsioPatchBay
{
    public class AsioInputPatcher : ISampleProvider
    {
        private readonly int outputChannels;
        private readonly int inputChannels;
       
        public AsioInputPatcher(int sampleRate, int inputChannels, int outputChannels)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outputChannels);
            this.outputChannels = outputChannels;
            this.inputChannels = inputChannels;            
        }

        // immediately after SetInputSamples, we are now asked for all the audio we want
        // to write to the soundcard
        public int Read(float[] buffer, int offset, int count)
        {
            // WARNING GONOT : don't know why I'm entering here !!! Cause an error -> Comment the instruction....
            //throw new InvalidOperationException("Should not be called");
            return 0;
        }

        public WaveFormat WaveFormat { get; }
    }
}