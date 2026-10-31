using System;
using Microsoft.Maui.Controls;
using FilipinoFolkloreApp.Views;
using FilipinoFolkloreApp.Views.Home;
using FilipinoFolkloreApp.Models;
using System.Threading.Tasks;
using FilipinoFolkloreApp.Services;
using Microsoft.Maui.Layouts;
using Plugin.Maui.Audio;
using System.IO;

namespace FilipinoFolkloreApp
{
    public partial class MainPage : ContentPage
    {
        private const int MIN_NAME_LENGTH = 2;
        private const int MAX_NAME_LENGTH = 20;

        private bool _navigated = false;

        private int _tutorialStep = 0;
        private const string TUTORIAL_COMPLETED_KEY = "MainPageTutorialCompleted";

        private IAudioPlayer? _tutorialAudioPlayer;
        private Stream? _tutorialAudioStream;

        private readonly TutorialStep[] _tutorialSteps = new[]
        {
            new TutorialStep
            {
                Title = "Maligayang pagdating!",
                Message = "Ako si Tarsy, ang iyong gabay! Tulungan kitang magsimula sa paglalakbay sa mundo ng Filipino Folklore.",
                TargetElementName = null,
            },
            new TutorialStep
            {
                Title = "Ilagay ang Iyong Pangalan",
                Message = "Pindutin ang mga titik sa ibaba para magsimula ng paglagay ng iyong pangalan. Kailangan ng 2-20 titik.",
                TargetElementName = "OutputEntry",
            },
            new TutorialStep
            {
                Title = "Tanggalin ang Mali",
                Message = "Kung may mali, pindutin ang ekis na button para burahin ang lahat ng titik.",
                TargetElementName = "EraseButton",
            },
            new TutorialStep
            {
                Title = "Ipagpatuloy",
                Message = "Kapag tapos ka na, pindutin ang OK button para magpatuloy sa paglalakbay!",
                TargetElementName = "EnterButton",
            }
        };

        public MainPage()
        {
            InitializeComponent();
            
            // Dynamically assign audio paths based on their sequential order
            for (int i = 0; i < _tutorialSteps.Length; i++)
            {
                _tutorialSteps[i].AudioPath = $"tutorialaudio/mainpagetutorial/mainpagetutorial{i + 1}.m4a"; // Adjust path dynamically based on your assets
            }
            
            UpdateCharacterCounter();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

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
                await PositionArrowToElement(step.TargetElementName);
            }
            else
            {
                ArrowPointer.Opacity = 0;
                PositionSpeechBubble(true);
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

        private async Task PositionArrowToElement(string elementName)
        {
            VisualElement? targetElement = elementName switch
            {
                "OutputEntry" => OutputEntry,
                "EraseButton" => EraseButton,
                "EnterButton" => EnterButton,
                "KeyboardGrid" => KeyboardGrid,
                _ => null
            };

            if (targetElement == null)
            {
                ArrowPointer.Opacity = 0;
                return;
            }

            await Task.Delay(100);

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
            PositionSpeechBubble(!arrowIsAtTopHalf);

            await AnimateArrowPointer();
        }

        private void PositionSpeechBubble(bool positionAtTop)
        {
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            double screenHeight = displayInfo.Height / displayInfo.Density;

            double bubbleX = 160;

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
            OutputEntry.Opacity = 1;
            KeyboardGrid.Opacity = 1;

            if (string.IsNullOrEmpty(targetName))
                return;

            switch (targetName)
            {
                case "OutputEntry":
                    KeyboardGrid.Opacity = 0.3;
                    break;
                case "EraseButton":
                case "EnterButton":
                    OutputEntry.Opacity = 0.5;
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

            OutputEntry.Opacity = 1;
            KeyboardGrid.Opacity = 1;
        }

        private async void Letter_Clicked(object sender, EventArgs e)
        {
            if (sender is ImageButton btn && btn.CommandParameter is string letter)
            {
                if (letter == "enter")
                {
                    var name = (OutputEntry.Text ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        await DisplayAlert("Kailangan ang Pangalan", "Pakilagay ang iyong pangalan.", "OK");
                        return;
                    }

                    if (name.Length < MIN_NAME_LENGTH)
                    {
                        await DisplayAlert("Masyadong Maikli", $"Ang pangalan ay dapat hindi bababa sa {MIN_NAME_LENGTH} titik.", "OK");
                        return;
                    }

                    if (name.Length > MAX_NAME_LENGTH)
                    {
                        await DisplayAlert("Masyadong Mahaba", $"Ang pangalan ay hindi dapat lumampas sa {MAX_NAME_LENGTH} titik.", "OK");
                        return;
                    }

                    try
                    {
                        await App.Database.SaveCharAsync(new Character { Id = 1, name = name, stars = 50 });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Save name failed: {ex}");
                        await DisplayAlert("Hindi Na-save", "Hindi naisave ang pangalan. Subukang muli.", "OK");
                        return;
                    }

                    if (!_navigated)
                    {
                        _navigated = true;
                        CharacterHelper.CurrentName = name;
                        CharacterHelper.CurrentStars = 50;

                        await Navigation.PushAsync(new AvatarSelectionPage(), true);
                        Navigation.RemovePage(this);
                    }
                }
                else if (letter == "erase")
                {
                    OutputEntry.Text = string.Empty;
                    UpdateCharacterCounter();
                }
                else
                {
                    string currentText = OutputEntry.Text ?? string.Empty;
                    if (currentText.Length < MAX_NAME_LENGTH)
                    {
                        OutputEntry.Text += letter.ToUpper();
                        UpdateCharacterCounter();
                    }
                    else
                    {
                        AnimateMaxLengthReached();
                    }
                }
            }
        }

        private void UpdateCharacterCounter()
        {
            int currentLength = OutputEntry.Text?.Length ?? 0;
            CharCounterLabel.Text = $"{currentLength}/{MAX_NAME_LENGTH}";

            if (currentLength >= MAX_NAME_LENGTH)
            {
                CharCounterLabel.TextColor = Color.FromArgb("#FF1493");
            }
            else if (currentLength >= MAX_NAME_LENGTH - 5)
            {
                CharCounterLabel.TextColor = Color.FromArgb("#FF69B4");
            }
            else
            {
                CharCounterLabel.TextColor = Color.FromArgb("#FFB6C1");
            }
        }

        private async void AnimateMaxLengthReached()
        {
            try
            {
                await CharCounterLabel.TranslateTo(-10, 0, 50);
                await CharCounterLabel.TranslateTo(10, 0, 50);
                await CharCounterLabel.TranslateTo(-10, 0, 50);
                await CharCounterLabel.TranslateTo(10, 0, 50);
                await CharCounterLabel.TranslateTo(0, 0, 50);
            }
            catch
            {
            }
        }

        private class TutorialStep
        {
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string? TargetElementName { get; set; }
            public string? AudioPath { get; set; }
        }
    }
}