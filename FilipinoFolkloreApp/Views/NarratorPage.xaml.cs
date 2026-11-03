using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using FilipinoFolkloreApp.Services;
using FilipinoFolkloreApp.Views.Home;
using Microsoft.Maui.Layouts;
using Plugin.Maui.Audio;
using System.IO;

namespace FilipinoFolkloreApp.Views;

public partial class NarratorPage : ContentPage
{
    private readonly string _storyId;
    private TaskCompletionSource<bool>? _alertTcs;
    private HeartService HeartService =>
    Application.Current!.Handler!.MauiContext!.Services.GetService<HeartService>()!;

    private SoundService SoundService =>
        Application.Current!.Handler!.MauiContext!.Services.GetService<SoundService>()!;

    private bool _isNavigating = false;

    private int _tutorialStep = 0;
    private const string TUTORIAL_COMPLETED_KEY = "NarratorPageTutorialCompleted42";

    private IAudioPlayer? _tutorialAudioPlayer;
    private Stream? _tutorialAudioStream;

    private readonly TutorialStep[] _tutorialSteps = new[]
{
        new TutorialStep
        {
            Title = "Pumili ng Narrator!",
            Message = "Dito makikita mo ang mga narrator na pwedeng magbasa ng kuwento. Piliin ang gusto mo!",
            TargetElementName = null,

        },
        new TutorialStep
        {
            Title = "Libre ang Tarsier!",
            Message = "Ang Tarsier narrator ay laging libre at pwede mong gamitin anumang oras! Pindutin para pumili!",
            TargetElementName = "FirstUnlockedNarrator",
            OffsetX = 100
        },
        new TutorialStep
        {
            Title = "Mga Nakandado",
            Message = "Ang may lock icon at presyo ay kailangan pang bilhin gamit ang iyong coins. Pindutin para bumili!",
            TargetElementName = "FirstLockedNarrator",
            OffsetX = -100
        }
    };

    class Card
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Avatar { get; set; } = "";
        public bool IsLocked { get; set; }
        public int Price { get; set; }
        public string PriceText => $"{Price}";
    }

    public NarratorPage(string storyId)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        _storyId = storyId;

        // Dynamically assign audio paths based on their sequential order
        for (int i = 0; i < _tutorialSteps.Length; i++)
        {
            _tutorialSteps[i].AudioPath = $"tutorialaudio/narratorpagetutorial/narratorpagetutorial{i + 1}.m4a"; // Adjust path dynamically based on your assets
        }  
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _isNavigating = false;

        AlamatContent.Hearts = HeartService.GetHearts();

        AlamatContent.CheckAndRefreshNarratorBattery();

        try
        {
            await App.Database.LoadStoriesAsync();
            await App.Database.LoadNarratorDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing stories on appear: {ex}");
        }

        LoadHud();
        RefreshNarratorList();

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
        Rect targetBounds = Rect.Zero;

        if (elementName == "FirstUnlockedNarrator" || elementName == "FirstLockedNarrator")
        {
            await Task.Delay(200);
            Rect cvBounds = GetAbsolutePosition(NarratorsView);

            if (cvBounds != Rect.Zero)
            {
                double columns = 3; if (DeviceInfo.Idiom == DeviceIdiom.Tablet) columns = 5;
                if (DeviceInfo.Idiom == DeviceIdiom.Desktop) columns = 6;

                double spacing = 16;
                double cardWidth = (cvBounds.Width - (spacing * (columns - 1))) / columns;

                double targetX = cvBounds.X;

                if (elementName == "FirstLockedNarrator")
                {
                    targetX += cardWidth + spacing;
                }

                targetBounds = new Rect(targetX, cvBounds.Y, cardWidth, 180);
            }
        }
        else if (elementName == "NarratorsGridContainer")
        {
            targetBounds = GetAbsolutePosition(NarratorsGridContainer);
        }

        if (targetBounds == Rect.Zero)
        {
            ArrowPointer.Opacity = 0;
            return;
        }

        await Task.Delay(150);

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        double screenHeight = displayInfo.Height / displayInfo.Density;
        double safeZone = 60;

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

    private VisualElement? GetNarratorCardByIndex(int targetIndex)
    {
        try
        {
            if (NarratorsView?.Handler?.PlatformView != null)
            {
                var items = GetVisualTreeDescendants(NarratorsView);
                var narratorCards = new List<Grid>();

                foreach (var item in items)
                {
                    if (item is Grid grid &&
                        grid.BindingContext is Card &&
                        grid.IsVisible)
                    {
                        narratorCards.Add(grid);
                    }
                }

                if (targetIndex >= 0 && targetIndex < narratorCards.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"Found narrator card at index {targetIndex}: {(narratorCards[targetIndex].BindingContext as Card)?.Name}");
                    return narratorCards[targetIndex];
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Narrator card index {targetIndex} out of range. Total cards: {narratorCards.Count}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting narrator card by index: {ex}");
        }

        return null;
    }

    private IEnumerable<Element> GetVisualTreeDescendants(Element element)
    {
        var queue = new Queue<Element>();
        queue.Enqueue(element);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            if (current is Layout layout)
            {
                foreach (var child in layout.Children.OfType<Element>())
                {
                    queue.Enqueue(child);
                }
            }
            else if (current is ContentView contentView && contentView.Content != null)
            {
                queue.Enqueue(contentView.Content);
            }
            else if (current is ScrollView scrollView && scrollView.Content != null)
            {
                queue.Enqueue(scrollView.Content);
            }
        }
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
        NarratorsGridContainer.Opacity = 1;

        if (string.IsNullOrEmpty(targetName))
            return;

        switch (targetName)
        {
            case "FirstUnlockedNarrator":
            case "FirstLockedNarrator":
                NarratorsGridContainer.Opacity = 0.95;
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

        NarratorsGridContainer.Opacity = 1;
    }

    void RefreshNarratorList()
    {
        var story = AlamatContent.Stories.FirstOrDefault(s => s.Id == _storyId);

        NarratorsView.ItemsSource = AlamatContent.Narrators.Select(n =>
        {
            bool unlocked = n.Id == "tarsier"
  || AlamatContent.UnlockedNarrators.Contains(n.Id) || (story != null && n.Id == "eagle" && story.NarratorEagleUnlocked)
  || (story != null && n.Id == "monkey" && story.NarratorMonkeyUnlocked);

            return new Card
            {
                Id = n.Id,
                Name = n.Name,
                Avatar = n.Avatar,
                IsLocked = !unlocked,
                Price = n.PriceStars
            };
        }).ToList();
    }

    async void OnNarratorTapped(object? sender, TappedEventArgs e)
    {
        if (_isNavigating)
        {
            System.Diagnostics.Debug.WriteLine("Double-tap prevented: Already navigating to story.");
            return;
        }

        await SoundService.PlayButtonClickAsync();

        if (sender is not Grid g || g.BindingContext is not Card c) return;

        if (!AlamatContent.CanUseNarrator())
        {
            var timeSinceLastUse = DateTime.Now - AlamatContent.LastNarratorUseTime;
            var minutesUntilRefresh = 10 - ((int)timeSinceLastUse.TotalMinutes % 10);

            await ShowGameAlertAsync(
                $"Maghintay ng {minutesUntilRefresh} minuto para sa susunod na narrator battery.",
                false
            );
            return;
        }

        _isNavigating = true;

        try
        {
            var story = AlamatContent.GetStory(_storyId);

            bool alreadyUnlockedForThisStory =
                c.Id == "tarsier" ||
                AlamatContent.UnlockedNarrators.Contains(c.Id) || (c.Id == "eagle" && story.NarratorEagleUnlocked) ||
                (c.Id == "monkey" && story.NarratorMonkeyUnlocked);

            if (!alreadyUnlockedForThisStory)
            {
                if (!AlamatContent.TrySpendStars(c.Price))
                {
                    await ShowGameAlertAsync($"Kailangan: {c.Price}", false);
                    _isNavigating = false; return;
                }

                bool previousEagle = story.NarratorEagleUnlocked;
                bool previousMonkey = story.NarratorMonkeyUnlocked;

                switch (c.Id)
                {
                    case "eagle":
                        story.NarratorEagleUnlocked = true;
                        break;
                    case "monkey":
                        story.NarratorMonkeyUnlocked = true;
                        break;
                }

                bool saved = false;
                try
                {
                    await App.Database.UpdateStoryAsync(story);
                    await App.Database.SetStarsAsync(CharacterHelper.CurrentStars - c.Price);
                    CharacterHelper.CurrentStars -= c.Price; saved = true;
                }
                catch (Exception ex)
                {
                    story.NarratorEagleUnlocked = previousEagle;
                    story.NarratorMonkeyUnlocked = previousMonkey;
                    AlamatContent.Stars += c.Price; System.Diagnostics.Debug.WriteLine($"UpdateStoryAsync failed while unlocking narrator: {ex}");
                    await ShowGameAlertAsync("Hindi naisave ang narrator — subukang muli.", false);
                }

                if (!saved)
                {
                    LoadHud();
                    RefreshNarratorList();
                    _isNavigating = false; return;
                }

                LoadHud();
                RefreshNarratorList();
            }

            await AlamatContent.UseNarratorAsync();

            AlamatContent.SelectedNarratorId = c.Id;
            AlamatContent.CurrentNarratorImage = c.Avatar;
            await App.Database.UpdateSelectedNarratorAsync(c.Id);

            await Navigation.PushAsync(new StoryPage(_storyId));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnNarratorTapped: {ex}");
            _isNavigating = false;
        }
    }

    void LoadHud()
    {
        HudAvatar.Source = CharacterHelper.CurrentAvatar;
        StarsLabel.Text = CharacterHelper.CurrentStars.ToString();
        PlayerNameLabel.Text = CharacterHelper.CurrentName;
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

    private Task<bool> ShowGameAlertAsync(string message, bool showYesNo = false)
    {
        if (GameAlertOverlay.IsVisible && _alertTcs != null)
            return _alertTcs.Task;

        _alertTcs = new TaskCompletionSource<bool>();

        AlertMessageLabel.Text = message;

        AlertButtonsPanel.Children.Clear();

        if (showYesNo)
        {
            var yesButton = new Button
            {
                Text = "Oo",
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 18,
                HeightRequest = 44,
                WidthRequest = 100,
                BackgroundColor = Color.FromArgb("#00A6FF"),
                TextColor = Colors.White
            };
            yesButton.Clicked += (s, e) => OnAlertYesClicked(s, e);
            AlertButtonsPanel.Children.Add(yesButton);

            var noButton = new Button
            {
                Text = "Hindi",
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 18,
                HeightRequest = 44,
                WidthRequest = 100,
                BackgroundColor = Color.FromArgb("#FF6B6B"),
                TextColor = Colors.White
            };
            noButton.Clicked += (s, e) => OnAlertNoClicked(s, e);
            AlertButtonsPanel.Children.Add(noButton);
        }
        else
        {
            var okButton = new Button
            {
                Text = "OK",
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 18,
                HeightRequest = 44,
                WidthRequest = 120,
                BackgroundColor = Color.FromArgb("#00A6FF"),
                TextColor = Colors.White
            };
            okButton.Clicked += (s, e) => OnAlertOkClicked(s, e);
            AlertButtonsPanel.Children.Add(okButton);
        }

        GameAlertOverlay.IsVisible = true;
        GameAlertOverlay.Opacity = 0;
        GameAlertCard.Scale = 0.96;

        _ = AnimateShowOverlayAsync();

        return _alertTcs.Task;
    }

    private async Task AnimateShowOverlayAsync()
    {
        try
        {
            await GameAlertOverlay.FadeTo(1, 180, Easing.CubicIn);
            await GameAlertCard.ScaleTo(1.06, 220, Easing.CubicOut);
            await GameAlertCard.ScaleTo(1.0, 120, Easing.CubicIn);
        }
        catch { }
    }

    private async Task HideGameAlertAsync(bool result)
    {
        if (!GameAlertOverlay.IsVisible) return;

        try
        {
            await GameAlertCard.ScaleTo(0.96, 120, Easing.CubicIn);
            await GameAlertOverlay.FadeTo(0, 140, Easing.CubicOut);
        }
        catch { }

        GameAlertOverlay.IsVisible = false;

        _alertTcs?.TrySetResult(result);
        _alertTcs = null;
    }

    private async void OnAlertOkClicked(object? sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await HideGameAlertAsync(true);
    }

    private async void OnAlertYesClicked(object? sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await HideGameAlertAsync(true);
    }

    private async void OnAlertNoClicked(object? sender, EventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        await HideGameAlertAsync(false);
    }

    private async void OnAlertBackgroundTapped(object? sender, EventArgs e)
    {
        await HideGameAlertAsync(false);
    }

    async void OnBackTapped(object? s, TappedEventArgs e)
    {
        await SoundService.PlayButtonClickAsync();
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }

    async void OnHomeTapped(object? s, TappedEventArgs e)
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