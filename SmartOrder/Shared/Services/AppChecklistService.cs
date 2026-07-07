using Microsoft.Maui.Controls.Shapes;

namespace SmartOrder.Shared.Services;

public sealed class ChecklistOption
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsSelected { get; set; }
}

public static class AppChecklistService
{
    public static async Task<IReadOnlyList<int>?> ShowAsync(
        string title,
        IEnumerable<ChecklistOption> options,
        string accept = "Aplicar",
        string cancel = "Cancelar")
    {
        var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage == null)
        {
            return null;
        }

        var result = new TaskCompletionSource<IReadOnlyList<int>?>();
        var optionList = options.ToList();

        async Task CloseAsync(IReadOnlyList<int>? selectedIds)
        {
            if (!result.Task.IsCompleted)
            {
                result.SetResult(selectedIds);
            }

            if (currentPage.Navigation.ModalStack.LastOrDefault() is ContentPage modal)
            {
                await currentPage.Navigation.PopModalAsync();
            }
        }

        var list = new VerticalStackLayout
        {
            Spacing = 4
        };

        foreach (var option in optionList)
        {
            var checkBox = new CheckBox
            {
                IsChecked = option.IsSelected,
                Color = Color.FromArgb("#0B6E4F"),
                VerticalOptions = LayoutOptions.Center,
                InputTransparent = true
            };
            checkBox.CheckedChanged += (_, args) => option.IsSelected = args.Value;

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10,
                Padding = new Thickness(4, 6)
            };
            row.Add(checkBox, 0, 0);
            row.Add(new Label
            {
                Text = option.Text,
                TextColor = Color.FromArgb("#102A43"),
                FontSize = 15,
                VerticalTextAlignment = TextAlignment.Center
            }, 1, 0);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => checkBox.IsChecked = !checkBox.IsChecked;
            row.GestureRecognizers.Add(tap);

            list.Children.Add(row);
        }

        var modalPage = new ContentPage
        {
            BackgroundColor = Color.FromArgb("#66000000"),
            Padding = new Thickness(24),
            Content = new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#B7DBC9"),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(22) },
                Padding = new Thickness(20),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                MaximumWidthRequest = 520,
                Content = new Grid
                {
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    RowSpacing = 16,
                    Children =
                    {
                        new Label
                        {
                            Text = title,
                            TextColor = Color.FromArgb("#12344D"),
                            FontSize = 22,
                            FontAttributes = FontAttributes.Bold
                        },
                        new ScrollView
                        {
                            MaximumHeightRequest = 360,
                            Content = list
                        }.Row(1),
                        new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = GridLength.Star },
                                new ColumnDefinition { Width = GridLength.Star }
                            },
                            ColumnSpacing = 10,
                            Children =
                            {
                                new Button
                                {
                                    Text = cancel,
                                    BackgroundColor = Color.FromArgb("#FDECEC"),
                                    TextColor = Color.FromArgb("#B42318"),
                                    CornerRadius = 12,
                                    HeightRequest = 46
                                }.Column(0).BindClick(async () => await CloseAsync(null)),
                                new Button
                                {
                                    Text = accept,
                                    BackgroundColor = Color.FromArgb("#0B6E4F"),
                                    TextColor = Colors.White,
                                    CornerRadius = 12,
                                    HeightRequest = 46
                                }.Column(1).BindClick(async () =>
                                    await CloseAsync(optionList.Where(option => option.IsSelected).Select(option => option.Id).ToList()))
                            }
                        }.Row(2)
                    }
                }
            }
        };

        await currentPage.Navigation.PushModalAsync(modalPage);
        return await result.Task;
    }

    private static T Row<T>(this T view, int row) where T : BindableObject
    {
        Grid.SetRow(view, row);
        return view;
    }

    private static T Column<T>(this T view, int column) where T : BindableObject
    {
        Grid.SetColumn(view, column);
        return view;
    }

    private static T BindClick<T>(this T button, Func<Task> action) where T : Button
    {
        button.Clicked += async (_, _) => await action();
        return button;
    }
}
