using Microsoft.Maui.Controls.Shapes;
using SmartOrder.Shared.Services;

namespace SmartOrder.Shared.Components;

public class AppMessagePage : ContentPage
{
    private readonly TaskCompletionSource<bool> _dismissed = new();
    private readonly INavigation _modalNavigation;

    public AppMessagePage(AppMessageOptions options, INavigation modalNavigation)
    {
        _modalNavigation = modalNavigation;
        BackgroundColor = Color.FromArgb("#66000000");
        Padding = new Thickness(24);

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

        var card = new Border
        {
            Stroke = accentColor,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24) },
            BackgroundColor = Colors.White,
            Padding = new Thickness(24),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 520,
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
                    CreateCloseButton(options.ButtonText, accentColor)
                }
            }
        };

        Content = new Grid
        {
            Children =
            {
                new BoxView { Color = Colors.Transparent },
                card
            }
        };
    }

    public Task WaitForDismissAsync() => _dismissed.Task;

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    private Button CreateCloseButton(string buttonText, Color accentColor)
    {
        var button = new Button
        {
            Text = buttonText,
            BackgroundColor = accentColor,
            TextColor = Colors.White,
            CornerRadius = 14,
            HeightRequest = 50,
            FontAttributes = FontAttributes.Bold
        };

        button.Clicked += async (_, _) => await CloseAsync();
        return button;
    }

    private async Task CloseAsync()
    {
        if (_dismissed.Task.IsCompleted)
        {
            return;
        }

        _dismissed.TrySetResult(true);

        if (_modalNavigation.ModalStack.LastOrDefault() == this)
        {
            await _modalNavigation.PopModalAsync();
        }
    }
}

