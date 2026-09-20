using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Orders.Models;
using SmartOrder.Modules.Sales.Services;
using SmartOrder.Shared.Catalogs;
using SmartOrder.Shared.Models;
using SmartOrder.Shared.Printing;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

namespace SmartOrder.Modules.Sales.ViewModels
{
    public class SaleFormViewModel : INotifyPropertyChanged
    {
        private const int DirectSaleBranchId = 1;

        private readonly int _defaultUserId;
        private readonly OrderService _orderService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly BranchService _branchService;
        private readonly DiscountRuleService _discountRuleService;
        private readonly DiscountLimitRuleService _discountLimitRuleService;
        private readonly ITicketPrinterService _ticketPrinterService;
        private readonly Command _saveCommand;
        private readonly Command _addProductCommand;
        private readonly Command _loadSaleForEditCommand;
        private readonly Command _cancelEditCommand;
        private readonly Dictionary<int, string> _categoryNamesById = new();

        private int? _editingOrderId;
        private DateTime _editingCreatedAtUtc;
        private string _paymentMethod = string.Empty;
        private LookupOption? _selectedCustomerGender;
        private LookupOption? _selectedCustomerAgeRange;
        private LookupOption? _selectedCustomerType;
        private LookupOption? _selectedAcquisitionChannel;
        private readonly HashSet<int> _selectedDiscountIds = new();
        private string _cashReceivedAmountText = string.Empty;
        private ProductDto? _selectedProduct;
        private string _productSearchText = string.Empty;
        private CategoryDto? _selectedCategory;
        private bool _isSaving;
        private bool _isLoadingCatalogData;
        private bool _skipNextCatalogRefreshOnAppearing;

        public SaleFormViewModel(
            int defaultUserId,
            OrderService orderService,
            ProductService productService,
            CategoryService categoryService,
            BranchService branchService,
            DiscountRuleService discountRuleService,
            DiscountLimitRuleService discountLimitRuleService,
            ITicketPrinterService ticketPrinterService)
        {
            _defaultUserId = defaultUserId;
            _orderService = orderService;
            _productService = productService;
            _categoryService = categoryService;
            _branchService = branchService;
            _discountRuleService = discountRuleService;
            _discountLimitRuleService = discountLimitRuleService;
            _ticketPrinterService = ticketPrinterService;

            IncreaseProduct = new Command<SaleItemViewModel>(item =>
            {
                if (item is null)
                {
                    return;
                }

                item.Quantity++;
                RefreshTotals();
            });

            SearchProductCommand = new Command(FilterProducts);
            SelectDiscountsCommand = new Command(async () => await SelectDiscountsAsync());

            _addProductCommand = new Command(AddSelectedProduct, () => CanAddProduct);
            AddProductCommand = _addProductCommand;

            _loadSaleForEditCommand = new Command(async () => await LoadSaleForEditAsync(), () => CanLoadSaleForEdit);
            LoadSaleForEditCommand = _loadSaleForEditCommand;

            _cancelEditCommand = new Command(CancelEdit, () => CanCancelEdit);
            CancelEditCommand = _cancelEditCommand;

            _saveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
            SaveCommand = _saveCommand;
            ExportSalePdfCommand = new Command(async () => await ExportSalePdfAsync());
            PrintSaleTicketCommand = new Command(async () => await PrintSaleTicketAsync());

            SaleItems.CollectionChanged += SaleItems_CollectionChanged;

            LoadCustomerOptions();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime SaleDate { get; set; } = DateTime.Now;

        public ObservableCollection<LookupOption> CustomerGenderOptions { get; } = new();
        public ObservableCollection<LookupOption> CustomerAgeRangeOptions { get; } = new();
        public ObservableCollection<LookupOption> CustomerTypeOptions { get; } = new();
        public ObservableCollection<LookupOption> AcquisitionChannelOptions { get; } = new();
        public ObservableCollection<DiscountRuleDto> Discounts { get; } = new();
        public ObservableCollection<DiscountLimitRuleDto> DiscountLimits { get; } = new();
        public ObservableCollection<CategoryDto> Categories { get; } = new();
        public ObservableCollection<ProductDto> Products { get; } = new();
        public ObservableCollection<ProductDto> FilteredProducts { get; } = new();
        public ObservableCollection<SaleItemViewModel> SaleItems { get; } = new();

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    OnPropertyChanged(nameof(PaymentMethod));
                    OnPropertyChanged(nameof(IsCashPayment));
                    OnPropertyChanged(nameof(IsCardPayment));
                    OnPropertyChanged(nameof(IsCashPaymentSelected));
                    RefreshCashChange();

                    if (!IsCashPaymentSelected)
                    {
                        CashReceivedAmountText = string.Empty;
                    }
                }
            }
        }

        public bool IsCashPayment
        {
            get => PaymentMethod == OrderCatalog.CashPaymentMethodCode;
            set
            {
                if (value)
                {
                    PaymentMethod = OrderCatalog.CashPaymentMethodCode;
                }
            }
        }

        public bool IsCardPayment
        {
            get => PaymentMethod == OrderCatalog.CardPaymentMethodCode;
            set
            {
                if (value)
                {
                    PaymentMethod = OrderCatalog.CardPaymentMethodCode;
                }
            }
        }

        public LookupOption? SelectedCustomerGender
        {
            get => _selectedCustomerGender;
            set
            {
                if (_selectedCustomerGender != value)
                {
                    _selectedCustomerGender = value;
                    OnPropertyChanged(nameof(SelectedCustomerGender));
                }
            }
        }

        public LookupOption? SelectedCustomerAgeRange
        {
            get => _selectedCustomerAgeRange;
            set
            {
                if (_selectedCustomerAgeRange != value)
                {
                    _selectedCustomerAgeRange = value;
                    OnPropertyChanged(nameof(SelectedCustomerAgeRange));
                }
            }
        }

        public LookupOption? SelectedCustomerType
        {
            get => _selectedCustomerType;
            set
            {
                if (_selectedCustomerType != value)
                {
                    _selectedCustomerType = value;
                    OnPropertyChanged(nameof(SelectedCustomerType));
                    OnPropertyChanged(nameof(IsAcquisitionChannelEnabled));

                    if (!IsAcquisitionChannelEnabled)
                    {
                        SelectedAcquisitionChannel = null;
                    }
                }
            }
        }

        public LookupOption? SelectedAcquisitionChannel
        {
            get => _selectedAcquisitionChannel;
            set
            {
                if (_selectedAcquisitionChannel != value)
                {
                    _selectedAcquisitionChannel = value;
                    OnPropertyChanged(nameof(SelectedAcquisitionChannel));
                }
            }
        }

        public bool IsAcquisitionChannelEnabled => SelectedCustomerType?.Code == OrderCatalog.NewCustomerTypeCode;
        public bool IsCashPaymentSelected => PaymentMethod == OrderCatalog.CashPaymentMethodCode;

        public string CashReceivedAmountText
        {
            get => _cashReceivedAmountText;
            set
            {
                if (_cashReceivedAmountText != value)
                {
                    _cashReceivedAmountText = value;
                    OnPropertyChanged(nameof(CashReceivedAmountText));
                    RefreshCashChange();
                }
            }
        }

        public decimal? CashReceivedAmount => TryParseCashReceivedAmount(out var amount) ? amount : null;
        public decimal CashChangeAmount => IsCashPaymentSelected && CashReceivedAmount.HasValue
            ? Math.Max(0, CashReceivedAmount.Value - Total)
            : 0;
        public string CashChangeAmountDisplay => IsCashPaymentSelected && CashReceivedAmount.HasValue
            ? CashChangeAmount.ToString("C2", CultureInfo.CurrentCulture)
            : "$0.00";

        public string DiscountSelectionSummary => GetSelectedDiscounts().Any()
            ? string.Join(", ", GetSelectedDiscounts().Select(discount => discount.Name))
            : "Sin descuento";

        public ProductDto? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    OnPropertyChanged(nameof(SelectedProduct));
                    OnPropertyChanged(nameof(CanAddProduct));
                    _addProductCommand.ChangeCanExecute();
                }
            }
        }

        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (_productSearchText != value)
                {
                    _productSearchText = value;
                    OnPropertyChanged(nameof(ProductSearchText));
                    FilterProducts();
                }
            }
        }

        public CategoryDto? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged(nameof(SelectedCategory));
                    FilterProducts();
                }
            }
        }

        public int TotalProductQuantity => SaleItems.Sum(item => item.Quantity);
        public decimal Subtotal => SaleItems.Sum(item => item.Subtotal);
        public decimal DiscountAmount => SaleItems.Sum(item => item.DiscountAmount);
        public decimal Total => Subtotal - DiscountAmount;
        public bool HasSaleItems => SaleItems.Any();
        public bool IsSaleItemsEmpty => !HasSaleItems;
        public bool CanAddProduct => SelectedProduct != null && !IsSaving && !IsLoadingCatalogData;
        public bool CanLoadSaleForEdit => !IsSaving && !IsLoadingCatalogData;
        public bool CanCancelEdit => IsEditingSale && !IsSaving;
        public bool TryConsumeCatalogRefreshSkip()
        {
            if (_skipNextCatalogRefreshOnAppearing)
            {
                _skipNextCatalogRefreshOnAppearing = false;
                return true;
            }

            return false;
        }
        public bool IsEditingSale => _editingOrderId.HasValue;
        public int? EditingOrderId => _editingOrderId;
        public bool ShowCancelEditButton => IsEditingSale;
        public string SaleHeaderTitle => IsEditingSale ? "Editar venta" : "Nueva venta";

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged(nameof(IsSaving));
                    OnPropertyChanged(nameof(IsNotSaving));
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(CanAddProduct));
                    OnPropertyChanged(nameof(CanLoadSaleForEdit));
                    OnPropertyChanged(nameof(CanCancelEdit));
                    OnPropertyChanged(nameof(ShowCancelEditButton));
                    _addProductCommand.ChangeCanExecute();
                    _loadSaleForEditCommand.ChangeCanExecute();
                    _cancelEditCommand.ChangeCanExecute();
                    _saveCommand.ChangeCanExecute();
                }
            }
        }

        public bool IsLoadingCatalogData
        {
            get => _isLoadingCatalogData;
            private set
            {
                if (_isLoadingCatalogData != value)
                {
                    _isLoadingCatalogData = value;
                    OnPropertyChanged(nameof(IsLoadingCatalogData));
                    OnPropertyChanged(nameof(CanAddProduct));
                    OnPropertyChanged(nameof(CanLoadSaleForEdit));
                    _addProductCommand.ChangeCanExecute();
                    _loadSaleForEditCommand.ChangeCanExecute();
                }
            }
        }

        public bool IsNotSaving => !IsSaving;
        public string SaveButtonText => IsSaving ? "Guardando..." : IsEditingSale ? "Guardar cambios" : "Confirmar venta";

        public ICommand SearchProductCommand { get; }
        public ICommand SelectDiscountsCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand LoadSaleForEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExportSalePdfCommand { get; }
        public ICommand PrintSaleTicketCommand { get; }
        public ICommand IncreaseProduct { get; }

        public async Task RefreshCatalogDataAsync()
        {
            if (IsLoadingCatalogData)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            IsLoadingCatalogData = true;
            try
            {
                var selectedCategoryId = SelectedCategory?.CategoryId;
                var selectedProductId = SelectedProduct?.ProductId;
                var selectedDiscountIds = _selectedDiscountIds.ToHashSet();

                Discounts.Clear();
                DiscountLimits.Clear();
                Categories.Clear();
                Products.Clear();
                FilteredProducts.Clear();
                _categoryNamesById.Clear();

                var discountsTask = _discountRuleService.GetAllAsync();
                var discountLimitsTask = _discountLimitRuleService.GetAllAsync();
                var productsTask = _productService.GetAllAsync();
                var categoriesTask = _categoryService.GetAllAsync();

                await Task.WhenAll(discountsTask, discountLimitsTask, productsTask, categoriesTask);

                var discounts = await discountsTask;
                foreach (var discount in discounts)
                {
                    Discounts.Add(discount);
                }

                foreach (var discountLimit in await discountLimitsTask)
                {
                    DiscountLimits.Add(discountLimit);
                }

                _selectedDiscountIds.Clear();
                foreach (var discountId in selectedDiscountIds.Where(id => Discounts.Any(discount => discount.DiscountRuleId == id)))
                {
                    _selectedDiscountIds.Add(discountId);
                }
                OnPropertyChanged(nameof(DiscountSelectionSummary));

                var products = (await productsTask)
                    .Where(product => product.IsActive && product.IsDirectSale)
                    .OrderBy(product => product.Name)
                    .ToList();

                foreach (var product in products)
                {
                    Products.Add(product);
                }

                var categories = (await categoriesTask)
                    .Where(category => category.IsActive && category.IsDirectSale)
                    .OrderBy(category => category.DisplayOrder)
                    .ThenBy(category => category.Name)
                    .ToList();

                foreach (var category in categories)
                {
                    Categories.Add(category);
                    _categoryNamesById[category.CategoryId] = category.Name;
                }

                SelectedCategory = Categories.FirstOrDefault(category => category.CategoryId == selectedCategoryId)
                    ?? Categories.FirstOrDefault();
                SelectedProduct = FilteredProducts.FirstOrDefault(product => product.ProductId == selectedProductId)
                    ?? FilteredProducts.FirstOrDefault();

                foreach (var item in SaleItems)
                {
                    var product = Products.FirstOrDefault(currentProduct => currentProduct.ProductId == item.ProductId);
                    if (product != null)
                    {
                        item.CategoryId = product.CategoryId;
                        item.CategoryName = ResolveCategoryName(product.CategoryId);
                        item.ProductName = product.Name;
                        item.Price = product.SalePrice;
                    }
                }

                RefreshItemDiscounts();
                RefreshSaleState();

                LogSlowOperation("SaleFormViewModel.RefreshCatalogDataAsync", stopwatch);
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SaleFormViewModel.RefreshCatalogDataAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo cargar la pantalla de ventas",
                    Message = "No fue posible cargar productos, categorias o descuentos. Revisa la conexión con el API."
                });
            }
            finally
            {
                IsLoadingCatalogData = false;
            }
        }

        private async Task ExportSalePdfAsync()
        {
            if (!SaleItems.Any())
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Venta vacia",
                    Message = "Agrega al menos un producto antes de exportar la nota de venta."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(PaymentMethod))
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Metodo de pago requerido",
                    Message = "Selecciona efectivo o tarjeta antes de exportar la nota de venta."
                });
                return;
            }

            try
            {
                RefreshItemDiscounts();
                var branch = await _branchService.GetByIdAsync(DirectSaleBranchId);
                var filePath = await SaleReceiptPdfExporter.ExportAsync(new SaleReceiptPdfRequest(
                    _editingOrderId,
                    branch?.Name ?? "SmartOrder",
                    BuildBranchAddress(branch),
                    branch?.Phone,
                    SaleDate,
                    OrderCatalog.GetPaymentMethodLabel(PaymentMethod),
                    SaleItems.ToList(),
                    Subtotal,
                    DiscountAmount,
                    Total));

                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(new Microsoft.Maui.ApplicationModel.OpenFileRequest
                {
                    File = new Microsoft.Maui.Storage.ReadOnlyFile(filePath)
                });

                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "PDF generado",
                    Message = "La nota de venta se genero correctamente en la carpeta Documentos\\SmartOrder\\Sales."
                });
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SaleFormViewModel.ExportSalePdfAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo exportar",
                    Message = "Ocurrio un error al generar el PDF de la venta. Revisa el archivo smartorder-errors.log."
                });
            }
        }

        private async Task PrintSaleTicketAsync()
        {
            if (!SaleItems.Any())
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Venta vacia",
                    Message = "Agrega al menos un producto antes de imprimir el ticket."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(PaymentMethod))
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Metodo de pago requerido",
                    Message = "Selecciona efectivo o tarjeta antes de imprimir el ticket."
                });
                return;
            }

            try
            {
                RefreshItemDiscounts();
                var branch = await _branchService.GetByIdAsync(DirectSaleBranchId);
                var result = await _ticketPrinterService.PrintAsync(BuildSaleTicketDocument(branch));

                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = result.Success ? AppMessageType.Success : AppMessageType.Info,
                    Title = result.Title,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SaleFormViewModel.PrintSaleTicketAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo imprimir",
                    Message = "Ocurrio un error al preparar el ticket de venta. Revisa el archivo smartorder-errors.log."
                });
            }
        }

        private TicketDocument BuildSaleTicketDocument(BranchDto? branch, int? folio = null)
        {
            var branchAddress = BuildBranchAddress(branch);
            var includeDiscounts = DiscountAmount > 0;
            var totals = new List<TicketTotalLine>
            {
                new("Subtotal", Subtotal)
            };

            if (includeDiscounts)
            {
                totals.Add(new TicketTotalLine("Descuento", DiscountAmount));
            }

            totals.Add(new TicketTotalLine("Total", Total, IsGrandTotal: true));

            if (IsCashPaymentSelected && CashReceivedAmount.HasValue)
            {
                totals.Add(new TicketTotalLine("Recibido", CashReceivedAmount.Value));
                totals.Add(new TicketTotalLine("Cambio", CashChangeAmount));
            }

            return new TicketDocument(
                branch?.Name ?? "SmartOrder",
                new[]
                {
                    branchAddress,
                    string.IsNullOrWhiteSpace(branch?.Phone) ? null : $"WhatsApp {branch.Phone}"
                }.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line!).ToList(),
                new[]
                {
                    new TicketInfoLine("Folio", (folio ?? _editingOrderId)?.ToString() ?? "Borrador"),
                    new TicketInfoLine("Fecha", SaleDate.ToString("dd/MM/yyyy HH:mm")),
                    new TicketInfoLine("Metodo de pago", OrderCatalog.GetPaymentMethodLabel(PaymentMethod)),
                    new TicketInfoLine("Piezas", TotalProductQuantity.ToString())
                },
                SaleItems.Select(item => new TicketItemLine(
                    item.CategoryName,
                    item.ProductName,
                    item.Quantity,
                    item.Price,
                    item.DiscountAmount,
                    item.Total)).ToList(),
                includeDiscounts,
                totals,
                new[] { "Gracias por su compra" });
        }

        private void FilterProducts()
        {
            FilteredProducts.Clear();

            var query = ProductSearchText?.Trim() ?? string.Empty;
            var categoryId = SelectedCategory?.CategoryId;

            var filtered = Products.Where(product =>
                (string.IsNullOrWhiteSpace(query) || product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
                (categoryId == null || product.CategoryId == categoryId));

            foreach (var product in filtered)
            {
                FilteredProducts.Add(product);
            }

            SelectedProduct = FilteredProducts.FirstOrDefault();
        }

        private void SaleItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SaleItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= SaleItem_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (SaleItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += SaleItem_PropertyChanged;
                }
            }

            RefreshItemDiscounts();
            RefreshSaleState();
        }

        private void SaleItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SaleItemViewModel.Quantity))
            {
                RefreshItemDiscounts();
                RefreshTotals();
                return;
            }

            if (e.PropertyName == nameof(SaleItemViewModel.Subtotal) ||
                e.PropertyName == nameof(SaleItemViewModel.DiscountAmount))
            {
                RefreshTotals();
            }
        }

        private void AddSelectedProduct()
        {
            if (!CanAddProduct || SelectedProduct is null)
            {
                return;
            }

            var existingItem = SaleItems.FirstOrDefault(item => item.ProductId == SelectedProduct.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
                MoveSaleItemToTop(existingItem);
                return;
            }

            SaleItems.Insert(0, new SaleItemViewModel(RemoveItem)
            {
                ProductId = SelectedProduct.ProductId,
                CategoryId = SelectedProduct.CategoryId,
                CategoryName = ResolveCategoryName(SelectedProduct.CategoryId),
                ProductName = SelectedProduct.Name,
                Price = ResolveCurrentSalePrice(SelectedProduct),
                UnitCost = ResolveCurrentUnitCost(SelectedProduct),
                ProductRecipeId = SelectedProduct.CurrentProductRecipeId,
                ProductPriceId = SelectedProduct.CurrentProductPriceId,
                CostCalculatedAt = DateTime.UtcNow,
                Quantity = 1
            });
            RefreshItemDiscounts();
        }

        private async Task LoadSaleForEditAsync()
        {
            if (!CanLoadSaleForEdit)
            {
                return;
            }

            var folioText = await AppPromptService.PromptAsync(new AppPromptOptions
            {
                Title = "Editar venta",
                Message = "Captura el folio de venta para editar.",
                Accept = "Aceptar",
                Cancel = "Cancelar",
                Placeholder = "Folio de venta",
                Keyboard = Keyboard.Numeric
            });

            if (folioText == null)
            {
                return;
            }

            if (!int.TryParse(folioText.Trim(), out var folio) || folio <= 0)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Folio invalido",
                    Message = "Captura un folio numerico valido para cargar la venta."
                });
                return;
            }

            try
            {
                var order = await _orderService.GetInStoreSaleByIdAsync(folio);
                if (order == null)
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "Venta no encontrada",
                        Message = $"No se encontro una venta de mostrador con el folio #{folio}."
                    });
                    return;
                }

                var missingProducts = order.OrderItems
                    .Where(orderItem => Products.All(product => product.ProductId != orderItem.ProductId))
                    .Select(orderItem => orderItem.ProductId)
                    .Distinct()
                    .ToList();

                if (missingProducts.Any())
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "No se puede editar la venta",
                        Message = "Hay productos del folio seleccionado que ya no estan disponibles en el catalogo de venta directa."
                    });
                    return;
                }

                ApplyOrderToForm(order);

                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Info,
                    Title = "Venta cargada",
                    Message = $"Ahora estas editando la venta con folio #{folio}."
                });
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SaleFormViewModel.LoadSaleForEditAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo cargar la venta",
                    Message = "Ocurrio un error al intentar cargar el folio solicitado para edicion."
                });
            }
        }

        private void ApplyOrderToForm(OrderDto order)
        {
            SaleItems.Clear();
            ProductSearchText = string.Empty;

            SetEditingOrder(order.OrderId, NormalizeStoredUtc(order.CreatedAt));
            SaleDate = ToLocalDisplayDateTime(_editingCreatedAtUtc);
            OnPropertyChanged(nameof(SaleDate));

            PaymentMethod = order.PaymentMethod;
            CashReceivedAmountText = order.CashReceivedAmount.HasValue
                ? order.CashReceivedAmount.Value.ToString("0.##", CultureInfo.CurrentCulture)
                : string.Empty;
            SelectedCustomerGender = CustomerGenderOptions.FirstOrDefault(option => option.Code == order.CustomerGender);
            SelectedCustomerAgeRange = CustomerAgeRangeOptions.FirstOrDefault(option => option.Code == order.CustomerAgeRange);
            SelectedCustomerType = CustomerTypeOptions.FirstOrDefault(option => option.Code == order.CustomerType);
            SelectedAcquisitionChannel = AcquisitionChannelOptions.FirstOrDefault(option => option.Code == order.AcquisitionChannel);

            _selectedDiscountIds.Clear();
            foreach (var discountId in order.OrderDiscounts.Select(discount => discount.DiscountRuleId).Distinct())
            {
                if (Discounts.Any(discount => discount.DiscountRuleId == discountId))
                {
                    _selectedDiscountIds.Add(discountId);
                }
            }
            OnPropertyChanged(nameof(DiscountSelectionSummary));

            var orderProducts = order.OrderItems
                .Select(orderItem => new
                {
                    OrderItem = orderItem,
                    Product = Products.First(product => product.ProductId == orderItem.ProductId)
                })
                .OrderBy(item => ResolveCategoryName(item.Product.CategoryId))
                .ThenBy(item => item.Product.Name)
                .ToList();

            var firstCategoryId = orderProducts.FirstOrDefault()?.Product.CategoryId;
            SelectedCategory = Categories.FirstOrDefault(category => category.CategoryId == firstCategoryId) ?? Categories.FirstOrDefault();

            foreach (var orderProduct in orderProducts)
            {
                SaleItems.Add(new SaleItemViewModel(RemoveItem)
                {
                    ProductId = orderProduct.OrderItem.ProductId,
                    CategoryId = orderProduct.Product.CategoryId,
                    CategoryName = ResolveCategoryName(orderProduct.Product.CategoryId),
                    ProductName = orderProduct.Product.Name,
                    Price = orderProduct.OrderItem.UnitPrice,
                    UnitCost = orderProduct.OrderItem.UnitCost,
                    ProductRecipeId = orderProduct.OrderItem.ProductRecipeId,
                    ProductPriceId = orderProduct.OrderItem.ProductPriceId,
                    CostCalculatedAt = orderProduct.OrderItem.CostCalculatedAt,
                    DiscountPerUnit = orderProduct.OrderItem.DiscountPerUnit ?? 0,
                    Quantity = orderProduct.OrderItem.Quantity
                });
            }

            RefreshSaleState();
        }

        private void MoveSaleItemToTop(SaleItemViewModel item)
        {
            var currentIndex = SaleItems.IndexOf(item);
            if (currentIndex > 0)
            {
                SaleItems.Move(currentIndex, 0);
            }
        }

        private string ResolveCategoryName(int categoryId)
        {
            if (_categoryNamesById.TryGetValue(categoryId, out var categoryName))
            {
                return categoryName;
            }

            return SelectedCategory?.Name ?? string.Empty;
        }

        private void RemoveItem(SaleItemViewModel item)
        {
            if (SaleItems.Contains(item))
            {
                SaleItems.Remove(item);
                RefreshSaleState();
            }
        }

        private void RefreshSaleState()
        {
            OnPropertyChanged(nameof(HasSaleItems));
            OnPropertyChanged(nameof(IsSaleItemsEmpty));
            RefreshTotals();
        }

        private void CancelEdit()
        {
            if (!CanCancelEdit)
            {
                return;
            }

            ResetForm();
        }

        private void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalProductQuantity));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(Total));
            RefreshCashChange();
        }

        private void RefreshCashChange()
        {
            OnPropertyChanged(nameof(CashReceivedAmount));
            OnPropertyChanged(nameof(CashChangeAmount));
            OnPropertyChanged(nameof(CashChangeAmountDisplay));
        }

        private void RefreshItemDiscounts()
        {
            foreach (var item in SaleItems)
            {
                item.DiscountPerUnit = CalculateDiscountPerUnit(item, GetSelectedDiscounts());
            }
        }

        private static decimal ResolveCurrentSalePrice(ProductDto product)
        {
            return product.CurrentSalePrice.GetValueOrDefault(product.SalePrice);
        }

        private static decimal ResolveCurrentUnitCost(ProductDto product)
        {
            return product.CurrentUnitCost.GetValueOrDefault(0);
        }

        private bool HasItemsWithoutCost()
        {
            return SaleItems.Any(item => item.UnitCost <= 0 || !item.ProductRecipeId.HasValue);
        }

        private string BuildCostingSaveMessage(string baseMessage)
        {
            return HasItemsWithoutCost()
                ? $"{baseMessage} Hay productos sin costo configurado; se guardaron con costo $0."
                : baseMessage;
        }

        private async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            IsSaving = true;
            try
            {
                if (SaleItems.Count == 0)
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "Faltan productos",
                        Message = "Agrega al menos un producto antes de confirmar la venta."
                    });
                    return;
                }

                if (string.IsNullOrWhiteSpace(PaymentMethod))
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "Falta el método de pago",
                        Message = "Selecciona si la venta fue pagada en efectivo o con tarjeta antes de confirmar."
                    });
                    return;
                }

                RefreshItemDiscounts();

                decimal? cashReceivedAmount = null;
                decimal? cashChangeAmount = null;
                if (IsCashPaymentSelected)
                {
                    if (string.IsNullOrWhiteSpace(CashReceivedAmountText))
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Error,
                            Title = "Efectivo recibido requerido",
                            Message = "Captura el monto recibido del cliente antes de confirmar la venta."
                        });
                        return;
                    }

                    if (!TryParseCashReceivedAmount(out var parsedCashReceivedAmount))
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Error,
                            Title = "Monto recibido invalido",
                            Message = "Captura un monto recibido valido para poder calcular el cambio."
                        });
                        return;
                    }

                    if (parsedCashReceivedAmount < Total)
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Error,
                            Title = "Efectivo insuficiente",
                            Message = "El monto recibido debe cubrir el total de la venta."
                        });
                        return;
                    }

                    cashReceivedAmount = parsedCashReceivedAmount;
                    cashChangeAmount = parsedCashReceivedAmount - Total;
                }

                var selectedDiscounts = GetSelectedDiscounts().ToList();

                var order = new OrderDto
                {
                    OrderId = _editingOrderId ?? 0,
                    BranchId = DirectSaleBranchId,
                    UserId = _defaultUserId,
                    Pieces = SaleItems.Sum(item => item.Quantity),
                    DiscountAmount = DiscountAmount,
                    TotalAmount = Total,
                    CashReceivedAmount = cashReceivedAmount,
                    CashChangeAmount = cashChangeAmount,
                    OrderStatusCode = OrderCatalog.CompletedOrderStatusCode,
                    PaymentStatusCode = OrderCatalog.PaidPaymentStatusCode,
                    SalesChannel = OrderCatalog.InStoreSalesChannelCode,
                    IsDirectSale = true,
                    PaymentMethod = PaymentMethod,
                    CustomerGender = IsEditingSale ? SelectedCustomerGender?.Code : null,
                    CustomerAgeRange = IsEditingSale ? SelectedCustomerAgeRange?.Code : null,
                    CustomerType = IsEditingSale ? SelectedCustomerType?.Code : null,
                    AcquisitionChannel = IsEditingSale ? SelectedAcquisitionChannel?.Code : null,
                    CreatedAt = IsEditingSale ? _editingCreatedAtUtc : DateTime.UtcNow,
                    UpdatedAt = IsEditingSale ? DateTime.UtcNow : null,
                    OrderItems = SaleItems.Select(item => new OrderItemDto
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        UnitCost = item.UnitCost,
                        DiscountPerUnit = item.DiscountPerUnit,
                        DiscountAmount = item.DiscountAmount,
                        ProductRecipeId = item.ProductRecipeId,
                        ProductPriceId = item.ProductPriceId,
                        CostCalculatedAt = item.CostCalculatedAt
                    }).ToList()
                };

                foreach (var discount in selectedDiscounts)
                {
                    order.OrderDiscounts.Add(new OrderDiscountDto
                    {
                        DiscountRuleId = discount.DiscountRuleId,
                        AppliedAmount = CalculateAppliedAmount(discount)
                    });
                }

                if (IsEditingSale)
                {
                    var updated = await _orderService.UpdateAsync(order);
                    if (updated)
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Success,
                            Title = "Edicion confirmada",
                            Message = BuildCostingSaveMessage($"La venta con folio #{order.OrderId} se actualizo correctamente.")
                        });
                        ResetForm();
                    }
                    else
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Error,
                            Title = "No se pudo actualizar la venta",
                            Message = $"No fue posible confirmar la edicion del folio #{order.OrderId}."
                        });
                    }
                }
                else
                {
                    var id = await _orderService.CreateAsync(order);
                    var newSaleId = id.GetValueOrDefault();
                    if (newSaleId > 0)
                    {
                        var branch = await _branchService.GetByIdAsync(DirectSaleBranchId);
                        var ticketDocument = BuildSaleTicketDocument(branch, newSaleId);
                        var saleMessage = BuildCostingSaveMessage($"La venta se guardó correctamente con el folio #{newSaleId}.");

                        _ = PrintConfirmedSaleTicketAsync(ticketDocument);

                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Success,
                            Title = "Venta registrada",
                            Message = saleMessage
                        });

                        ResetForm();
                    }
                    else
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Error,
                            Title = "No se pudo registrar la venta",
                            Message = "La venta no devolvió un folio válido. Revisa la conexión con el API o intenta nuevamente."
                        });
                    }
                }

                LogSlowOperation("SaleFormViewModel.SaveAsync", stopwatch);
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SaleFormViewModel.SaveCommand", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = IsEditingSale ? "Error al confirmar la edicion" : "Error al registrar la venta",
                    Message = "Ocurrio un error inesperado al guardar la venta. Revisa el archivo smartorder-errors.log."
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task PrintConfirmedSaleTicketAsync(TicketDocument ticketDocument)
        {
            try
            {
                var result = await _ticketPrinterService.PrintAsync(ticketDocument, TimeSpan.FromSeconds(3));
                if (!result.Success)
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Info,
                        Title = result.Title,
                        Message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SaleFormViewModel.PrintConfirmedSaleTicketAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Info,
                    Title = "Ticket pendiente",
                    Message = "La venta se guardo, pero no se pudo imprimir el ticket automaticamente."
                });
            }
        }

        public void ResetForm()
        {
            SaleItems.Clear();
            SetEditingOrder(null);
            _editingCreatedAtUtc = default;
            SaleDate = DateTime.Now;
            ProductSearchText = string.Empty;
            SelectedCategory = Categories.FirstOrDefault();
            _selectedDiscountIds.Clear();
            OnPropertyChanged(nameof(DiscountSelectionSummary));
            PaymentMethod = string.Empty;
            CashReceivedAmountText = string.Empty;
            SelectedCustomerGender = null;
            SelectedCustomerAgeRange = null;
            SelectedCustomerType = null;
            SelectedAcquisitionChannel = null;
            OnPropertyChanged(nameof(SaleDate));
            RefreshSaleState();
        }

        private void SetEditingOrder(int? orderId, DateTime? createdAtUtc = null)
        {
            _editingOrderId = orderId;
            if (createdAtUtc.HasValue)
            {
                _editingCreatedAtUtc = createdAtUtc.Value;
            }

            OnPropertyChanged(nameof(IsEditingSale));
            OnPropertyChanged(nameof(EditingOrderId));
            OnPropertyChanged(nameof(CanCancelEdit));
            OnPropertyChanged(nameof(ShowCancelEditButton));
            OnPropertyChanged(nameof(SaleHeaderTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            _cancelEditCommand.ChangeCanExecute();
        }

        private static DateTime NormalizeStoredUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static DateTime ToLocalDisplayDateTime(DateTime utcDateTime)
        {
            return utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime.ToLocalTime()
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc).ToLocalTime();
        }

        private static string? BuildBranchAddress(BranchDto? branch)
        {
            if (branch == null)
            {
                return null;
            }

            var addressParts = new[]
            {
                branch.Address,
                branch.City,
                branch.State,
                branch.PostalCode
            };

            var address = string.Join(", ", addressParts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => NormalizeDisplayText(part!)));

            return string.IsNullOrWhiteSpace(address) ? null : address;
        }

        private static string NormalizeDisplayText(string value)
        {
            return string.Join(' ', value
                .Trim()
                .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
        }

        private bool TryParseCashReceivedAmount(out decimal amount)
        {
            var text = CashReceivedAmountText?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                amount = 0;
                return false;
            }

            return decimal.TryParse(text, NumberStyles.Currency, CultureInfo.CurrentCulture, out amount)
                || decimal.TryParse(text, NumberStyles.Currency, CultureInfo.InvariantCulture, out amount);
        }

        private async Task SelectDiscountsAsync()
        {
            _skipNextCatalogRefreshOnAppearing = true;
            var selectedIds = await AppChecklistService.ShowAsync(
                "Selecciona descuentos",
                Discounts
                    .Where(discount => discount.IsActive && discount.DiscountRuleId > 0)
                    .Select(discount => new ChecklistOption
                    {
                        Id = discount.DiscountRuleId,
                        Text = discount.Name,
                        IsSelected = _selectedDiscountIds.Contains(discount.DiscountRuleId)
                    }));

            if (selectedIds == null)
            {
                return;
            }

            _selectedDiscountIds.Clear();
            foreach (var discountId in selectedIds)
            {
                _selectedDiscountIds.Add(discountId);
            }

            OnPropertyChanged(nameof(DiscountSelectionSummary));
            RefreshItemDiscounts();
            RefreshTotals();
        }

        private IEnumerable<DiscountRuleDto> GetSelectedDiscounts()
        {
            return Discounts.Where(discount => _selectedDiscountIds.Contains(discount.DiscountRuleId));
        }

        private decimal CalculateDiscountPerUnit(SaleItemViewModel item, IEnumerable<DiscountRuleDto> discounts)
        {
            return DiscountCalculator.CalculateDiscountPerUnit(
                new DiscountLineInput(item.ProductId, item.CategoryId, item.Quantity, item.Price),
                discounts,
                DiscountLimits,
                TotalProductQuantity,
                Subtotal,
                DateOnly.FromDateTime(SaleDate));
        }

        private decimal CalculateAppliedAmount(DiscountRuleDto discount)
        {
            return SaleItems.Sum(item => CalculateDiscountPerUnit(item, new[] { discount }) * item.Quantity);
        }

        private static void LogSlowOperation(string source, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            if (stopwatch.Elapsed >= TimeSpan.FromSeconds(5))
            {
                FileErrorLogger.LogMessage(source, $"Operacion lenta: {stopwatch.Elapsed.TotalSeconds:0.0} segundos.");
            }
        }

        private void LoadCustomerOptions()
        {
            CustomerGenderOptions.Clear();
            foreach (var option in OrderCatalog.CustomerGenderOptions)
            {
                CustomerGenderOptions.Add(option);
            }

            CustomerAgeRangeOptions.Clear();
            foreach (var option in OrderCatalog.CustomerAgeRangeOptions)
            {
                CustomerAgeRangeOptions.Add(option);
            }

            CustomerTypeOptions.Clear();
            foreach (var option in OrderCatalog.CustomerTypeOptions)
            {
                CustomerTypeOptions.Add(option);
            }

            AcquisitionChannelOptions.Clear();
            foreach (var option in OrderCatalog.AcquisitionChannelOptions)
            {
                AcquisitionChannelOptions.Add(option);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
