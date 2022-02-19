using BeatDetector.AudioProcessing;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public ObservableCollection<BeatLight> BeatLights { get; } = new ObservableCollection<BeatLight>();

    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();

        for (int i = 0; i < 4; i++)
        {
            BeatLights.Add(new BeatLight());
        }

        AudioBeatDetector audioBeatDetector = new AudioBeatDetector(new TimeSpan(0, 0, 0, 0, 100), 1024 * 2, BeatLights.Count);
        audioBeatDetector.OnBeat += AudioBeatDetector_OnBeat;
        audioBeatDetector.OnNoBeat += AudioBeatDetector_OnNoBeat;
    }

    private void AudioBeatDetector_OnBeat(object? sender, BeatDetectorEventArgs e)
    {
        Dispatcher.Invoke(() => BeatLights[e.ChunkIndex].Color = Brushes.Red);
    }

    private void AudioBeatDetector_OnNoBeat(object? sender, BeatDetectorEventArgs e)
    {
        Dispatcher.Invoke(() => BeatLights[e.ChunkIndex].Color = Brushes.White);
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