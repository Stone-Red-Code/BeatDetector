using NAudio.Dsp;

using System;

namespace BeatDetector.AudioProcessing;

internal class SampleAggregator
{
    // FFT
    public event EventHandler<FftEventArgs>? FftCalculated;

    public bool PerformFFT { get; set; }
    private readonly Complex[] fftBuffer;
    private readonly FftEventArgs fftArgs;
    private int fftPos;
    private readonly int fftLength;
    private readonly int m;

    public SampleAggregator(int fftLength)
    {
        if (!IsPowerOfTwo(fftLength))
        {
            throw new ArgumentException("FFT Length must be a power of two");
        }
        m = (int)Math.Log(fftLength, 2.0);
        this.fftLength = fftLength;
        fftBuffer = new Complex[fftLength];
        fftArgs = new FftEventArgs(fftBuffer);
    }

    private bool IsPowerOfTwo(int x)
    {
        return (x & (x - 1)) == 0;
    }

    public void Add(float value)
    {
        if (PerformFFT && FftCalculated is not null)
        {
            fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
            fftBuffer[fftPos].Y = 0; // This is always zero with audio.
            fftPos++;
            if (fftPos >= fftLength)
            {
                fftPos = 0;
                FastFourierTransform.FFT(true, m, fftBuffer);
                FftCalculated(this, fftArgs);
            }
        }
    }
}

public class FftEventArgs : EventArgs
{
    public FftEventArgs(Complex[] result)
    {
        Result = result;
    }

    public Complex[] Result { get; private set; }
}