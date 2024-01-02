using System.Collections.Generic;
using System.Diagnostics;

namespace BeatDetector.AudioProcessing;

internal class BeatChunk
{
    public Stopwatch StopWatch { get; } = new Stopwatch();
    public List<double> EnergyHistory { get; } = new List<double>();
    public double AverageDifference { get; set; }

    public int NoBeatCount { get; set; }
}