using Microsoft.Maui.Controls.Shapes;

namespace SmartOrder.Shared.Services;

public enum AppMessageType
{
    Info,
    Success,
    Error
}

public sealed class AppMessageOptions
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string ButtonText { get; init; } = "Entendido";
    public AppMessageType Type { get; init; } = AppMessageType.Info;
}

public static class AppMessageService
{
    private static readonly SemaphoreSlim MessageLock = new(1, 1);

    public static async Task ShowAsync(AppMessageOptions options)
    {
        await MessageLock.WaitAsync();
        try
        {
            var currentPage = GetCurrentPage();
            if (currentPage == null)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => ShowInternalAsync(currentPage, options));
        }
        finally
        {
            MessageLock.Release();
        }
    }

    private static Page? GetCurrentPage()
    {
        var rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        return UnwrapPage(rootPage);
    }

    private static async Task ShowInternalAsync(Page currentPage, AppMessageOptions options)
    {
        if (currentPage is not ContentPage contentPage || contentPage.Content == null)
        {
            await currentPage.DisplayAlertAsync(options.Title, options.Message, options.ButtonText);
            return;
        }

        var originalContent = contentPage.Content;
        Grid hostGrid;
        var restoreOriginalContent = false;

        if (originalContent is Grid existingGrid)
        {
            hostGrid = existingGrid;
        }
        else
        {
            contentPage.Content = null;
            hostGrid = new Grid();
            hostGrid.Children.Add(originalContent);
            contentPage.Content = hostGrid;
            restoreOriginalContent = true;
        }

        var dismissed = new TaskCompletionSource<bool>();
        Grid? overlay = null;
        Border? messageCard = null;

        async Task CloseOverlayAsync()
        {
            if (dismissed.Task.IsCompleted)
            {
                return;
            }

            if (overlay != null && messageCard != null)
            {
                await Task.WhenAll(
                    overlay.FadeTo(0, 140, Easing.CubicIn),
                    messageCard.ScaleTo(0.96, 140, Easing.CubicIn));
            }

            if (overlay != null && hostGrid.Children.Contains(overlay))
            {
                hostGrid.Children.Remove(overlay);
            }

            if (restoreOriginalContent && contentPage.Content == hostGrid)
            {
                hostGrid.Children.Remove(originalContent);
                contentPage.Content = originalContent;
            }

            dismissed.TrySetResult(true);
        }

        overlay = BuildOverlay(options, CloseOverlayAsync, out messageCard);
        Grid.SetRow(overlay, 0);
        Grid.SetColumn(overlay, 0);
        Grid.SetRowSpan(overlay, Math.Max(hostGrid.RowDefinitions.Count, 1));
        Grid.SetColumnSpan(overlay, Math.Max(hostGrid.ColumnDefinitions.Count, 1));
        hostGrid.Children.Add(overlay);

        await Task.WhenAll(
            overlay.FadeTo(1, 170, Easing.CubicOut),
            messageCard.ScaleTo(1, 170, Easing.CubicOut));

        await dismissed.Task;
    }

    private static Grid BuildOverlay(AppMessageOptions options, Func<Task> closeAction, out Border messageCard)
    {
        var accentColor = options.Type switch
        {
            AppMessageType.Success => Color.FromArgb("#0B6E4F"),
            AppMessageType.Error => Color.FromArgb("#B42318"),
            _ => Color.FromArgb("#0F4C81")
        };

        var softColor = options.Type switch
        {
            AppMessageType.Success => Color.FromArgb("#E8F5EF"),
            AppMessageType.Error => Color.FromArgb("#FDECEC"),
            _ => Color.FromArgb("#EAF2FB")
        };

        var iconText = options.Type switch
        {
            AppMessageType.Success => "OK",
            AppMessageType.Error => "!",
            _ => "i"
        };

        var overlay = new Grid
        {
            BackgroundColor = Color.FromArgb("#66000000"),
            Padding = new Thickness(24),
            ZIndex = 9999,
            Opacity = 0
        };

        var backdropTap = new TapGestureRecognizer();
        backdropTap.Tapped += async (_, _) => await closeAction();

        var backdrop = new BoxView
        {
            Color = Colors.Transparent
        };
        backdrop.GestureRecognizers.Add(backdropTap);

        var closeButton = new Button
        {
            Text = options.ButtonText,
            BackgroundColor = accentColor,
            TextColor = Colors.White,
            CornerRadius = 14,
            HeightRequest = 50,
            FontAttributes = FontAttributes.Bold
        };
        closeButton.Clicked += async (_, _) => await closeAction();

        messageCard = new Border
        {
            Stroke = accentColor,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24) },
            BackgroundColor = Colors.White,
            Padding = new Thickness(24),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 520,
            Scale = 0.96,
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                Children =
                {
                    new Border
                    {
                        BackgroundColor = softColor,
                        Stroke = accentColor,
                        StrokeThickness = 1,
                        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
                        WidthRequest = 54,
                        HeightRequest = 54,
                        Padding = 0,
                        HorizontalOptions = LayoutOptions.Center,
                        Content = new Label
                        {
                            Text = iconText,
                            TextColor = accentColor,
                            FontSize = 22,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center
                        }
                    },
                    new Label
                    {
                        Text = options.Title,
                        TextColor = Color.FromArgb("#102A43"),
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = options.Message,
                        TextColor = Color.FromArgb("#486581"),
                        FontSize = 16,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    closeButton
                }
            }
        };

        overlay.Children.Add(backdrop);
        overlay.Children.Add(messageCard);

        return overlay;
    }

    private static Page? UnwrapPage(Page? page)
    {
        return page switch
        {
            Microsoft.Maui.Controls.Shell shell => shell.CurrentPage,
            NavigationPage navigationPage => navigationPage.CurrentPage,
            TabbedPage tabbedPage => tabbedPage.CurrentPage,
            FlyoutPage flyoutPage => UnwrapPage(flyoutPage.Detail),
            _ => page
        };
    }
}
