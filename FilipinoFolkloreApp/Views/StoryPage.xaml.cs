using FilipinoFolkloreApp.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FilipinoFolkloreApp.Views;

public partial class StoryPage : ContentPage
{
    bool _isNavigatingToQuiz = false;

    private readonly string _storyId;
    int _idx = 0;
    bool _playing = true;
    CancellationTokenSource? _hudCts;
    CancellationTokenSource? _cts; 
    readonly IAudioManager _audioManager = AudioManager.Current;
    private Stream? _audioStream;
    private IAudioPlayer? _player;

    public StoryPage(string storyId)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _storyId = storyId;
        //bool isCompleted = Preferences.Get(
        //$"StoryCompleted_{_storyId}", false);

        //PrevButton.IsVisible = isCompleted;
        //NextButton.IsVisible = isCompleted;
        PrevButton.IsVisible = true;
        NextButton.IsVisible = true;
        NarratorAvatar.Source = AlamatContent.CurrentNarrator.Avatar;
        if (Application.Current is App app)
        {
            app.PauseBackgroundMusic();
        }
        _ = ShowSlideAsync(initial: true);
    }

    async Task ShowSlideAsync(bool initial = false)
    {
        var story = AlamatContent.GetStory(_storyId);

        if (_idx >= story.Slides.Count)
        {
            
            if (_isNavigatingToQuiz)
                return;
            Preferences.Set($"StoryCompleted_{_storyId}", true);
            _isNavigatingToQuiz = true;
            _cts?.Cancel();
            _player?.Stop();
            _player?.Dispose();
            _player = null;

            _audioStream?.Dispose();
            _audioStream = null;

            await Navigation.PushAsync(new QuizPage(_storyId));
            return;
        }
        if (_idx < 0) _idx = 0;

        var slide = story.Slides[_idx];

        SubtitleBar.CancelAnimations();
        await SubtitleBar.FadeTo(0, 120);

        var front = BgA.Opacity >= BgB.Opacity ? BgA : BgB;
        var back = front == BgA ? BgB : BgA;
        back.Source = slide.Background;
        back.Opacity = initial ? 1 : 0;
        if (!initial)
            await Task.WhenAll(front.FadeTo(0, 250), back.FadeTo(1, 250));

        CharacterLayer.Children.Clear();
        foreach (var img in slide.Characters)
        {
            CharacterLayer.Children.Add(new Image
            {
                Source = img,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                HeightRequest = 240
            });
        }

        if (!string.IsNullOrWhiteSpace(slide.Subtitle))
        {
            SubtitleText.Text = slide.Subtitle!;
        }
        else
        {
            SubtitleText.Text = "";
            SubtitleBar.Opacity = 0;
        }

        StartForSlide(slide);
    }

    async void StartForSlide(AlamatContent.Slide slide)
    {
        StopKaraoke();
        _cts?.Cancel();

        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _audioStream?.Dispose();
        _audioStream = null;

        var narratorId = AlamatContent.SelectedNarratorId;

        if (slide.AudioByNarrator != null &&
            slide.AudioByNarrator.TryGetValue(narratorId, out var relPath) &&
            !string.IsNullOrWhiteSpace(relPath))
        {
            var cts = new CancellationTokenSource();
            _cts = cts;
            var ct = cts.Token;

            try
            {
                // relPath is available here.
                await LoadKaraokeAsync(relPath, slide.Subtitle ?? "");

                if (ct.IsCancellationRequested) return;

                var stream = await FileSystem.OpenAppPackageFileAsync(relPath);

                if (ct.IsCancellationRequested)
                {
                    stream.Dispose();
                    return;
                }

                _audioStream = stream;

                var player = AudioManager.Current.CreatePlayer(stream);
                _player = player;
                player.Volume = AlamatContent.NarratorVolume;

                player.PlaybackEnded += async (s, e) =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.5), ct);

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (ct.IsCancellationRequested ||
                                !ReferenceEquals(_player, player) ||
                                _isNavigatingToQuiz)
                            {
                                return;
                            }

                            StopKaraoke();
                            _idx++;
                            await ShowSlideAsync();
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        // The slide changed or playback was cancelled.
                    }
                };

                StartKaraoke(player);

                if (_playing)
                {
                    player.Play();
                    ShowSubtitleIfAvailable();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                if (!ct.IsCancellationRequested &&
                    ReferenceEquals(_cts, cts))
                {
                    StopKaraoke();
                    StopAudio();
                    StartAutoTimerIfPlaying();
                }
            }
        }
        else
        {
            StartAutoTimerIfPlaying();
        }
    }

    void ShowSubtitleIfAvailable()
    {
        bool hasSubtitle =
            !string.IsNullOrWhiteSpace(SubtitleText.Text) ||
            (SubtitleText.FormattedText?.Spans.Count ?? 0) > 0;

        if (hasSubtitle)
        {
            SubtitleBar.CancelAnimations();
            _ = SubtitleBar.FadeTo(1, 150);
        }
    }

    void OnScreenTapped(object? s, TappedEventArgs e)
    {
        _ = ShowControlsTemporarilyAsync();
    }

    async Task ShowControlsTemporarilyAsync()
    {
        _hudCts?.Cancel();
        _hudCts = new CancellationTokenSource();
        var ct = _hudCts.Token;

        ControlsPanel.InputTransparent = false;
        await ControlsPanel.FadeTo(1, 150);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            if (ct.IsCancellationRequested) return;

            await ControlsPanel.FadeTo(0, 150);
            ControlsPanel.InputTransparent = true;
        }
        catch (TaskCanceledException) { /* ignore */ }
    }

    protected override void OnDisappearing()
    {
        _isNavigatingToQuiz = true;
        base.OnDisappearing();

        _cts?.Cancel();

        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _audioStream?.Dispose();
        _audioStream = null;
        if (Application.Current is App app)
        {
            app.UpdateBackgroundMusic(AlamatContent.MusicIsEnabled);
        }
    }

    void StartAutoTimerIfPlaying()
    {
        if (!_playing) return;

        ShowSubtitleIfAvailable();

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = AutoAdvanceAsync(_cts.Token);
    }

    void StopAudio()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _audioStream?.Dispose();
        _audioStream = null;
    }

    async Task AutoAdvanceAsync(CancellationToken ct)
    {
        try
        {
            // 2. Long delay for reading when NO AUDIO is playing
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            if (ct.IsCancellationRequested) return;

            _idx++;
            await ShowSlideAsync();
        }
        catch (TaskCanceledException) { }
    }

    async void OnPrev(object? s, TappedEventArgs e)
    {
        _cts?.Cancel();
        StopAudio();
        _idx--;
        await ShowSlideAsync();
    }

    async void OnNext(object? s, TappedEventArgs e)
    {
        if (_isNavigatingToQuiz)
            return;
        _ = ShowControlsTemporarilyAsync();
        _cts?.Cancel();
        StopAudio();
        _idx++;
        await ShowSlideAsync();
    }

    void OnTogglePlay(object? s, TappedEventArgs e)
    {
        _ = ShowControlsTemporarilyAsync();
        _playing = !_playing;
        PlayIcon.Source = _playing ? "elements/pause.png" : "elements/play.png";

        if (_playing)
        {
            if (_player != null)
            {
                _player.Play();
                ShowSubtitleIfAvailable();
            }
            else 
            {
                StartAutoTimerIfPlaying();
            }
        }
        else
        {
            _player?.Pause();

            // Cancel only the fallback timer when there is no audio.
            if (_player == null)
                _cts?.Cancel();
        }
    }
    public sealed class WordTiming
    {
        public string Text { get; set; } = "";
        public double Start { get; set; }
        public double End { get; set; }
    }

    private readonly List<Span> _wordSpans = new();
    private List<WordTiming> _wordTimings = new();
    private Microsoft.Maui.Dispatching.IDispatcherTimer? _karaokeTimer;
    private int _highlightedWord = -1;

    private async Task LoadKaraokeAsync(
        string audioPath,
        string subtitle)
    {
        StopKaraoke();

        _wordSpans.Clear();
        _wordTimings.Clear();
        SubtitleText.FormattedText = null;
        SubtitleText.Text = subtitle;

        // Example: audio/story1.mp3 → audio/story1.words.json
        string timingPath = System.IO.Path.ChangeExtension(
            audioPath, ".words.json");

        try
        {
            using var stream =
                await FileSystem.OpenAppPackageFileAsync(timingPath);

            var timings =
                await System.Text.Json.JsonSerializer
                    .DeserializeAsync<List<WordTiming>>(
                        stream,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            if (timings == null || timings.Count == 0)
                return;
            // Normalize newline markers in timestamp text.
            foreach (var timing in timings)
            {
                timing.Text = timing.Text
                    .Replace("\\n", " ")
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();
            }
            // Keep the original subtitle if timestamps do not match its words.
            var matches = System.Text.RegularExpressions.Regex.Matches(
                subtitle, @"\S+");

            if (matches.Count != timings.Count)
                return;

            for (int i = 0; i < matches.Count; i++)
            {
                var word = timings[i];

                if (word.Text != matches[i].Value ||
                    !double.IsFinite(word.Start) ||
                    !double.IsFinite(word.End) ||
                    word.Start < 0 ||
                    word.End <= word.Start ||
                    (i > 0 && word.Start < timings[i - 1].End))
                    return;
            }

            _wordTimings = timings;

            // Preserve punctuation and whitespace from the subtitle.
            var formatted = new FormattedString();
            int position = 0;

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Index > position)
                {
                    formatted.Spans.Add(new Span
                    {
                        Text = subtitle.Substring(
                            position, match.Index - position),
                        TextColor = Colors.White
                    });
                }

                var span = new Span
                {
                    Text = match.Value,
                    TextColor = Colors.White
                };

                formatted.Spans.Add(span);
                _wordSpans.Add(span);
                position = match.Index + match.Length;
            }

            if (position < subtitle.Length)
            {
                formatted.Spans.Add(new Span
                {
                    Text = subtitle.Substring(position),
                    TextColor = Colors.White
                });
            }

            SubtitleText.Text = null;
            SubtitleText.FormattedText = formatted;
        }
        catch (System.IO.FileNotFoundException)
        {
            // This recording has no timing file yet.
        }
        catch (System.Text.Json.JsonException)
        {
            // Keep displaying the ordinary subtitle.
        }
    }

    private void StartKaraoke(IAudioPlayer player)
    {
        StopKaraoke();

        if (_wordTimings.Count == 0)
            return;

        _karaokeTimer = Dispatcher.CreateTimer();
        _karaokeTimer.Interval = TimeSpan.FromMilliseconds(40);

        _karaokeTimer.Tick += (_, _) =>
        {
            if (!ReferenceEquals(_player, player))
            {
                StopKaraoke();
                return;
            }

            // Read the audio position, so pausing does not cause drift.
            double seconds = player.CurrentPosition;

            int active = _wordTimings.FindIndex(
                w => seconds >= w.Start && seconds < w.End);

            if (active == _highlightedWord)
                return;

            if (_highlightedWord >= 0)
                _wordSpans[_highlightedWord].TextColor = Colors.White;

            if (active >= 0)
                _wordSpans[active].TextColor = Colors.Yellow;

            _highlightedWord = active;
        };

        _karaokeTimer.Start();
    }

    private void StopKaraoke()
    {
        _karaokeTimer?.Stop();
        _karaokeTimer = null;

        if (_highlightedWord >= 0 &&
            _highlightedWord < _wordSpans.Count)
        {
            _wordSpans[_highlightedWord].TextColor = Colors.White;
        }

        _highlightedWord = -1;
    }
}
