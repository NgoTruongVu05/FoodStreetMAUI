using FoodStreetGuide.ViewModels;

namespace FoodStreetGuide.Controls;

public partial class AudioPlayerControl : ContentView
{
    private readonly List<BoxView> _waveBars = new();
    private CancellationTokenSource? _animCts;
    private static readonly Random _rng = new();

    public AudioPlayerControl()
    {
        InitializeComponent();
        BuildWaveform();
        BindingContextChanged += OnBindingContextChanged;
    }

    // ── Waveform ──────────────────────────────────────────────────────────────
    private void BuildWaveform()
    {
        var grid = new Grid { HeightRequest = 28 };
        for (int i = 0; i < 32; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (int i = 0; i < 32; i++)
        {
            var bar = new BoxView
            {
                Color        = Color.FromArgb("#185FA5"),
                CornerRadius = 2,
                HeightRequest= 6,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(bar, i);
            grid.Add(bar);
            _waveBars.Add(bar);
        }

        // Replace placeholder with real waveform
        var parent = WaveformPlaceholder.Parent as Layout;
        if (parent is not null)
        {
            int idx = parent.IndexOf(WaveformPlaceholder);
            parent.RemoveAt(idx);
            parent.Insert(idx, grid);
        }
    }

    private void StartWaveAnimation()
    {
        _animCts?.Cancel();
        _animCts = new CancellationTokenSource();
        var token = _animCts.Token;

        Task.Run(async () =>
        {
            int frame = 0;
            while (!token.IsCancellationRequested)
            {
                int pivot = frame % _waveBars.Count;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    for (int i = 0; i < _waveBars.Count; i++)
                    {
                        bool active = Math.Abs(i - pivot) <= 2;
                        _waveBars[i].Color         = active
                            ? Color.FromArgb("#378ADD")
                            : Color.FromArgb("#185FA5");
                        _waveBars[i].HeightRequest = active
                            ? 8 + _rng.Next(12)
                            : 4 + _rng.Next(6);
                    }
                });
                frame++;
                await Task.Delay(110, token).ConfigureAwait(false);
            }
        }, token);
    }

    private void StopWaveAnimation()
    {
        _animCts?.Cancel();
        foreach (var bar in _waveBars)
        {
            bar.Color         = Color.FromArgb("#185FA5");
            bar.HeightRequest = 6;
        }
    }

    // ── ViewModel binding ─────────────────────────────────────────────────────
    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is AudioPlayerViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(AudioPlayerViewModel.IsPlaying))
                {
                    if (vm.IsPlaying) StartWaveAnimation();
                    else              StopWaveAnimation();
                }
            };
        }
    }
}
