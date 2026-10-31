using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using FilipinoFolkloreApp.Services;
using Microsoft.Maui.Layouts;
using Plugin.Maui.Audio;
using System.IO;

namespace FilipinoFolkloreApp.Views;

public partial class MgaLaroPage : ContentPage
{
    private SoundService SoundService =>
        Application.Current!.Handler!.MauiContext!.Services.GetService<SoundService>()!;

    private int _tutorialStep = 0;
    private const string TUTORIAL_COMPLETED_KEY = "MgaLaroPageTutorialCompleted7";

    private IAudioPlayer? _tutorialAudioPlayer;
    private Stream? _tutorialAudioStream;

    private readonly TutorialStep[] _tutorialSteps = new[]
    {
        new TutorialStep
        {
            Title = "Maligayang Pagdating sa Mga Laro!",
            Message = "Dito makikita mo ang mga laro na pwede mong laruin para kumita ng coins!",

        },
        new TutorialStep
        {
            Title = "Bugtong",
            Message = "Pindutin ito para maglaro ng Bugtong! Sagutan ang mga bugtong at manalo ng coins!",
            TargetElementName = "BugtongButton",
            OffsetX = +330
        },
        new TutorialStep
        {
            Title = "Magkulay",
            Message = "Pindutin ito para magkulay! Kulayan ang mga larawan at mag-enjoy!",
            TargetElementName = "MagkulayButton",
            OffsetX = +330
        },
        new TutorialStep
        {
            Title = "Estado ng Narrator",
            Message = "Pindutin ang puno para tingnan ang iyong koleksyon ng narrator at natitira nilang baterya!",
            TargetElementName = "NarratorTreeImage",
            OffsetX = +150
        }
    };

    public MgaLaroPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        for (int i = 0; i < _tutorialSteps.Length; i++)
        {
            _tutorialSteps[i].AudioPath = $"tutorialaudio/mgalaropagetutorial/mgalaropagetutorial{i + 1}.m4a";
        }

        LoadHUD();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadHUD();

        bool tutorialCompleted = Preferences.Get(TUTORIAL_COMPLETED_KEY, false);
        if (!tutorialCompleted)
        {
            await Task.Delay(500);
            await ShowTutorial();
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTutorialAudio();
    }

    private async Task ShowTutorial()
    {
        _tutorialStep = 0;

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
            if (Application.Current is App app)
            {
                app.PauseBackgroundMusic();
            }

            _tutorialAudioStream = await FileSystem.OpenAppPackageFileAsync(audioPath);
            _tutorialAudioPlayer = AudioManager.Current.CreatePlayer(_tutorialAudioStream);
            _tutorialAudioPlayer.Volume = AlamatContent.NarratorVolume;
            
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
            "GamesContentArea" => GamesContentArea,
            "BugtongButton" => BugtongButton,
            "MagkulayButton" => MagkulayButton,
            "NarratorTreeImage" => NarratorTreeImage,
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

        double x = element.X + element.TranslationX;
        double y = element.Y + element.TranslationY;
        double width = element.Width;
        double height = element.Height;

        var current = element.Parent as VisualElement;

        while (current != null && !(current is Page))
        {
            x += current.X + current.TranslationX;
            y += current.Y + current.TranslationY;

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
        GamesContentArea.Opacity = 1;
        BugtongButton.Opacity = 1;
        MagkulayButton.Opacity = 1;
        NarratorTreeImage.Opacity = 1;

        if (string.IsNullOrEmpty(targetName))
            return;

        switch (targetName)
        {
            case "GamesContentArea":
                break;
            case "BugtongButton":
                MagkulayButton.Opacity = 0.3;
                NarratorTreeImage.Opacity = 0.3;
                break;
            case "MagkulayButton":
                BugtongButton.Opacity = 0.3;
                NarratorTreeImage.Opacity = 0.3;
                break;
            case "NarratorTreeImage":
                BugtongButton.Opacity = 0.3;
                MagkulayButton.Opacity = 0.3;
                break;
        }
    }

    private async Task CompleteTutorial()
    {
        Preferences.Set(TUTORIAL_COMPLETED_KEY, true);
        StopTutorialAudio();
        
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

        GamesContentArea.Opacity = 1;
        BugtongButton.Opacity = 1;
        MagkulayButton.Opacity = 1;
        NarratorTreeImage.Opacity = 1;
    }

    private void LoadHUD()
    {
        HudAvatar.Source = CharacterHelper.CurrentAvatar;
        PlayerNameLabel.Text = CharacterHelper.CurrentName;
        StarsLabel.Text = CharacterHelper.CurrentStars.ToString();
        NarratorImage.Source = AlamatContent.CurrentNarratorImage;
        NarratorBatteryImage.Source = AlamatContent.GetNarratorBatteryImage();
        RefreshHearts();
    }

    private void RefreshHearts()
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

    private async void OnNarratorTreeTapped(object? sender, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await Navigation.PushAsync(new NarratorManagementPage());
    }

    private async void OnBugtongTapped(object? sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await Navigation.PushAsync(new BugtongListPage());
    }

    private async void OnMagpintaTapped(object? sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await Navigation.PushAsync(new ColoringSelectionPage());
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await Navigation.PopAsync();
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await NavigationHelper.NavigateToIndexPage(Navigation);
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