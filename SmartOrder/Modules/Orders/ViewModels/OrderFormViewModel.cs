using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Orders.Models;
using SmartOrder.Modules.Orders.Pages;
using SmartOrder.Modules.Orders.Services;
using SmartOrder.Shared.Catalogs;
using SmartOrder.Shared.Models;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace SmartOrder.Modules.Orders.ViewModels
{
    public class OrderFormViewModel : INotifyPropertyChanged
    {
        private const int DefaultBranchId = 1;

        private readonly int _defaultUserId;
        private readonly OrderService _orderService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly CustomerService _customerService;
        private readonly BranchService _branchService;
        private readonly OrderStatusService _orderStatusService;
        private readonly PaymentStatusService _paymentStatusService;
        private readonly DiscountRuleService _discountRuleService;
        private readonly DiscountLimitRuleService _discountLimitRuleService;
        private readonly Command _addProductCommand;
        private readonly Command _saveCommand;
        private readonly Command _loadOrderForEditCommand;
        private readonly Command _cancelEditCommand;
        private readonly Dictionary<int, string> _categoryNamesById = new();

        private int _currentBranchId = DefaultBranchId;
        private int? _editingOrderId;
        private DateTime _editingCreatedAtUtc;
        private bool _isSaving;
        private bool _isLoadingCatalogData;
        private bool _isLoadingOrderForEdit;
        private bool _skipNextCatalogRefreshOnAppearing;
        private CategoryDto? _selectedCategory;
        private ProductDto? _selectedProduct;
        private CustomerDto? _selectedCustomer;
        private OrderStatusDto? _selectedOrderStatus;
        private PaymentStatusDto? _selectedPaymentStatus;
        private LookupOption? _selectedPaymentMethod;
        private readonly HashSet<int> _selectedDiscountIds = new();
        private string _productSearchText = string.Empty;
        private string _comments = string.Empty;

        public OrderFormViewModel(
            int defaultUserId,
            OrderService orderService,
            ProductService productService,
            CategoryService categoryService,
            CustomerService customerService,
            BranchService branchService,
            OrderStatusService orderStatusService,
            PaymentStatusService paymentStatusService,
            DiscountRuleService discountRuleService,
            DiscountLimitRuleService discountLimitRuleService)
        {
            _defaultUserId = defaultUserId;
            _orderService = orderService;
            _productService = productService;
            _categoryService = categoryService;
            _customerService = customerService;
            _branchService = branchService;
            _orderStatusService = orderStatusService;
            _paymentStatusService = paymentStatusService;
            _discountRuleService = discountRuleService;
            _discountLimitRuleService = discountLimitRuleService;

            SearchProductCommand = new Command(FilterProducts);
            SelectDiscountsCommand = new Command(async () => await SelectDiscountsAsync());
            _addProductCommand = new Command(AddSelectedProduct, () => CanAddProduct);
            AddProductCommand = _addProductCommand;
            _saveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
            SaveCommand = _saveCommand;
            _loadOrderForEditCommand = new Command(async () => await LoadOrderForEditAsync(), () => CanLoadOrderForEdit);
            LoadOrderForEditCommand = _loadOrderForEditCommand;
            _cancelEditCommand = new Command(CancelEdit, () => CanCancelEdit);
            CancelEditCommand = _cancelEditCommand;
            ExportOrderPdfCommand = new Command(async () => await ExportOrderPdfAsync());

            OrderItems.CollectionChanged += OrderItems_CollectionChanged;
            LoadPaymentMethods();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime ProductionStartDate { get; set; } = DateTime.Today;
        public DateTime ProductionEndDate { get; set; } = DateTime.Today;
        public DateTime DeliveryDate { get; set; } = DateTime.Today;
        public TimeSpan DeliveryTime { get; set; } = DateTime.Now.TimeOfDay;

        public ObservableCollection<CategoryDto> Categories { get; } = new();
        public ObservableCollection<ProductDto> Products { get; } = new();
        public ObservableCollection<ProductDto> FilteredProducts { get; } = new();
        public ObservableCollection<CustomerDto> Customers { get; } = new();
        public ObservableCollection<OrderStatusDto> OrderStatuses { get; } = new();
        public ObservableCollection<PaymentStatusDto> PaymentStatuses { get; } = new();
        public ObservableCollection<DiscountRuleDto> Discounts { get; } = new();
        public ObservableCollection<DiscountLimitRuleDto> DiscountLimits { get; } = new();
        public ObservableCollection<LookupOption> PaymentMethods { get; } = new();
        public ObservableCollection<OrderItemViewModel> OrderItems { get; } = new();

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

        public CustomerDto? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    OnPropertyChanged(nameof(SelectedCustomer));
                }
            }
        }

        public OrderStatusDto? SelectedOrderStatus
        {
            get => _selectedOrderStatus;
            set
            {
                if (_selectedOrderStatus != value)
                {
                    _selectedOrderStatus = value;
                    OnPropertyChanged(nameof(SelectedOrderStatus));
                }
            }
        }

        public PaymentStatusDto? SelectedPaymentStatus
        {
            get => _selectedPaymentStatus;
            set
            {
                if (_selectedPaymentStatus != value)
                {
                    _selectedPaymentStatus = value;
                    OnPropertyChanged(nameof(SelectedPaymentStatus));
                }
            }
        }

        public LookupOption? SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (_selectedPaymentMethod != value)
                {
                    _selectedPaymentMethod = value;
                    OnPropertyChanged(nameof(SelectedPaymentMethod));
                }
            }
        }

        public string DiscountSelectionSummary => GetSelectedDiscounts().Any()
            ? string.Join(", ", GetSelectedDiscounts().Select(discount => discount.Name))
            : "Sin descuento";

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

        public string Comments
        {
            get => _comments;
            set
            {
                if (_comments != value)
                {
                    _comments = value;
                    OnPropertyChanged(nameof(Comments));
                }
            }
        }

        public int TotalProductQuantity => OrderItems.Sum(item => item.Quantity);
        public decimal Subtotal => OrderItems.Sum(item => item.Subtotal);
        public decimal DiscountAmount => OrderItems.Sum(item => item.DiscountAmount);
        public decimal Total => Subtotal - DiscountAmount;
        public bool HasOrderItems => OrderItems.Any();
        public bool IsOrderItemsEmpty => !HasOrderItems;
        public int? EditingOrderId => _editingOrderId;
        public bool IsEditingOrder => _editingOrderId.HasValue;
        public bool IsNotSaving => !IsSaving;
        public bool CanAddProduct => SelectedProduct != null && !IsSaving && !IsLoadingCatalogData;
        public bool CanLoadOrderForEdit => !IsSaving && !IsLoadingCatalogData && !_isLoadingOrderForEdit;
        public bool CanCancelEdit => IsEditingOrder && !IsSaving;
        public bool ShowCancelEditButton => IsEditingOrder;
        public bool TryConsumeCatalogRefreshSkip()
        {
            if (_skipNextCatalogRefreshOnAppearing)
            {
                _skipNextCatalogRefreshOnAppearing = false;
                return true;
            }

            return false;
        }
        public string HeaderTitle => IsEditingOrder ? "Editar pedido" : "Nuevo pedido";
        public string SaveButtonText => IsSaving ? "Guardando..." : IsEditingOrder ? "Guardar cambios" : "Confirmar pedido";

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
                    OnPropertyChanged(nameof(CanLoadOrderForEdit));
                    OnPropertyChanged(nameof(CanCancelEdit));
                    OnPropertyChanged(nameof(ShowCancelEditButton));
                    _addProductCommand.ChangeCanExecute();
                    _saveCommand.ChangeCanExecute();
                    _loadOrderForEditCommand.ChangeCanExecute();
                    _cancelEditCommand.ChangeCanExecute();
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
                    OnPropertyChanged(nameof(CanLoadOrderForEdit));
                    _addProductCommand.ChangeCanExecute();
                    _loadOrderForEditCommand.ChangeCanExecute();
                }
            }
        }

        private bool IsLoadingOrderForEdit
        {
            get => _isLoadingOrderForEdit;
            set
            {
                if (_isLoadingOrderForEdit != value)
                {
                    _isLoadingOrderForEdit = value;
                    OnPropertyChanged(nameof(CanLoadOrderForEdit));
                    _loadOrderForEditCommand.ChangeCanExecute();
                }
            }
        }

        public ICommand SearchProductCommand { get; }
        public ICommand SelectDiscountsCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand LoadOrderForEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ExportOrderPdfCommand { get; }

        public async Task RefreshCatalogDataAsync()
        {
            IsLoadingCatalogData = true;
            try
            {
                var selectedCategoryId = SelectedCategory?.CategoryId;
                var selectedProductId = SelectedProduct?.ProductId;
                var selectedCustomerId = SelectedCustomer?.CustomerId;
                var selectedOrderStatusCode = SelectedOrderStatus?.OrderStatusCode;
                var selectedPaymentStatusCode = SelectedPaymentStatus?.PaymentStatusCode;
                var selectedDiscountIds = _selectedDiscountIds.ToHashSet();

                Categories.Clear();
                Products.Clear();
                FilteredProducts.Clear();
                Customers.Clear();
                OrderStatuses.Clear();
                PaymentStatuses.Clear();
                Discounts.Clear();
                DiscountLimits.Clear();
                _categoryNamesById.Clear();

                var products = (await _productService.GetAllAsync())
                    .Where(product => product.IsActive)
                    .OrderBy(product => product.Name)
                    .ToList();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                var categories = (await _categoryService.GetAllAsync())
                    .Where(category => category.IsActive)
                    .OrderBy(category => category.DisplayOrder)
                    .ThenBy(category => category.Name)
                    .ToList();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                    _categoryNamesById[category.CategoryId] = category.Name;
                }

                var customers = (await _customerService.GetAllAsync())
                    .Where(customer => customer.IsActive)
                    .Select(NormalizeCustomerForPicker)
                    .GroupBy(customer => customer.CustomerId)
                    .Select(group => group.First())
                    .OrderBy(customer => customer.FullName)
                    .ToList();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }

                foreach (var status in (await _orderStatusService.GetAllAsync()).Where(status => status.IsActive))
                {
                    OrderStatuses.Add(status);
                }

                foreach (var status in (await _paymentStatusService.GetAllAsync()).Where(status => status.IsActive))
                {
                    PaymentStatuses.Add(status);
                }

                foreach (var discount in (await _discountRuleService.GetAllAsync())
                    .GroupBy(discount => discount.DiscountRuleId)
                    .Select(group => group.First()))
                {
                    AddDiscountIfMissing(discount);
                }

                foreach (var discountLimit in (await _discountLimitRuleService.GetAllAsync())
                    .GroupBy(discountLimit => discountLimit.DiscountLimitRuleId)
                    .Select(group => group.First()))
                {
                    DiscountLimits.Add(discountLimit);
                }

                SelectedCategory = Categories.FirstOrDefault(category => category.CategoryId == selectedCategoryId)
                    ?? Categories.FirstOrDefault();
                SelectedProduct = FilteredProducts.FirstOrDefault(product => product.ProductId == selectedProductId)
                    ?? FilteredProducts.FirstOrDefault();
                SelectedCustomer = Customers.FirstOrDefault(customer => customer.CustomerId == selectedCustomerId);
                SelectedOrderStatus = OrderStatuses.FirstOrDefault(status => CodesEqual(status.OrderStatusCode, selectedOrderStatusCode));
                SelectedPaymentStatus = PaymentStatuses.FirstOrDefault(status => CodesEqual(status.PaymentStatusCode, selectedPaymentStatusCode));
                _selectedDiscountIds.Clear();
                foreach (var discountId in selectedDiscountIds.Where(id => Discounts.Any(discount => discount.DiscountRuleId == id)))
                {
                    _selectedDiscountIds.Add(discountId);
                }
                OnPropertyChanged(nameof(DiscountSelectionSummary));

                foreach (var item in OrderItems)
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
                RefreshOrderState();
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("OrderFormViewModel.RefreshCatalogDataAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo cargar pedidos",
                    Message = "No fue posible cargar productos, clientes o estatus. Revisa la conexion con el API."
                });
            }
            finally
            {
                IsLoadingCatalogData = false;
            }
        }

        private void LoadPaymentMethods()
        {
            PaymentMethods.Clear();
            foreach (var option in OrderCatalog.PaymentMethodOptions)
            {
                PaymentMethods.Add(option);
            }
        }

        private async Task ExportOrderPdfAsync()
        {
            if (SelectedCustomer == null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Cliente requerido",
                    Message = "Selecciona un cliente antes de exportar la nota del pedido."
                });
                return;
            }

            if (!OrderItems.Any())
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Pedido vacio",
                    Message = "Agrega al menos un producto antes de exportar la nota del pedido."
                });
                return;
            }

            try
            {
                var branch = await _branchService.GetByIdAsync(_currentBranchId);
                var filePath = await OrderReceiptPdfExporter.ExportAsync(new OrderReceiptPdfRequest(
                    _editingOrderId,
                    branch?.Name ?? "SmartOrder",
                    BuildBranchAddress(branch),
                    branch?.Phone,
                    SelectedCustomer.FullName,
                    DeliveryDate,
                    DeliveryTime,
                    Comments,
                    OrderItems.ToList(),
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
                    Message = "La nota del pedido se genero correctamente en la carpeta Documentos\\SmartOrder\\Orders."
                });
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("OrderFormViewModel.ExportOrderPdfAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo exportar",
                    Message = "Ocurrio un error al generar el PDF del pedido. Revisa el archivo smartorder-errors.log."
                });
            }
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

        private void AddSelectedProduct()
        {
            if (!CanAddProduct || SelectedProduct == null)
            {
                return;
            }

            var existingItem = OrderItems.FirstOrDefault(item => item.ProductId == SelectedProduct.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
                MoveOrderItemToTop(existingItem);
                return;
            }

            OrderItems.Insert(0, new OrderItemViewModel(RemoveItem)
            {
                ProductId = SelectedProduct.ProductId,
                CategoryId = SelectedProduct.CategoryId,
                CategoryName = ResolveCategoryName(SelectedProduct.CategoryId),
                ProductName = SelectedProduct.Name,
                Price = SelectedProduct.SalePrice,
                Quantity = 1
            });
            RefreshItemDiscounts();
        }

        private async Task LoadOrderForEditAsync()
        {
            if (!CanLoadOrderForEdit)
            {
                return;
            }

            IsLoadingOrderForEdit = true;
            try
            {
                var folio = await OrderEditSelectionPage.ShowAsync(await BuildEditableOrderListAsync());
                if (!folio.HasValue)
                {
                    return;
                }

                await LoadOrderByIdAsync(folio.Value, showLoadedMessage: true);
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("OrderFormViewModel.LoadOrderForEditAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo cargar el pedido",
                    Message = "Ocurrio un error al intentar cargar el folio solicitado."
                });
            }
            finally
            {
                IsLoadingOrderForEdit = false;
            }
        }

        public async Task LoadOrderByIdAsync(int orderId, bool showLoadedMessage = false)
        {
            var order = await _orderService.GetCustomOrderByIdAsync(orderId);
            if (order == null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Pedido no disponible",
                    Message = $"No se encontro un pedido con el folio #{orderId}."
                });
                return;
            }

            if (!Products.Any())
            {
                await RefreshCatalogDataAsync();
            }

            var availableProductIds = Products.Select(product => product.ProductId).ToHashSet();
            var missingProducts = order.OrderItems
                .Where(orderItem => !availableProductIds.Contains(orderItem.ProductId))
                .Select(orderItem => orderItem.ProductId)
                .Distinct()
                .OrderBy(productId => productId)
                .ToList();

            if (missingProducts.Any())
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se puede editar el pedido",
                    Message = $"Hay productos del folio seleccionado que no estan disponibles en el catalogo de pedidos. Producto(s): {string.Join(", ", missingProducts)}."
                });
                return;
            }

            await ApplyOrderToFormAsync(order);

            if (showLoadedMessage)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Info,
                    Title = "Pedido cargado",
                    Message = $"Ahora estas editando el pedido con folio #{orderId}."
                });
            }
        }

        private async Task<IReadOnlyList<OrderEditSelectionItem>> BuildEditableOrderListAsync()
        {
            var customersById = Customers
                .GroupBy(customer => customer.CustomerId)
                .ToDictionary(group => group.Key, group => group.First());

            return (await _orderService.GetActiveCustomOrdersAsync())
                .Where(order => !IsCompletedOrCancelled(order))
                .Select(order =>
                {
                    var customerName = order.CustomerId.HasValue && customersById.TryGetValue(order.CustomerId.Value, out var customer)
                        ? customer.FullName
                        : "Cliente no encontrado";
                    var deliveryDate = ToLocalDisplayDate(order.DeliveryDate);

                    return new
                    {
                        DeliveryDate = deliveryDate,
                        Item = new OrderEditSelectionItem(
                        order.OrderId,
                        NormalizeDisplayText(customerName),
                        order.Pieces,
                            deliveryDate.ToString("dd/MM/yyyy"))
                    };
                })
                .OrderBy(order => order.DeliveryDate)
                .ThenBy(order => order.Item.Folio)
                .Select(order => order.Item)
                .ToList();
        }

        private static bool IsCompletedOrCancelled(OrderDto order)
        {
            return string.Equals(order.OrderStatusCode, OrderCatalog.CompletedOrderStatusCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.OrderStatusCode, OrderCatalog.CancelledOrderStatusCode, StringComparison.OrdinalIgnoreCase);
        }

        private async Task ApplyOrderToFormAsync(OrderDto order)
        {
            OrderItems.Clear();
            ProductSearchText = string.Empty;

            SetEditingOrder(order.OrderId, NormalizeStoredUtc(order.CreatedAt));
            _currentBranchId = order.BranchId;
            OrderDate = ToLocalDisplayDateTime(_editingCreatedAtUtc);
            ProductionStartDate = ToLocalDisplayDate(order.ProductionStartDate);
            ProductionEndDate = ToLocalDisplayDate(order.ProductionEndDate);
            DeliveryDate = ToLocalDisplayDate(order.DeliveryDate);
            DeliveryTime = ToLocalDisplayTime(order.DeliveryDate);
            Comments = order.Comments ?? string.Empty;
            SelectedCustomer = Customers.FirstOrDefault(customer => customer.CustomerId == order.CustomerId);
            SelectedOrderStatus = FindOrCreateOrderStatus(order.OrderStatusCode);
            SelectedPaymentStatus = FindOrCreatePaymentStatus(order.PaymentStatusCode);
            SelectedPaymentMethod = PaymentMethods.FirstOrDefault(method => CodesEqual(method.Code, order.PaymentMethod));
            await ApplyOrderDiscountsAsync(order);

            OnPropertyChanged(nameof(OrderDate));
            OnPropertyChanged(nameof(ProductionStartDate));
            OnPropertyChanged(nameof(ProductionEndDate));
            OnPropertyChanged(nameof(DeliveryDate));
            OnPropertyChanged(nameof(DeliveryTime));

            var orderProducts = order.OrderItems
                .Select(orderItem => new
                {
                    OrderItem = orderItem,
                    Product = Products.FirstOrDefault(product => product.ProductId == orderItem.ProductId)
                })
                .Where(item => item.Product != null)
                .OrderBy(item => ResolveCategoryName(item.Product!.CategoryId))
                .ThenBy(item => item.Product!.Name)
                .ToList();

            var firstCategoryId = orderProducts.FirstOrDefault()?.Product?.CategoryId;
            SelectedCategory = Categories.FirstOrDefault(category => category.CategoryId == firstCategoryId) ?? Categories.FirstOrDefault();

            foreach (var orderProduct in orderProducts)
            {
                OrderItems.Add(new OrderItemViewModel(RemoveItem)
                {
                    ProductId = orderProduct.OrderItem.ProductId,
                    CategoryId = orderProduct.Product!.CategoryId,
                    CategoryName = ResolveCategoryName(orderProduct.Product!.CategoryId),
                    ProductName = orderProduct.Product.Name,
                    Price = orderProduct.OrderItem.UnitPrice,
                    DiscountPerUnit = orderProduct.OrderItem.DiscountPerUnit ?? 0,
                    Quantity = orderProduct.OrderItem.Quantity
                });
            }

            RefreshItemDiscounts();
            RefreshOrderState();
        }

        private void MoveOrderItemToTop(OrderItemViewModel item)
        {
            var currentIndex = OrderItems.IndexOf(item);
            if (currentIndex > 0)
            {
                OrderItems.Move(currentIndex, 0);
            }
        }

        private async Task SaveAsync()
        {
            if (IsSaving)
            {
                return;
            }

            IsSaving = true;
            try
            {
                if (!Validate(out var validationMessage))
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "Pedido incompleto",
                        Message = validationMessage
                    });
                    return;
                }

                RefreshItemDiscounts();
                var selectedDiscounts = GetSelectedDiscounts().ToList();

                var order = new OrderDto
                {
                    OrderId = _editingOrderId ?? 0,
                    BranchId = _currentBranchId,
                    UserId = _defaultUserId,
                    CustomerId = SelectedCustomer!.CustomerId,
                    Pieces = TotalProductQuantity,
                    DiscountAmount = DiscountAmount,
                    TotalAmount = Total,
                    ProductionStartDate = ToUtcFromLocalDate(ProductionStartDate),
                    ProductionEndDate = ToUtcFromLocalDate(ProductionEndDate),
                    DeliveryDate = ToUtcFromLocalDateTime(DeliveryDate, DeliveryTime),
                    OrderStatusCode = SelectedOrderStatus!.OrderStatusCode,
                    PaymentStatusCode = SelectedPaymentStatus!.PaymentStatusCode,
                    PaymentMethod = SelectedPaymentMethod!.Code,
                    SalesChannel = OrderCatalog.CustomOrderSalesChannelCode,
                    Comments = string.IsNullOrWhiteSpace(Comments) ? null : Comments.Trim(),
                    IsDirectSale = false,
                    CreatedAt = IsEditingOrder ? _editingCreatedAtUtc : DateTime.UtcNow,
                    UpdatedAt = IsEditingOrder ? DateTime.UtcNow : null,
                    OrderItems = OrderItems.Select(item => new OrderItemDto
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        DiscountPerUnit = item.DiscountPerUnit,
                        DiscountAmount = item.DiscountAmount
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

                if (IsEditingOrder)
                {
                    var updated = await _orderService.UpdateAsync(order);
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = updated ? AppMessageType.Success : AppMessageType.Error,
                        Title = updated ? "Pedido actualizado" : "No se pudo actualizar",
                        Message = updated
                            ? $"El pedido con folio #{order.OrderId} se actualizo correctamente."
                            : $"No fue posible actualizar el pedido #{order.OrderId}."
                    });

                    if (updated)
                    {
                        ResetForm();
                    }
                }
                else
                {
                    var id = await _orderService.CreateAsync(order);
                    if (id.HasValue)
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Success,
                            Title = "Pedido registrado",
                            Message = $"El pedido se guardo correctamente con el folio #{id.Value}."
                        });
                        ResetForm();
                    }
                    else
                    {
                        await AppMessageService.ShowAsync(new AppMessageOptions
                        {
                            Type = AppMessageType.Error,
                            Title = "No se pudo registrar",
                            Message = "El API no devolvio un folio valido para el pedido."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("OrderFormViewModel.SaveAsync", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Error al guardar pedido",
                    Message = "Ocurrio un error inesperado al guardar el pedido. Revisa el archivo smartorder-errors.log."
                });
            }
            finally
            {
                IsSaving = false;
            }
        }

        private bool Validate(out string message)
        {
            if (!OrderItems.Any())
            {
                message = "Agrega al menos un producto antes de confirmar el pedido.";
                return false;
            }

            if (SelectedCustomer == null)
            {
                message = "Selecciona el cliente al que pertenece el pedido.";
                return false;
            }

            if (SelectedOrderStatus == null || SelectedPaymentStatus == null || SelectedPaymentMethod == null)
            {
                message = "Selecciona estatus del pedido, estatus del pago y metodo de pago.";
                return false;
            }

            if (ProductionEndDate.Date < ProductionStartDate.Date)
            {
                message = "La fecha final de produccion no puede ser menor que la fecha de inicio.";
                return false;
            }

            if (DeliveryDate.Date < ProductionEndDate.Date)
            {
                message = "La fecha de entrega no puede ser menor que la fecha final de produccion.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        public void ResetForm()
        {
            OrderItems.Clear();
            SetEditingOrder(null);
            _currentBranchId = DefaultBranchId;
            _editingCreatedAtUtc = default;
            OrderDate = DateTime.Now;
            ProductionStartDate = DateTime.Today;
            ProductionEndDate = DateTime.Today;
            DeliveryDate = DateTime.Today;
            DeliveryTime = DateTime.Now.TimeOfDay;
            Comments = string.Empty;
            ProductSearchText = string.Empty;
            SelectedCategory = Categories.FirstOrDefault();
            SelectedCustomer = null;
            SelectedOrderStatus = null;
            SelectedPaymentStatus = null;
            SelectedPaymentMethod = null;
            _selectedDiscountIds.Clear();
            OnPropertyChanged(nameof(DiscountSelectionSummary));
            OnPropertyChanged(nameof(OrderDate));
            OnPropertyChanged(nameof(ProductionStartDate));
            OnPropertyChanged(nameof(ProductionEndDate));
            OnPropertyChanged(nameof(DeliveryDate));
            OnPropertyChanged(nameof(DeliveryTime));
            RefreshOrderState();
        }

        private void CancelEdit()
        {
            if (CanCancelEdit)
            {
                ResetForm();
            }
        }

        private void OrderItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (OrderItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= OrderItem_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (OrderItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += OrderItem_PropertyChanged;
                }
            }

            RefreshOrderState();
        }

        private void OrderItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderItemViewModel.Quantity))
            {
                RefreshItemDiscounts();
                RefreshTotals();
                return;
            }

            if (e.PropertyName == nameof(OrderItemViewModel.Subtotal) ||
                e.PropertyName == nameof(OrderItemViewModel.DiscountAmount))
            {
                RefreshTotals();
            }
        }

        private void RemoveItem(OrderItemViewModel item)
        {
            if (OrderItems.Contains(item))
            {
                OrderItems.Remove(item);
            }
        }

        private void RefreshOrderState()
        {
            OnPropertyChanged(nameof(HasOrderItems));
            OnPropertyChanged(nameof(IsOrderItemsEmpty));
            RefreshTotals();
        }

        private void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalProductQuantity));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(Total));
        }

        private void RefreshItemDiscounts()
        {
            foreach (var item in OrderItems)
            {
                item.DiscountPerUnit = CalculateDiscountPerUnit(item, GetSelectedDiscounts());
            }
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

        private decimal CalculateDiscountPerUnit(OrderItemViewModel item, IEnumerable<DiscountRuleDto> discounts)
        {
            return DiscountCalculator.CalculateDiscountPerUnit(
                new DiscountLineInput(item.ProductId, item.CategoryId, item.Quantity, item.Price),
                discounts,
                DiscountLimits,
                TotalProductQuantity,
                Subtotal,
                DateOnly.FromDateTime(OrderDate));
        }

        private decimal CalculateAppliedAmount(DiscountRuleDto discount)
        {
            return OrderItems.Sum(item => CalculateDiscountPerUnit(item, new[] { discount }) * item.Quantity);
        }

        private string ResolveCategoryName(int categoryId)
        {
            return _categoryNamesById.TryGetValue(categoryId, out var categoryName)
                ? categoryName
                : SelectedCategory?.Name ?? string.Empty;
        }

        private OrderStatusDto? FindOrCreateOrderStatus(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var status = OrderStatuses.FirstOrDefault(currentStatus => CodesEqual(currentStatus.OrderStatusCode, code));
            if (status != null)
            {
                return status;
            }

            status = new OrderStatusDto
            {
                OrderStatusCode = code.Trim(),
                DisplayName = $"{code.Trim()} (inactivo)",
                IsActive = false
            };
            OrderStatuses.Add(status);
            return status;
        }

        private PaymentStatusDto? FindOrCreatePaymentStatus(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var status = PaymentStatuses.FirstOrDefault(currentStatus => CodesEqual(currentStatus.PaymentStatusCode, code));
            if (status != null)
            {
                return status;
            }

            status = new PaymentStatusDto
            {
                PaymentStatusCode = code.Trim(),
                DisplayName = $"{code.Trim()} (inactivo)",
                IsActive = false
            };
            PaymentStatuses.Add(status);
            return status;
        }

        private async Task ApplyOrderDiscountsAsync(OrderDto order)
        {
            _selectedDiscountIds.Clear();
            foreach (var discountId in order.OrderDiscounts.Select(discount => discount.DiscountRuleId).Distinct())
            {
                var discount = Discounts.FirstOrDefault(currentDiscount => currentDiscount.DiscountRuleId == discountId);
                if (discount == null)
                {
                    try
                    {
                        discount = await _discountRuleService.GetByIdAsync(discountId);
                    }
                    catch (Exception ex)
                    {
                        FileErrorLogger.Log("OrderFormViewModel.ApplyOrderDiscountsAsync", ex, $"DiscountRuleId={discountId}");
                    }

                    discount ??= CreateHistoricalDiscountOption(discountId, order);
                    AddDiscountIfMissing(discount);
                }

                _selectedDiscountIds.Add(discountId);
            }

            OnPropertyChanged(nameof(DiscountSelectionSummary));
        }

        private DiscountRuleDto AddDiscountIfMissing(DiscountRuleDto discount)
        {
            var existing = Discounts.FirstOrDefault(currentDiscount => currentDiscount.DiscountRuleId == discount.DiscountRuleId);
            if (existing != null)
            {
                return existing;
            }

            Discounts.Add(discount);
            return discount;
        }

        private static DiscountRuleDto CreateHistoricalDiscountOption(int discountRuleId, OrderDto order)
        {
            var discountPerUnit = order.OrderItems
                .Where(item => item.DiscountPerUnit.HasValue)
                .Select(item => item.DiscountPerUnit!.Value)
                .DefaultIfEmpty(order.Pieces > 0 ? (order.DiscountAmount ?? 0) / order.Pieces : 0)
                .First();

            return new DiscountRuleDto
            {
                DiscountRuleId = discountRuleId,
                Name = $"Descuento aplicado #{discountRuleId}",
                DiscountTypeCode = string.Empty,
                DiscountTargetCode = string.Empty,
                DiscountValue = discountPerUnit,
                IsActive = false
            };
        }

        private static bool CodesEqual(string? left, string? right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void SetEditingOrder(int? orderId, DateTime? createdAtUtc = null)
        {
            _editingOrderId = orderId;
            if (createdAtUtc.HasValue)
            {
                _editingCreatedAtUtc = createdAtUtc.Value;
            }

            OnPropertyChanged(nameof(IsEditingOrder));
            OnPropertyChanged(nameof(CanCancelEdit));
            OnPropertyChanged(nameof(ShowCancelEditButton));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(SaveButtonText));
            _cancelEditCommand.ChangeCanExecute();
        }

        private static DateTime ToUtcFromLocalDate(DateTime value)
        {
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Local).ToUniversalTime();
        }

        private static DateTime ToUtcFromLocalDateTime(DateTime date, TimeSpan time)
        {
            return DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Local).ToUniversalTime();
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

        private static DateTime ToLocalDisplayDate(DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
            {
                return DateTime.Today;
            }

            return ToLocalDisplayDateTime(NormalizeStoredUtc(utcDateTime.Value)).Date;
        }

        private static TimeSpan ToLocalDisplayTime(DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
            {
                return DateTime.Now.TimeOfDay;
            }

            return ToLocalDisplayDateTime(NormalizeStoredUtc(utcDateTime.Value)).TimeOfDay;
        }

        private static CustomerDto NormalizeCustomerForPicker(CustomerDto customer)
        {
            customer.FullName = NormalizeDisplayText(customer.FullName);
            return customer;
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
