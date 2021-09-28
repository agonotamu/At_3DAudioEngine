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


        // float[] inBuffers, IntPtr[] outBuffers, int sampleCount, AsioSampleType sampleType
        public void ClearOutbuffer()
        {
            
        }

        public void ProcessBuffer(float[] inBuffers, IntPtr[] outBuffers, int sampleCount, AsioSampleType sampleType, int masterChannel, int maxDeviceChannel)
        {
            Action<IntPtr, int, float> setOutputSample;
            if (sampleType == AsioSampleType.Int32LSB)
                setOutputSample = SetOutputSampleInt32LSB;
            else if (sampleType == AsioSampleType.Int16LSB)
                setOutputSample = SetOutputSampleInt16LSB;
            else if (sampleType == AsioSampleType.Int24LSB)
                throw new InvalidOperationException("Not supported");
            else if (sampleType == AsioSampleType.Float32LSB)
                setOutputSample = SetOutputSampleFloat32LSB;
            else
                throw new ArgumentException(@"Unsupported ASIO sample type {sampleType}");



            if (masterChannel < maxDeviceChannel)
            for (int n = 0; n < sampleCount; n++)
            {
                if(!float.IsNaN(inBuffers[n]))
                    setOutputSample(outBuffers[masterChannel], n, inBuffers[n]);
            }               

        }

        private unsafe void SetOutputSampleInt32LSB(IntPtr buffer, int n, float value)
        {
            *((int*)buffer + n) = (int)(value * int.MaxValue);
        }

        private unsafe void SetOutputSampleInt16LSB(IntPtr buffer, int n, float value)
        {
            *((short*)buffer + n) = (short)(value * short.MaxValue);
        }

        private unsafe void SetOutputSampleFloat32LSB(IntPtr buffer, int n, float value)
        {
            *((float*) buffer + n) = value;
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