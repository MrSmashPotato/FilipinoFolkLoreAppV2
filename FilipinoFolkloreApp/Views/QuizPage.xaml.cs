using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using FilipinoFolkloreApp.Views.Home;
using FilipinoFolkloreApp.Services;
using Microsoft.Maui.Layouts;
using Plugin.Maui.Audio;
using System.IO;

namespace FilipinoFolkloreApp.Views;

public partial class QuizPage : ContentPage
{
    private readonly string _storyId;
    int _correctIndex = 0;
    int _quizIndex = 0;
    int _totalQuizStarsEarned = 0;

    CancellationTokenSource? _cts;
    private HeartService HeartService =>
    Application.Current!.Handler!.MauiContext!.Services.GetService<HeartService>()!;

    private SoundService SoundService =>
        Application.Current!.Handler!.MauiContext!.Services.GetService<SoundService>()!;

    private int _tutorialStep = 0;
    private const string TUTORIAL_COMPLETED_KEY = "QuizPageTutorialCompleted4";

    private IAudioPlayer? _tutorialAudioPlayer;
    private Stream? _tutorialAudioStream;

    private readonly TutorialStep[] _tutorialSteps = new[]
{
        new TutorialStep
        {
            Title = "Ito ang Quiz!",
            Message = "Basahin ang tanong sa itaas at piliin ang tamang sagot!",
            TargetElementName = "QuestionArea",
        },
        new TutorialStep
        {
            Title = "Pumili ng Sagot",
            Message = "Pindutin ang isa sa tatlong pagpipilian para sagutin ang tanong. Bilisan dahil limitado laman ang oras!",
            TargetElementName = "ChoicesArea",
            OffsetX = -150
        }
    };

    public QuizPage(string storyId)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        _storyId = storyId;
        
        // Dynamically assign audio paths based on their sequential order
        for (int i = 0; i < _tutorialSteps.Length; i++)
        {
            _tutorialSteps[i].AudioPath = $"tutorialaudio/quizpagetutorial/quizpagetutorial{i + 1}.m4a"; // Adjust path dynamically based on your assets
        }  

        LoadHud();

        var story = AlamatContent.GetStory(_storyId);
        if (story.Quiz == null || story.Quiz.Count == 0)
        {
            _ = HandleCorrectAsync();
            return;
        }


        LoadQuiz();

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AlamatContent.Hearts = HeartService.GetHearts();
        RefreshHearts();

        bool tutorialCompleted = Preferences.Get(TUTORIAL_COMPLETED_KEY, false);
        if (!tutorialCompleted)
        {
            await Task.Delay(800); await ShowTutorial();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTutorialAudio();
        _cts?.Cancel();
    }

    private async Task ShowTutorial()
    {
        _tutorialStep = 0;

        _cts?.Cancel();

        UpdateTutorialStep();
        TutorialOverlay.IsVisible = true;

        await Task.WhenAll(
            TarsierImage.FadeTo(1, 400, Easing.CubicOut),
            TarsierImage.ScaleTo(1, 400, Easing.BounceOut)
        );

        await Task.Delay(200);
        await Task.WhenAll(
            SpeechBubbleContainer.FadeTo(1, 300, Easing.CubicOut),
            SpeechBubbleContainer.ScaleTo(1, 300, Easing.BounceOut)
        );
    }
    
    private void OnTutorialPrevStep(object? sender, EventArgs e)
    {
        if (_tutorialStep > 0)
        {
            _tutorialStep--;
            UpdateTutorialStep();
        }
    }

    private async void OnTutorialNextStep(object? sender, EventArgs e)
    {
        _tutorialStep++;

        if (_tutorialStep >= _tutorialSteps.Length)
        {
            await CompleteTutorial();
            return;
        }

        UpdateTutorialStep();
    }

    private async void UpdateTutorialStep()
    {
        var step = _tutorialSteps[_tutorialStep];

        TutorialTitleLabel.Text = step.Title;
        TutorialMessageLabel.Text = step.Message;
        TutorialProgressLabel.Text = $"{_tutorialStep + 1}/{_tutorialSteps.Length}";
        
        PrevTutorialButton.IsVisible = _tutorialStep > 0;
        NextTutorialButton.Text = _tutorialStep == _tutorialSteps.Length - 1 ? "Tapusin" : "Susunod";

        await PlayTutorialAudio(step.AudioPath);

        if (!string.IsNullOrEmpty(step.TargetElementName))
        {
            await PositionArrowToElement(step.TargetElementName, step.OffsetX);
        }
        else
        {
            ArrowPointer.Opacity = 0;
            PositionSpeechBubble(true, 0);
        }

        HighlightTargetElement(step.TargetElementName);
    }
    
    private async Task PlayTutorialAudio(string? audioPath)
    {
        StopTutorialAudio();

        if (string.IsNullOrWhiteSpace(audioPath))
            return;

        try
        {
            // Pause background music before speaking
            if (Application.Current is App app)
            {
                app.PauseBackgroundMusic();
            }

            _tutorialAudioStream = await FileSystem.OpenAppPackageFileAsync(audioPath);
            _tutorialAudioPlayer = AudioManager.Current.CreatePlayer(_tutorialAudioStream);
            _tutorialAudioPlayer.Volume = AlamatContent.NarratorVolume;
            
            // Resume background music when the audio file finishes
            _tutorialAudioPlayer.PlaybackEnded += (sender, args) =>
            {
                if (Application.Current is App a)
                {
                    a.ResumeBackgroundMusic();
                }
            };
            
            _tutorialAudioPlayer.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing tutorial audio: {ex}");
            
            // Re-enable if it failed to play
            if (Application.Current is App a)
            {
                a.ResumeBackgroundMusic();
            }
        }
    }

    private void StopTutorialAudio()
    {
        _tutorialAudioPlayer?.Stop();
        _tutorialAudioPlayer?.Dispose();
        _tutorialAudioPlayer = null;

        _tutorialAudioStream?.Dispose();
        _tutorialAudioStream = null;
    }

    private async Task PositionArrowToElement(string elementName, double offsetX = 0)
    {
        VisualElement? targetElement = elementName switch
        {
            "QuestionArea" => QuestionArea,
            "ChoicesArea" => ChoicesArea,
            _ => null
        };

        if (targetElement == null)
        {
            ArrowPointer.Opacity = 0;
            return;
        }

        await Task.Delay(150);

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        double screenHeight = displayInfo.Height / displayInfo.Density;
        double safeZone = 60;

        Rect targetBounds = GetAbsolutePosition(targetElement);

        if (targetBounds == Rect.Zero) return;

        double arrowWidth = 50;
        double arrowHeight = 50;
        double padding = 10;

        double yAbove = targetBounds.Top - arrowHeight - padding;
        double yBelow = targetBounds.Bottom + padding;

        bool preferAbove = targetBounds.Center.Y > (screenHeight / 2);

        double finalArrowY;
        bool isArrowAbove;

        if (preferAbove)
        {
            if (yAbove >= safeZone)
            {
                finalArrowY = yAbove;
                isArrowAbove = true;
            }
            else
            {
                finalArrowY = yBelow;
                isArrowAbove = false;
            }
        }
        else
        {
            if (yBelow + arrowHeight <= screenHeight - safeZone)
            {
                finalArrowY = yBelow;
                isArrowAbove = false;
            }
            else
            {
                finalArrowY = yAbove;
                isArrowAbove = true;
            }
        }

        double arrowX = targetBounds.Center.X - (arrowWidth / 2);

        AbsoluteLayout.SetLayoutBounds(ArrowPointer, new Rect(arrowX, finalArrowY, arrowWidth, arrowHeight));
        AbsoluteLayout.SetLayoutFlags(ArrowPointer, AbsoluteLayoutFlags.None);

        ArrowPointer.Rotation = isArrowAbove ? 90 : -90;

        bool arrowIsAtTopHalf = finalArrowY < (screenHeight / 2);
        PositionSpeechBubble(!arrowIsAtTopHalf, offsetX);

        await AnimateArrowPointer();
    }

    private void PositionSpeechBubble(bool positionAtTop, double offsetX)
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        double screenHeight = displayInfo.Height / displayInfo.Density;

        double bubbleX = 160 + offsetX;
        double bubbleWidth = 350;
        double safePadding = 60;

        if (positionAtTop)
        {
            AbsoluteLayout.SetLayoutBounds(SpeechBubbleContainer, new Rect(bubbleX, safePadding, bubbleWidth, AbsoluteLayout.AutoSize));
        }
        else
        {
            AbsoluteLayout.SetLayoutBounds(SpeechBubbleContainer, new Rect(bubbleX, screenHeight - 200, bubbleWidth, AbsoluteLayout.AutoSize));
        }

        AbsoluteLayout.SetLayoutFlags(SpeechBubbleContainer, AbsoluteLayoutFlags.None);
    }

    private Rect GetAbsolutePosition(VisualElement element)
    {
        if (element == null) return Rect.Zero;

        double x = element.X;
        double y = element.Y;
        double width = element.Width;
        double height = element.Height;

        var current = element.Parent as VisualElement;

        while (current != null && !(current is Page))
        {
            x += current.X;
            y += current.Y;

            if (current is ScrollView scrollView)
            {
                x -= scrollView.ScrollX;
                y -= scrollView.ScrollY;
            }

            current = current.Parent as VisualElement;
        }

        return new Rect(x, y, width, height);
    }

    private async Task AnimateArrowPointer()
    {
        ArrowPointer.Opacity = 0;
        ArrowPointer.Scale = 0.8;

        await Task.Delay(100);

        await Task.WhenAll(
            ArrowPointer.FadeTo(1, 300, Easing.CubicOut),
            ArrowPointer.ScaleTo(1, 300, Easing.BounceOut)
        );

        _ = Task.Run(async () =>
        {
            try
            {
                while (ArrowPointer.Opacity > 0 && TutorialOverlay.IsVisible)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (ArrowPointer.Opacity > 0 && TutorialOverlay.IsVisible)
                        {
                            await ArrowPointer.ScaleTo(1.2, 500, Easing.CubicInOut);
                            if (ArrowPointer.Opacity > 0 && TutorialOverlay.IsVisible)
                            {
                                await ArrowPointer.ScaleTo(1.0, 500, Easing.CubicInOut);
                            }
                        }
                    });
                    await Task.Delay(100);
                }
            }
            catch { }
        });
    }

    private void HighlightTargetElement(string? targetName)
    {
        QuestionArea.Opacity = 1;
        ChoicesArea.Opacity = 1;

        if (string.IsNullOrEmpty(targetName))
            return;

        switch (targetName)
        {
            case "QuestionArea":
                ChoicesArea.Opacity = 0.3;
                break;
            case "ChoicesArea":
                QuestionArea.Opacity = 0.5;
                break;
        }
    }

    private async Task CompleteTutorial()
    {
        Preferences.Set(TUTORIAL_COMPLETED_KEY, true);
        StopTutorialAudio();
        
        // Ensure music plays if tutorial ends or is skipped early
        if (Application.Current is App app)
        {
            app.ResumeBackgroundMusic();
        }

        await Task.WhenAll(
            ArrowPointer.FadeTo(0, 200),
            SpeechBubbleContainer.FadeTo(0, 300),
            TarsierImage.FadeTo(0, 300),
            TutorialOverlay.FadeTo(0, 400)
        );

        TutorialOverlay.IsVisible = false;

        QuestionArea.Opacity = 1;
        ChoicesArea.Opacity = 1;

        var story = AlamatContent.GetStory(_storyId);
        var q = story.Quiz[_quizIndex];
        _cts = new CancellationTokenSource();
        _ = TimerAsync(q.TimeLimitSec, _cts.Token);
    }

    async Task LoadQuiz()
    {
        var story = AlamatContent.GetStory(_storyId);

        var q = story.Quiz[_quizIndex];

        await AnimateQuizSwapAsync(() =>
        {
            QuizPrompt.Text = q.Prompt;
            _correctIndex = q.CorrectIndex;

            Choice0Text.Text = q.ChoiceTexts.ElementAtOrDefault(0) ?? "";
            Choice1Text.Text = q.ChoiceTexts.ElementAtOrDefault(1) ?? "";
            Choice2Text.Text = q.ChoiceTexts.ElementAtOrDefault(2) ?? "";
        });
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = TimerAsync(q.TimeLimitSec, _cts.Token);
    }

    async Task TimerAsync(int seconds, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
            if (!ct.IsCancellationRequested)
                await HandleWrongAsync();
        }
        catch (TaskCanceledException) { }
    }
    async Task AnimateQuizSwapAsync(Action swapContent)
    {
        await Task.WhenAll(
            QuizContentWrapper.FadeTo(0, 150, Easing.CubicIn),
            QuizContentWrapper.TranslateTo(0, -12, 150, Easing.CubicIn)
         );

        swapContent.Invoke();

        QuizContentWrapper.TranslationY = 12;

        await Task.WhenAll(
            QuizContentWrapper.FadeTo(1, 180, Easing.CubicOut),
            QuizContentWrapper.TranslateTo(0, 0, 180, Easing.CubicOut)
         );
    }

    async Task HandlePickAsync(int idx)
    {
        await SoundService.PlayButtonClickAsync();

        if (HeartService.GetHearts() <= 0)
        {
            await DisplayAlert(
                "Wala nang ??",
                "Babalik ang mga puso pagkalipas ng 5 minuto.",
                "OK"
            );
            return;
        }

        if (idx == _correctIndex)
            await HandleCorrectAsync();
        else
            await HandleWrongAsync();
    }
    async Task HandleCorrectAsync()
    {
        _cts?.Cancel();

        var story = AlamatContent.GetStory(_storyId);
        var currentQuestion = story.Quiz[_quizIndex];

        bool isFirstTime = await App.Database.SetQuizQuestionAnsweredAsync(_storyId, _quizIndex);

        if (isFirstTime)
        {
            int starsToAward = currentQuestion.RewardStars;
            _totalQuizStarsEarned += starsToAward;

            await App.Database.SetStarsAsync(CharacterHelper.CurrentStars + starsToAward);
            CharacterHelper.CurrentStars += starsToAward;
            RefreshStars();
        }

        _quizIndex++;

        if (_quizIndex < story.Quiz.Count)
        {
            await LoadQuiz();
            return;
        }

        var reward = story.RewardStars;
        var isClaimed = story.IsRewardClaimed;

        if (!isClaimed)
        {
            await App.Database.SetStarsAsync(CharacterHelper.CurrentStars + reward);
            CharacterHelper.CurrentStars += reward;
            RefreshStars();
        }

        await Navigation.PushAsync(new RewardPage(reward, _storyId));
    }

    async Task HandleWrongAsync()
    {
        _cts?.Cancel();
        HeartService.LoseHeart();
        RefreshHearts();

        await ShowWrongModalAsync();
    }

    async Task ShowWrongModalAsync()
    {
        var story = AlamatContent.GetStory(_storyId);

        AlertNarrator.Source = AlamatContent.CurrentNarrator.Avatar;

        AlertHeartsPanel.Children.Clear();
        for (int i = 0; i < AlamatContent.Hearts; i++)
        {
            AlertHeartsPanel.Children.Add(new Image
            {
                Source = "heart_full.png",
                WidthRequest = 24,
                HeightRequest = 24,
                Aspect = Aspect.AspectFit
            });
        }

        AlertThumb.Source = story.Thumb;

        GameAlertOverlay.IsVisible = true;
        GameAlertOverlay.Opacity = 0;
        GameAlertCard.Scale = 0.96;

        await Task.WhenAll(
            GameAlertOverlay.FadeTo(1, 180, Easing.CubicOut),
            GameAlertCard.ScaleTo(1.0, 180, Easing.CubicOut)
        );
    }

    async Task HideWrongModalAsync()
    {
        await Task.WhenAll(
            GameAlertOverlay.FadeTo(0, 80, Easing.CubicIn),
            GameAlertCard.ScaleTo(0.96, 80, Easing.CubicIn)
        );
        GameAlertOverlay.IsVisible = false;
    }

    async void OnAlertReplayTapped(object? s, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await HideWrongModalAsync();
        await Navigation.PushAsync(new StoryPage(_storyId));
    }

    async void OnAlertCloseTapped(object? s, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        _cts?.Cancel();
        var pages = Navigation.NavigationStack.ToList();
        foreach (var page in pages)
        {
            if (page is RewardPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is QuizPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is StoryPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is NarratorPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is AlamatPage)
            {
                Navigation.RemovePage(page);
            }
        }
        await Navigation.PushAsync(new IndexPage());
    }

    void OnAlertBackgroundTapped(object? s, TappedEventArgs e)
    {
    }


    async void OnPick0(object? s, TappedEventArgs e) => await HandlePickAsync(0);
    async void OnPick1(object? s, TappedEventArgs e) => await HandlePickAsync(1);
    async void OnPick2(object? s, TappedEventArgs e) => await HandlePickAsync(2);

    void LoadHud()
    {
        HudAvatar.Source = CharacterHelper.CurrentAvatar;
        NarratorAvatar.Source = AlamatContent.CurrentNarrator.Avatar;
        PlayerNameLabel.Text = CharacterHelper.CurrentName;
        RefreshStars();
        RefreshHearts();
    }

    void RefreshStars()
    {
        StarsLabel.Text = CharacterHelper.CurrentStars.ToString();
    }

    void RefreshHearts()
    {
        HeartsPanel.Children.Clear();
        for (int i = 0; i < AlamatContent.Hearts; i++)
        {
            HeartsPanel.Children.Add(new Image
            {
                Source = "heart_full.png",
                WidthRequest = 24,
                HeightRequest = 24,
                Aspect = Aspect.AspectFit
            });
        }
    }

    async void OnHomeTapped(object? s, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        _cts?.Cancel();
        var pages = Navigation.NavigationStack.ToList();
        foreach (var page in pages)
        {
            if (page is RewardPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is QuizPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is StoryPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is NarratorPage)
            {
                Navigation.RemovePage(page);
            }
            if (page is AlamatPage)
            {
                Navigation.RemovePage(page);
            }
        }

        await Navigation.PushAsync(new IndexPage());
    }

    private class TutorialStep
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? TargetElementName { get; set; }
        public double OffsetX { get; set; } = 0;
        public string? AudioPath { get; set; }
    }
}