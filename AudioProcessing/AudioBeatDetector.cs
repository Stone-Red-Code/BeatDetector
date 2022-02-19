using NAudio.Dsp;
using NAudio.Wave;

using System;
using System.Linq;

namespace BeatDetector.AudioProcessing;

internal class AudioBeatDetector
{
    public event EventHandler<BeatDetectorEventArgs>? OnBeat;

    public event EventHandler<BeatDetectorEventArgs>? OnNoBeat;

    private readonly IWaveIn waveIn;
    private readonly SampleAggregator sampleAggregator;
    private readonly TimeSpan beatDetectionDelay;
    private readonly int fftLength;

    private readonly int chunkCount;
    private readonly BeatChunk[] beatChunks;

    public AudioBeatDetector(TimeSpan beatDetectionDelay, int fftLength, int chunkCount)
    {
        this.beatDetectionDelay = beatDetectionDelay;
        this.chunkCount = chunkCount;
        this.fftLength = fftLength;

        beatChunks = new BeatChunk[chunkCount];
        sampleAggregator = new SampleAggregator(fftLength);

        for (int i = 0; i < chunkCount; i++)
        {
            beatChunks[i] = new BeatChunk();
            beatChunks[i].StopWatch.Start();
        }

        sampleAggregator.FftCalculated += new EventHandler<FftEventArgs>(FftCalculated);
        sampleAggregator.PerformFFT = true;

        waveIn = new WasapiLoopbackCapture();
        waveIn.DataAvailable += OnDataAvailable;

        waveIn.StartRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        byte[] buffer = e.Buffer;
        int bytesRecorded = e.BytesRecorded;
        int bufferIncrement = waveIn.WaveFormat.BlockAlign;

        for (int index = 0; index < bytesRecorded; index += bufferIncrement)
        {
            float sample32 = BitConverter.ToSingle(buffer, index);
            sampleAggregator.Add(sample32);
        }
    }

    private void FftCalculated(object? sender, FftEventArgs e)
    {
        Complex[][] chunks = e.Result.Chunk(fftLength / chunkCount).ToArray();

        for (int i = 0; i < chunkCount; i++)
        {
            CalculateBeat(chunks[i], beatChunks[i], i);
        }
    }

    private void CalculateBeat(Complex[] values, BeatChunk beatChunk, int chunkIndex)
    {
        double energyLevel = 0;
        foreach (Complex value in values)
        {
            energyLevel += Math.Pow(Math.Abs(value.Y), 2);
        }

        double averageEnergyLevel = beatChunk.EnergyHistory.Count > 0 ? beatChunk.EnergyHistory.Average() : double.MaxValue;

        if (beatChunk.StopWatch.Elapsed >= beatDetectionDelay)
        {
            double difference = energyLevel - averageEnergyLevel;

            if (difference > beatChunk.AverageDifference / 2)
            {
                beatChunk.AverageDifference = (difference + beatChunk.AverageDifference) / 2d;
                OnBeat?.Invoke(this, new BeatDetectorEventArgs(chunkIndex, difference));
            }
            else
            {
                OnNoBeat?.Invoke(this, new BeatDetectorEventArgs(chunkIndex, 0));
            }
            beatChunk.StopWatch.Restart();
        }

        beatChunk.EnergyHistory.Add(energyLevel);
        if (beatChunk.EnergyHistory.Count > 43)
        {
            beatChunk.EnergyHistory.RemoveAt(0);
        }
    }
}

public class BeatDetectorEventArgs : EventArgs
{
    public int ChunkIndex { get; }
    public double DetectedValue { get; }

    public BeatDetectorEventArgs(int chunkIndex, double detectedValue)
    {
        ChunkIndex = chunkIndex;
        DetectedValue = detectedValue;
    }
}