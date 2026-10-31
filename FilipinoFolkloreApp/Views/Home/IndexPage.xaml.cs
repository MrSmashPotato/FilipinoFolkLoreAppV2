using FilipinoFolkloreApp.Services;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Maui.Audio;
using System.IO;

namespace FilipinoFolkloreApp.Views.Home;

public partial class IndexPage : ContentPage
{
    private HeartService HeartService =>
        Application.Current!.Handler!.MauiContext!.Services.GetService<HeartService>()!;

    private SoundService SoundService =>
        Application.Current!.Handler!.MauiContext!.Services.GetService<SoundService>()!;

    // Double-tap prevention flag
    private bool _isNavigating = false;

    // Tutorial state
    private int _tutorialStep = 0;
    private const string TUTORIAL_COMPLETED_KEY = "IndexPageTutorialCompleted123";

    private IAudioPlayer? _tutorialAudioPlayer;
    private Stream? _tutorialAudioStream;

    // Tutorial steps configuration
    private readonly TutorialStep[] _tutorialSteps = new[]
    {
        new TutorialStep
        {
            Title = "Maligayang Pagdating sa Home!",
            Message = "Ito ang iyong Home Page! Dito mo makikita ang lahat ng mahalagang bagay.",
            TargetElementName = null
        },
        new TutorialStep
        {
            Title = "Iyong Avatar",
            Message = "Pindutin ang iyong avatar para magpalit ng kasuotan at hitsura!",
            TargetElementName = "HudAvatar"
        },
        new TutorialStep
        {
            Title = "Coins",
            Message = "Dito mo makikita ang iyong coins na kikitain sa bawat adventure!",
            TargetElementName = "StarsLabel"
        },
        new TutorialStep
        {
            Title = "Mga Puso",
            Message = "Ito ang iyong mga buhay. Kailangan mo nito para maglaro ng mga quiz!",
            TargetElementName = "HeartsPanel"
        },
        new TutorialStep
        {
            Title = "Medalya",
            Message = "Pindutin ito para tingnan ang mga medalyang nakuha mo!",
            TargetElementName = "MedalButton"
        },
        new TutorialStep
        {
            Title = "Settings",
            Message = "Pindutin ito para i-adjust ang tunog ng musika at narrator!",
            TargetElementName = "SettingsButton"
        },
        new TutorialStep
        {
            Title = "Alamat",
            Message = "Pindutin ito para magbasa ng mga Alamat - mga kwentong nagpapaliwanag kung paano nabuo ang mga bagay!",
            TargetElementName = "AlamatButton",
            OffsetX = 150
        },
        new TutorialStep
        {
            Title = "Epiko",
            Message = "Pindutin ito para magbasa ng mga Epiko - mga kwento ng mga bayani at kanilang mga paglalakbay!",
            TargetElementName = "EpikoButton",
            OffsetX = 300
        },
        new TutorialStep
        {
            Title = "Pabula",
            Message = "Pindutin ito para magbasa ng mga Pabula - mga kwentong may aral na may mga hayop bilang tauhan!",
            TargetElementName = "PabulaButton",
            OffsetX = -100
        },
        new TutorialStep
        {
            Title = "Mga Laro",
            Message = "Pindutin ito para maglaro ng Bugtong at Magkulay!",
            TargetElementName = "MgaLaroButton"
        }
    };

    public IndexPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        
        // Dynamically assign audio paths based on their sequential order
        for (int i = 0; i < _tutorialSteps.Length; i++)
        {
            _tutorialSteps[i].AudioPath = $"tutorialaudio/indexpagetutorial/indexpagetutorial{i + 1}.m4a";
        }
        
        loadhud();
        var data = App.Database.GetCharAsync();
        LoadSettings();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reset navigation flag when page appears
        _isNavigating = false;

        AlamatContent.Hearts = HeartService.GetHearts();
        loadhud();
        LoadSettings();

        // Resume background music based on settings
        if (Application.Current is App app)
        {
            app.ResumeBackgroundMusic();
        }

        // Check if tutorial should be shown
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

        // Animate tarsier entrance
        await Task.WhenAll(
            TarsierImage.FadeTo(1, 400, Easing.CubicOut),
            TarsierImage.ScaleTo(1, 400, Easing.BounceOut)
        );

        // Animate speech bubble
        await Task.Delay(200);
        await Task.WhenAll(
            SpeechBubbleContainer.FadeTo(1, 300, Easing.CubicOut),
            SpeechBubbleContainer.ScaleTo(1, 300, Easing.BounceOut)
        );

    }

    private async void OnTutorialPrevStep(object? sender, EventArgs e)
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
            "HudAvatar" => HudAvatar,
            "StarsLabel" => StarsLabel.Parent as VisualElement,
            "HeartsPanel" => HeartsPanel,
            "MedalButton" => MedalButton,
            "SettingsButton" => SettingsButton,
            "AlamatButton" => AlamatButton,
            "EpikoButton" => EpikoButton,
            "PabulaButton" => PabulaButton,
            "MgaLaroButton" => MgaLaroButton,
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

        double bubbleX = 160;
        bubbleX += offsetX;
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
            catch
            {
               
            }
        });
    }

    private void HighlightTargetElement(string? targetName)
    {
        HudAvatar.Opacity = 1;
        StarsLabel.Opacity = 1;
        HeartsPanel.Opacity = 1;
        AlamatButton.Opacity = 1;
        EpikoButton.Opacity = 1;
        PabulaButton.Opacity = 1;
        MgaLaroButton.Opacity = 1;

        if (string.IsNullOrEmpty(targetName))
            return;

        switch (targetName)
        {
            case "HudAvatar":
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                AlamatButton.Opacity = 0.3;
                EpikoButton.Opacity = 0.3;
                PabulaButton.Opacity = 0.3;
                MgaLaroButton.Opacity = 0.3;
                break;
            case "StarsLabel":
                HudAvatar.Opacity = 0.5;
                HeartsPanel.Opacity = 0.3;
                AlamatButton.Opacity = 0.3;
                EpikoButton.Opacity = 0.3;
                PabulaButton.Opacity = 0.3;
                MgaLaroButton.Opacity = 0.3;
                break;
            case "HeartsPanel":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                AlamatButton.Opacity = 0.3;
                EpikoButton.Opacity = 0.3;
                PabulaButton.Opacity = 0.3;
                MgaLaroButton.Opacity = 0.3;
                break;
            case "AlamatButton":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                EpikoButton.Opacity = 0.3;
                PabulaButton.Opacity = 0.3;
                MgaLaroButton.Opacity = 0.3;
                break;
            case "EpikoButton":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                AlamatButton.Opacity = 0.3;
                PabulaButton.Opacity = 0.3;
                MgaLaroButton.Opacity = 0.3;
                break;
            case "PabulaButton":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                AlamatButton.Opacity = 0.3;
                EpikoButton.Opacity = 0.3;
                MgaLaroButton.Opacity = 0.3;
                break;
            case "MgaLaroButton":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                AlamatButton.Opacity = 0.3;
                EpikoButton.Opacity = 0.3;
                PabulaButton.Opacity = 0.3;
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

        HudAvatar.Opacity = 1;
        StarsLabel.Opacity = 1;
        HeartsPanel.Opacity = 1;
        AlamatButton.Opacity = 1;
        EpikoButton.Opacity = 1;
        PabulaButton.Opacity = 1;
        MgaLaroButton.Opacity = 1;
    }

    void loadhud()
    {
        HudAvatar.Source = CharacterHelper.CurrentAvatar;
        PlayerNameLabel.Text = CharacterHelper.CurrentName;
        StarsLabel.Text = CharacterHelper.CurrentStars.ToString();

        RefreshHearts();
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

    private async void OnAvatarTapped(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new CharacterCostume());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAvatarTapped: {ex}");
            _isNavigating = false;
        }
    }

    private async void OnAlamatClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new AlamatPage("alamat"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAlamatClicked: {ex}");
            _isNavigating = false;
        }
    }

    private async void OnEpikoClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new AlamatPage("epiko"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnEpikoClicked: {ex}");
            _isNavigating = false;
        }
    }

    private async void OnPabulaClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new AlamatPage("pabula"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnPabulaClicked: {ex}");
            _isNavigating = false;
        }
    }

    private async void OnMedalyaClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new MedalPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnMedalyaClicked: {ex}");
            _isNavigating = false;
        }
    }

    private async void OnMgaLaroClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new MgaLaroPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnMgaLaroClicked: {ex}");
            _isNavigating = false;
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        SettingsModalOverlay.IsVisible = true;
    }

    private async void OnCloseSettingsModal(object sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        SettingsModalOverlay.IsVisible = false;
    }

    private void OnMusicToggled(object sender, ToggledEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.UpdateBackgroundMusic(e.Value);
        }
    }

    private void OnBackgroundVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        int volumePercent = (int)e.NewValue;
        BackgroundVolumeLabel.Text = $"{volumePercent}%";

        double volumeValue = e.NewValue / 100.0;
        Preferences.Set("BackgroundMusicVolume", volumeValue);

        if (Application.Current is App app)
        {
            app.SetBackgroundMusicVolume(volumeValue);
        }
    }

    private async void OnKreditsClicked(object sender, EventArgs e)
    {
        if (_isNavigating) return;

        await SoundService.PlayButtonClickAsync();

        // Close the settings modal first
        SettingsModalOverlay.IsVisible = false;

        _isNavigating = true;
        try
        {
            await Navigation.PushAsync(new CreditsPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnKreditsClicked: {ex}");
            _isNavigating = false;
        }
    }

    private void OnVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        int volumePercent = (int)e.NewValue;
        BackgroundVolumeLabel.Text = $"{volumePercent}%";

        double volumeValue = e.NewValue / 100.0;
        Preferences.Set("NarratorVolume", volumeValue);

        AlamatContent.NarratorVolume = volumeValue;
    }

    private void OnNarratorVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        int volumePercent = (int)e.NewValue;
        NarratorVolumeLabel.Text = $"{volumePercent}%";

        double volumeValue = e.NewValue / 100.0;
        Preferences.Set("NarratorVolume", volumeValue);

        AlamatContent.NarratorVolume = volumeValue;
    }

    private void LoadSettings()
    {
        double savedBackgroundVolume = Preferences.Get("BackgroundMusicVolume", 0.3);
        BackgroundVolumeSlider.ValueChanged -= OnBackgroundVolumeChanged;
        BackgroundVolumeSlider.Value = savedBackgroundVolume * 100;
        BackgroundVolumeLabel.Text = $"{(int)(savedBackgroundVolume * 100)}%";
        BackgroundVolumeSlider.ValueChanged += OnBackgroundVolumeChanged;

        double savedNarratorVolume = Preferences.Get("NarratorVolume", 1.0);
        AlamatContent.NarratorVolume = savedNarratorVolume;

        NarratorVolumeSlider.ValueChanged -= OnNarratorVolumeChanged;
        NarratorVolumeSlider.Value = savedNarratorVolume * 100;
        NarratorVolumeLabel.Text = $"{(int)(savedNarratorVolume * 100)}%";
        NarratorVolumeSlider.ValueChanged += OnNarratorVolumeChanged;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

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