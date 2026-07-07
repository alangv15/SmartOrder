using Microsoft.Maui.Controls.Shapes;

namespace SmartOrder.Modules.Orders.Pages
{
    public sealed record OrderEditSelectionItem(
        int Folio,
        string CustomerName,
        int Products,
        string DeliveryDateText);

    public sealed class OrderEditSelectionPage : ContentPage
    {
        private readonly TaskCompletionSource<int?> _completion = new();
        private readonly Entry _folioEntry;
        private readonly Label _validationLabel;
        private bool _isClosing;

        private OrderEditSelectionPage(IReadOnlyList<OrderEditSelectionItem> orders)
        {
            BackgroundColor = Color.FromArgb("#800B1220");
            Padding = new Thickness(24);

            _folioEntry = new Entry
            {
                Placeholder = "Folio de pedido",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#16213A"),
                PlaceholderColor = Color.FromArgb("#7A8CA5"),
                HeightRequest = 46,
                Margin = new Thickness(0)
            };
            _folioEntry.Completed += async (_, _) => await AcceptTypedFolioAsync();

            _validationLabel = new Label
            {
                Text = "Captura un folio o selecciona un pedido vigente de la tabla.",
                TextColor = Color.FromArgb("#6B7A90"),
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            Content = new Border
            {
                BackgroundColor = Color.FromArgb("#F7FAFC"),
                Stroke = Color.FromArgb("#D9E5F2"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24) },
                MaximumWidthRequest = 760,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(22),
                Content = BuildContent(orders)
            };
        }

        public static async Task<int?> ShowAsync(IReadOnlyList<OrderEditSelectionItem> orders)
        {
            var page = new OrderEditSelectionPage(orders);
            var navigation = Microsoft.Maui.Controls.Shell.Current?.Navigation
                ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;

            if (navigation == null)
            {
                return null;
            }

            try
            {
                await navigation.PushModalAsync(page);
                return await page._completion.Task;
            }
            catch
            {
                page._completion.TrySetResult(null);
                return null;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CloseAsync(null);
            return true;
        }

        private View BuildContent(IReadOnlyList<OrderEditSelectionItem> orders)
        {
            var title = new Label
            {
                Text = "Editar pedido",
                FontAttributes = FontAttributes.Bold,
                FontSize = 24,
                TextColor = Color.FromArgb("#102A43")
            };

            var subtitle = new Label
            {
                Text = "Abre un pedido por folio o elige uno vigente.",
                FontSize = 13,
                TextColor = Color.FromArgb("#486581")
            };

            var openButton = new Button
            {
                Text = "Abrir folio",
                BackgroundColor = Color.FromArgb("#1B587C"),
                TextColor = Colors.White,
                CornerRadius = 14,
                HeightRequest = 46
            };
            openButton.Clicked += async (_, _) => await AcceptTypedFolioAsync();

            var cancelButton = new Button
            {
                Text = "Cancelar",
                BackgroundColor = Color.FromArgb("#E6EDF5"),
                TextColor = Color.FromArgb("#1B365D"),
                CornerRadius = 14,
                HeightRequest = 46
            };
            cancelButton.Clicked += async (_, _) => await CloseAsync(null);

            var folioGrid = new Grid
            {
                ColumnSpacing = 10,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            folioGrid.Add(_folioEntry, 0, 0);
            folioGrid.Add(openButton, 1, 0);
            folioGrid.Add(cancelButton, 2, 0);

            var table = BuildOrderTable(orders);

            return new VerticalStackLayout
            {
                Spacing = 16,
                Children =
                {
                    title,
                    subtitle,
                    folioGrid,
                    _validationLabel,
                    table
                }
            };
        }

        private View BuildOrderTable(IReadOnlyList<OrderEditSelectionItem> orders)
        {
            if (orders.Count == 0)
            {
                return new Border
                {
                    BackgroundColor = Color.FromArgb("#EEF6FF"),
                    Stroke = Color.FromArgb("#C9DDF2"),
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
                    Padding = new Thickness(16),
                    Content = new Label
                    {
                        Text = "No hay pedidos vigentes para mostrar.",
                        TextColor = Color.FromArgb("#1B587C"),
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                };
            }

            var collection = new CollectionView
            {
                ItemsSource = orders,
                SelectionMode = SelectionMode.None,
                HeightRequest = 340,
                ItemTemplate = new DataTemplate(() => CreateOrderRow())
            };

            return new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#D9E5F2"),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
                Padding = new Thickness(0),
                Content = new VerticalStackLayout
                {
                    Spacing = 0,
                    Children =
                    {
                        CreateHeaderRow(),
                        collection
                    }
                }
            };
        }

        private static Grid CreateHeaderRow()
        {
            var row = CreateTableGrid(Color.FromArgb("#1B587C"), new Thickness(12, 10));
            row.Add(CreateHeaderLabel("Folio"), 0, 0);
            row.Add(CreateHeaderLabel("Cliente"), 1, 0);
            row.Add(CreateHeaderLabel("Productos"), 2, 0);
            row.Add(CreateHeaderLabel("Entrega"), 3, 0);
            return row;
        }

        private View CreateOrderRow()
        {
            var row = CreateTableGrid(Colors.White, new Thickness(12, 9));

            var folio = CreateBodyLabel(isBold: true);
            folio.SetBinding(Label.TextProperty, nameof(OrderEditSelectionItem.Folio));

            var customer = CreateBodyLabel();
            customer.SetBinding(Label.TextProperty, nameof(OrderEditSelectionItem.CustomerName));

            var products = CreateBodyLabel(horizontalAlignment: TextAlignment.Center);
            products.SetBinding(Label.TextProperty, nameof(OrderEditSelectionItem.Products));

            var delivery = CreateBodyLabel(horizontalAlignment: TextAlignment.Center);
            delivery.SetBinding(Label.TextProperty, nameof(OrderEditSelectionItem.DeliveryDateText));

            row.Add(folio, 0, 0);
            row.Add(customer, 1, 0);
            row.Add(products, 2, 0);
            row.Add(delivery, 3, 0);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (_, _) =>
            {
                if (row.BindingContext is OrderEditSelectionItem item)
                {
                    await CloseAsync(item.Folio);
                }
            };
            row.GestureRecognizers.Add(tapGesture);

            return row;
        }

        private static Grid CreateTableGrid(Color backgroundColor, Thickness padding)
        {
            return new Grid
            {
                BackgroundColor = backgroundColor,
                Padding = padding,
                ColumnSpacing = 10,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2.1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }
                }
            };
        }

        private static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                VerticalTextAlignment = TextAlignment.Center
            };
        }

        private static Label CreateBodyLabel(bool isBold = false, TextAlignment horizontalAlignment = TextAlignment.Start)
        {
            return new Label
            {
                TextColor = Color.FromArgb("#102A43"),
                FontSize = 13,
                FontAttributes = isBold ? FontAttributes.Bold : FontAttributes.None,
                HorizontalTextAlignment = horizontalAlignment,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };
        }

        private async Task AcceptTypedFolioAsync()
        {
            if (!int.TryParse(_folioEntry.Text?.Trim(), out var folio) || folio <= 0)
            {
                _validationLabel.Text = "Captura un folio numerico valido.";
                _validationLabel.TextColor = Color.FromArgb("#B42318");
                return;
            }

            await CloseAsync(folio);
        }

        private async Task CloseAsync(int? folio)
        {
            if (_isClosing)
            {
                return;
            }

            _isClosing = true;
            try
            {
                if (Navigation.ModalStack.LastOrDefault() == this)
                {
                    await Navigation.PopModalAsync();
                }

                _completion.TrySetResult(folio);
            }
            catch
            {
                _completion.TrySetResult(null);
            }
        }
    }
}
