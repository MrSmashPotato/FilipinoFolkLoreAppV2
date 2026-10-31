using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using FilipinoFolkloreApp.Views.Home;
using FilipinoFolkloreApp.Services;
using Microsoft.Maui.Layouts;
using Plugin.Maui.Audio;
using System.IO;

namespace FilipinoFolkloreApp.Views;

public partial class AlamatPage : ContentPage
{
    private HeartService HeartService =>
    Application.Current!.Handler!.MauiContext!.Services.GetService<HeartService>()!;

    private SoundService SoundService =>
        Application.Current!.Handler!.MauiContext!.Services.GetService<SoundService>()!;

    private TaskCompletionSource<bool>? _alertTcs;

    // Double-tap prevention flag
    private bool _isNavigating = false;

    // Tutorial state
    private int _tutorialStep = 0;
    private const string TUTORIAL_COMPLETED_KEY = "AlamatPageTutorialCompleted";

    private IAudioPlayer? _tutorialAudioPlayer;
    private Stream? _tutorialAudioStream;

    // Tutorial steps configuration
    private readonly TutorialStep[] _tutorialSteps = new[]
    {
        new TutorialStep
        {
            Title = "Maligayang Pagdating sa mga Kwento!",
            Message = "Dito makikita mo ang lahat ng kwentong maaari mong basahin!",
            TargetElementName = null,
        },
        new TutorialStep
        {
            Title = "Iyong Avatar",
            Message = "Ito ang iyong avatar at pangalan!",
            TargetElementName = "HudAvatar",
        },
        new TutorialStep
        {
            Title = "Coins",
            Message = "Dito mo makikita ang iyong coins na pwede mong gamitin para bumili ng mga kwento!",
            TargetElementName = "StarsLabel",
        },
        new TutorialStep
        {
            Title = "Mga Puso",
            Message = "Ito ang iyong mga puso o buhay para sa mga quiz!",
            TargetElementName = "HeartsPanel",
        },
        new TutorialStep
        {
            Title = "Balik Button",
            Message = "Pindutin ito para bumalik sa nakaraang page!",
            TargetElementName = "BackButton",
        },
        new TutorialStep
        {
            Title = "Home Button",
            Message = "Pindutin ito para bumalik sa Home Page!",
            TargetElementName = "HomeButton",
        },
        new TutorialStep
        {
            Title = "Mga Kwento",
            Message = "Ito ang mga kwentong maaari mong basahin! Ang may lock ay kailangan mong bilhin gamit ang coins.",
            TargetElementName = "SecondStoryCard",
            OffsetX = +200
        }
    };

    public class StoryCard
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Thumb { get; set; } = "";
        public bool IsLocked { get; set; }
        public string Category { get; set; } = "";
        public bool IsPurchased { get; set; }
        public bool IsRewardClaimed { get; set; }
        public int Price { get; set; }
        public string PriceText => $"{Price}";
    }

    public AlamatPage(string category)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        AlamatContent.category = category;
        
        // Dynamically assign audio paths based on their sequential order
        for (int i = 0; i < _tutorialSteps.Length; i++)
        {
            _tutorialSteps[i].AudioPath = $"tutorialaudio/alamatpagetutorial/alamatpagetutorial{i + 1}.m4a"; // Adjust path dynamically based on your assets
        }  

        LoadHud();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reset navigation flag when page appears
        _isNavigating = false;

        try
        {
            await App.Database.LoadStoriesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadStoriesAsync failed: {ex}");
        }

        LoadHud();
        LoadStories();

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
        _firstStoryImage = null; // Reset captured image reference for tutorial targeting
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

        // Update arrow pointer to point at target element dynamically
        if (!string.IsNullOrEmpty(step.TargetElementName))
        {
            await PositionArrowToElement(step.TargetElementName, step.OffsetX);
        }
        else
        {
            ArrowPointer.Opacity = 0;
            // Position speech bubble at default location (upper right of tarsier)
            PositionSpeechBubble(true, 0);
        }

        // Highlight target element
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


    // Update the PositionArrowToElement method to use the named elements directly
    private async Task PositionArrowToElement(string elementName, double offsetX = 0)
    {
        Rect targetBounds = Rect.Zero;

        // 1. Find the target bounds
        if (elementName == "SecondStoryCard" || elementName == "FirstStoryCard")
        {
            // Give the CollectionView a tiny moment to finish rendering
            await Task.Delay(200);

            Rect cvBounds = GetAbsolutePosition(StoriesView);

            if (cvBounds != Rect.Zero)
            {
                // Your XAML uses Span="5" and HorizontalItemSpacing="8"
                double columns = 5;
                double spacing = 8;

                // Calculate the exact width of one card based on the actual screen width
                double cardWidth = (cvBounds.Width - (spacing * (columns - 1))) / columns;

                // Start with the X position of the first column
                double targetX = cvBounds.X;

                // Shift right by one full card width + spacing to hit the second column
                if (elementName == "SecondStoryCard")
                {
                    targetX += cardWidth + spacing;
                }

                // Target box: X position + 8px visual padding, Y position + 8px padding
                targetBounds = new Rect(targetX + 8, cvBounds.Y + 8, cardWidth, 150);
            }
        }
        else
        {
            VisualElement? targetElement = elementName switch
            {
                "HudAvatar" => HudAvatar,
                "StarsLabel" => StarsLabel.Parent as VisualElement,
                "HeartsPanel" => HeartsPanel,
                "BackButton" => BackButton,
                "HomeButton" => HomeButton,
                _ => null
            };

            if (targetElement != null)
            {
                targetBounds = GetAbsolutePosition(targetElement);
            }
        }

        // If we couldn't find the bounds, hide the arrow and bail out
        if (targetBounds == Rect.Zero)
        {
            ArrowPointer.Opacity = 0;
            return;
        }

        // Wait briefly for layout to settle
        await Task.Delay(150);

        // 2. Get screen info (targetBounds is already calculated now!)
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        double screenHeight = displayInfo.Height / displayInfo.Density;
        double safeZone = 60; // Padding from edges (Safe Zone)

        // Arrow dimensions
        double arrowWidth = 50;
        double arrowHeight = 50;
        double padding = 10; // Space between arrow and element

        // 3. Calculate Potential Positions
        // Position A: Above the element (Pointing Down)
        double yAbove = targetBounds.Top - arrowHeight - padding;

        // Position B: Below the element (Pointing Up)
        double yBelow = targetBounds.Bottom + padding;

        // 4. Determine Best Position
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

        // 5. Apply Position and Rotation
        double arrowX = targetBounds.Center.X - (arrowWidth / 2);

        AbsoluteLayout.SetLayoutBounds(ArrowPointer, new Rect(arrowX, finalArrowY, arrowWidth, arrowHeight));
        AbsoluteLayout.SetLayoutFlags(ArrowPointer, AbsoluteLayoutFlags.None);

        ArrowPointer.Rotation = isArrowAbove ? 90 : -90;

        // 6. Sync Speech Bubble
        bool arrowIsAtTopHalf = finalArrowY < (screenHeight / 2);
        PositionSpeechBubble(!arrowIsAtTopHalf, offsetX);

        await AnimateArrowPointer();
    }

    private VisualElement? GetFirstVisibleStoryCard()
    {
        return _firstStoryImage;
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

        // Use a fixed X position instead of centering
        // Tarsier ends at 180 (X=30 + Width=150). 
        // We set Bubble X to 160 to slightly overlap the tail with the Tarsier.
        double bubbleX = 160;
        bubbleX += offsetX; // Apply any additional offset from the tutorial step
        double bubbleWidth = 350;
        double safePadding = 60; // Padding from top/bottom screen edges

        if (positionAtTop)
        {
            // Position at Top
            AbsoluteLayout.SetLayoutBounds(SpeechBubbleContainer, new Rect(bubbleX, safePadding, bubbleWidth, AbsoluteLayout.AutoSize));
        }
        else
        {
            // Position at Bottom
            // Using a fixed offset from bottom (e.g., 200) to ensure it doesn't cover keyboard/nav bar
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

        // Bounce animation loop
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
                // Animation cancelled - ignore
            }
        });
    }

    // Update HighlightTargetElement to use direct references
    private void HighlightTargetElement(string? targetName)
    {
        // Reset all highlights
        HudAvatar.Opacity = 1;
        StarsLabel.Opacity = 1;
        HeartsPanel.Opacity = 1;
        StoriesView.Opacity = 1;
        BackButton.Opacity = 1;
        HomeButton.Opacity = 1;

        if (string.IsNullOrEmpty(targetName))
            return;

        // Dim everything except target
        switch (targetName)
        {
            case "HudAvatar":
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                BackButton.Opacity = 0.3;
                HomeButton.Opacity = 0.3;
                StoriesView.Opacity = 0.3;
                break;
            case "StarsLabel":
                HudAvatar.Opacity = 0.5;
                HeartsPanel.Opacity = 0.3;
                BackButton.Opacity = 0.3;
                HomeButton.Opacity = 0.3;
                StoriesView.Opacity = 0.3;
                break;
            case "HeartsPanel":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                BackButton.Opacity = 0.3;
                HomeButton.Opacity = 0.3;
                StoriesView.Opacity = 0.3;
                break;
            case "BackButton":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                HomeButton.Opacity = 0.3;
                StoriesView.Opacity = 0.3;
                break;
            case "HomeButton":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                BackButton.Opacity = 0.3;
                StoriesView.Opacity = 0.3;
                break;
            case "SecondStoryCard":
                HudAvatar.Opacity = 0.5;
                StarsLabel.Opacity = 0.3;
                HeartsPanel.Opacity = 0.3;
                BackButton.Opacity = 0.3;
                HomeButton.Opacity = 0.3;
                StoriesView.Opacity = 1;
                break;
        }
    }

    private VisualElement? _firstStoryImage;

    private void OnTargetImageLoaded(object? sender, EventArgs e)
    {
        if (_firstStoryImage == null && sender is VisualElement img)
        {
            _firstStoryImage = img;
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
        StoriesView.Opacity = 1;
        BackButton.Opacity = 1;
        HomeButton.Opacity = 1;
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

    void LoadHud()
    {
        HudAvatar.Source = CharacterHelper.CurrentAvatar;
        PlayerNameLabel.Text = CharacterHelper.CurrentName;
        StarsLabel.Text = CharacterHelper.CurrentStars.ToString();
        RefreshHearts();
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
        await Navigation.PushAsync(new IndexPage());
    }

    void LoadStories()
    {
        var currentCategory = AlamatContent.category ?? "";

        StoriesView.ItemsSource = AlamatContent.Stories.Where(s => string.IsNullOrEmpty(currentCategory)
                || (!string.IsNullOrEmpty(s.Category)
                    && s.Category.Equals(currentCategory, StringComparison.OrdinalIgnoreCase))).Select(s => new StoryCard
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Thumb = s.Thumb,
                        IsPurchased = s.IsPurchased,
                        IsRewardClaimed = s.IsRewardClaimed,

                        Price = s.PriceStars,
                        IsLocked = !(s.PriceStars == 0 || s.IsPurchased || AlamatContent.UnlockedStories.Contains(s.Id))
                    }).ToList();
    }

    async void OnStoryTapped(object? sender, TappedEventArgs e)
    {
        if (_isNavigating)
        {
            System.Diagnostics.Debug.WriteLine("Double-tap prevented: Already navigating to NarratorPage.");
            return;
        }

        await SoundService.PlayButtonClickAsync();

        if (sender is not Grid g || g.BindingContext is not StoryCard card) return;

        _isNavigating = true;

        try
        {
            var story = AlamatContent.GetStory(card.Id);

            if (!AlamatContent.IsStoryUnlocked(card.Id))
            {
                if (!AlamatContent.TrySpendStars(card.Price))
                {
                    await ShowGameAlertAsync($"Kailangan: {card.Price}", false);
                    _isNavigating = false; 
                    return;
                }

                story.IsPurchased = true;

                bool savedToDb = false;
                try
                {
                    await App.Database.UpdateStoryAsync(story);

                    AlamatContent.UnlockedStories.Add(story.Id);
                    await App.Database.SetStarsAsync(CharacterHelper.CurrentStars - card.Price);
                    CharacterHelper.CurrentStars -= card.Price;
                    savedToDb = true;
                }
                catch (Exception ex)
                {
                    story.IsPurchased = false;
                    AlamatContent.Stars += card.Price;
                    System.Diagnostics.Debug.WriteLine($"UpdateStoryAsync failed: {ex}");

                    await ShowGameAlertAsync("Hindi naisave ang binili — subukang muli.", false);
                }

                if (!savedToDb)
                {
                    _isNavigating = false; 
                    return;
                }

                LoadHud();
                LoadStories();
            }

            AlamatContent.CurrentStoryId = story.Id;

            await Navigation.PushAsync(new NarratorPage(card.Id));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnStoryTapped: {ex}");
            _isNavigating = false; 
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

    private class TutorialStep
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? TargetElementName { get; set; }
        public double OffsetX { get; set; } = 0;
        public string? AudioPath { get; set; }
    }
}