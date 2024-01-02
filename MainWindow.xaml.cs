using BeatDetector.AudioProcessing;

using NAudio.Wave;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace BeatDetector;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Brush[] brushes = new Brush[]
    {
        Brushes.Red,
        Brushes.Orange,
        Brushes.Plum,
        Brushes.Purple,
        Brushes.Blue,
        Brushes.Green,
    };

    private readonly IWaveIn audioStream;
    private int lightAmountFactor = 3;
    private AudioBeatDetector? audioBeatDetector;
    public ObservableCollection<BeatLight> BeatLights { get; } = new ObservableCollection<BeatLight>();

    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();

        audioStream = new WasapiLoopbackCapture();
        audioStream.StartRecording();

        SetupAudioBeatDetector();
    }

    private void AudioBeatDetector_OnBeat(object? sender, BeatDetectorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            double index = (brushes.Length - 1) / (double)BeatLights.Count * BeatLights.Count(x => x.Color != Brushes.White);
            BeatLights[e.ChunkIndex].Color = brushes[(int)index];
        });
    }

    private void AudioBeatDetector_OnNoBeat(object? sender, BeatDetectorEventArgs e)
    {
        _ = Dispatcher.Invoke(() => BeatLights[e.ChunkIndex].Color = Brushes.White);
    }

    private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Add && lightAmountFactor < 10)
        {
            lightAmountFactor++;
            audioBeatDetector?.Dispose();
            SetupAudioBeatDetector();
        }
        else if (e.Key == System.Windows.Input.Key.Subtract && lightAmountFactor > 1)
        {
            lightAmountFactor--;
            audioBeatDetector?.Dispose();
            SetupAudioBeatDetector();
        }
    }

    private void SetupAudioBeatDetector()
    {
        BeatLights.Clear();

        for (int i = 0; i < Math.Pow(lightAmountFactor, 2); i++)
        {
            BeatLights.Add(new BeatLight());
        }

        if (audioBeatDetector is not null)
        {
            audioBeatDetector.OnBeat -= AudioBeatDetector_OnBeat;
            audioBeatDetector.OnNoBeat -= AudioBeatDetector_OnNoBeat;
        }

        audioBeatDetector = new AudioBeatDetector(audioStream, TimeSpan.FromMilliseconds(0), 1024 * 2, BeatLights.Count);

        audioBeatDetector.OnBeat += AudioBeatDetector_OnBeat;
        audioBeatDetector.OnNoBeat += AudioBeatDetector_OnNoBeat;

        Width = (Math.Sqrt(BeatLights.Count) * 110) + 15;
        Height = (Math.Sqrt(BeatLights.Count) * 110) + 40;
    }
}

public class BeatLight : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private Brush color = Brushes.White;

    public Brush Color
    {
        get => color;
        set
        {
            if (value != color)
            {
                color = value;
                NotifyPropertyChanged();
            }
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}