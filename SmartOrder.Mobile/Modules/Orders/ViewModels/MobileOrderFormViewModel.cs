using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Entities.Orders.Models;
using SmartOrder.Mobile.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SmartOrder.Mobile.Modules.Orders.ViewModels;

public sealed class MobileOrderFormViewModel : ObservableObject
{
    private const int DefaultBranchId = 1;
    private const int DefaultUserId = 1;

    private readonly OrderService _orderService;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly CustomerService _customerService;
    private readonly OrderStatusService _orderStatusService;
    private readonly PaymentStatusService _paymentStatusService;
    private readonly DiscountRuleService _discountRuleService;
    private readonly DiscountLimitRuleService _discountLimitRuleService;
    private readonly Dictionary<int, string> _categoryNamesById = new();
    private bool _catalogsLoaded;
    private bool _isLoading;
    private bool _isSaving;
    private int? _editingOrderId;
    private DateTime _editingCreatedAtUtc;
    private CategoryDto? _selectedCategory;
    private ProductDto? _selectedProduct;
    private CustomerDto? _selectedCustomer;
    private OrderStatusDto? _selectedOrderStatus;
    private PaymentStatusDto? _selectedPaymentStatus;
    private LookupOption? _selectedPaymentMethod;
    private string _productSearchText = string.Empty;
    private string _comments = string.Empty;
    private string _message = string.Empty;
    private bool _isInternalProduction;

    public MobileOrderFormViewModel(
        OrderService orderService,
        ProductService productService,
        CategoryService categoryService,
        CustomerService customerService,
        OrderStatusService orderStatusService,
        PaymentStatusService paymentStatusService,
        DiscountRuleService discountRuleService,
        DiscountLimitRuleService discountLimitRuleService)
    {
        _orderService = orderService;
        _productService = productService;
        _categoryService = categoryService;
        _customerService = customerService;
        _orderStatusService = orderStatusService;
        _paymentStatusService = paymentStatusService;
        _discountRuleService = discountRuleService;
        _discountLimitRuleService = discountLimitRuleService;

        AddProductCommand = new Command(AddSelectedProduct);
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsSaving);
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        foreach (var option in OrderCatalog.PaymentMethodOptions)
        {
            PaymentMethods.Add(option);
        }
    }

    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<ProductDto> FilteredProducts { get; } = new();
    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<OrderStatusDto> OrderStatuses { get; } = new();
    public ObservableCollection<PaymentStatusDto> PaymentStatuses { get; } = new();
    public ObservableCollection<LookupOption> PaymentMethods { get; } = new();
    public ObservableCollection<MobileDiscountOptionViewModel> Discounts { get; } = new();
    public ObservableCollection<DiscountLimitRuleDto> DiscountLimits { get; } = new();
    public ObservableCollection<MobileOrderLineViewModel> OrderItems { get; } = new();
    public ICommand AddProductCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public DateTime ProductionStartDate { get; set; } = DateTime.Today;
    public DateTime ProductionEndDate { get; set; } = DateTime.Today;
    public DateTime DeliveryDate { get; set; } = DateTime.Today;
    public TimeSpan DeliveryTime { get; set; } = DateTime.Now.TimeOfDay;

    public string HeaderTitle => _editingOrderId.HasValue ? $"Editar pedido #{_editingOrderId}" : "Nuevo pedido";
    public string SaveButtonText => IsSaving ? "Guardando..." : _editingOrderId.HasValue ? "Guardar cambios" : "Confirmar pedido";
    public bool IsEditing => _editingOrderId.HasValue;
    public bool IsInternalProduction
    {
        get => _isInternalProduction;
        set
        {
            if (SetProperty(ref _isInternalProduction, value))
            {
                OnPropertyChanged(nameof(IsRegularCustomerOrder));
                ApplyInternalProductionState();
                RefreshDiscountsAndTotals();
            }
        }
    }

    public bool IsRegularCustomerOrder => !IsInternalProduction;
    public bool IsBusy => IsLoading || IsSaving;
    public bool HasItems => OrderItems.Count > 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public int TotalPieces => OrderItems.Sum(item => item.Quantity);
    public decimal Subtotal => IsInternalProduction ? 0 : OrderItems.Sum(item => item.Subtotal);
    public decimal DiscountAmount => IsInternalProduction ? 0 : OrderItems.Sum(item => item.DiscountAmount);
    public decimal Total => IsInternalProduction ? 0 : Subtotal - DiscountAmount;
    public string TotalPiecesText => $"{TotalPieces:N0} piezas";
    public string SubtotalText => Subtotal.ToString("C2");
    public string DiscountAmountText => DiscountAmount.ToString("C2");
    public string TotalText => Total.ToString("C2");

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(SaveButtonText));
                if (SaveCommand is Command command)
                {
                    command.ChangeCanExecute();
                }
            }
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (SetProperty(ref _message, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                FilterProducts();
            }
        }
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public OrderStatusDto? SelectedOrderStatus
    {
        get => _selectedOrderStatus;
        set => SetProperty(ref _selectedOrderStatus, value);
    }

    public PaymentStatusDto? SelectedPaymentStatus
    {
        get => _selectedPaymentStatus;
        set => SetProperty(ref _selectedPaymentStatus, value);
    }

    public LookupOption? SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set => SetProperty(ref _selectedPaymentMethod, value);
    }

    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (SetProperty(ref _productSearchText, value))
            {
                FilterProducts();
            }
        }
    }

    public string Comments
    {
        get => _comments;
        set => SetProperty(ref _comments, value);
    }

    public async Task InitializeAsync(int? orderId)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            Message = string.Empty;
            if (!_catalogsLoaded)
            {
                await LoadCatalogsAsync();
                _catalogsLoaded = true;
            }

            if (orderId.HasValue)
            {
                await LoadOrderAsync(orderId.Value);
            }
            else if (!_editingOrderId.HasValue)
            {
                ResetForm();
            }
        }
        catch
        {
            Message = "No fue posible cargar el formulario de pedidos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCatalogsAsync()
    {
        Categories.Clear();
        Products.Clear();
        FilteredProducts.Clear();
        Customers.Clear();
        OrderStatuses.Clear();
        PaymentStatuses.Clear();
        Discounts.Clear();
        DiscountLimits.Clear();
        _categoryNamesById.Clear();

        foreach (var category in (await _categoryService.GetAllAsync()).Where(category => category.IsActive).OrderBy(category => category.DisplayOrder).ThenBy(category => category.Name))
        {
            Categories.Add(category);
            _categoryNamesById[category.CategoryId] = category.Name;
        }

        foreach (var product in (await _productService.GetAllAsync()).Where(product => product.IsActive).OrderBy(product => product.Name))
        {
            Products.Add(product);
        }

        foreach (var customer in (await _customerService.GetAllAsync()).Where(customer => customer.IsActive).OrderBy(customer => customer.FullName))
        {
            customer.FullName = Normalize(customer.FullName);
            Customers.Add(customer);
        }

        foreach (var status in (await _orderStatusService.GetAllAsync()).Where(status => status.IsActive))
        {
            status.DisplayName = string.IsNullOrWhiteSpace(status.DisplayName) ? status.OrderStatusCode : status.DisplayName.Trim();
            OrderStatuses.Add(status);
        }

        foreach (var status in (await _paymentStatusService.GetAllAsync()).Where(status => status.IsActive))
        {
            status.DisplayName = string.IsNullOrWhiteSpace(status.DisplayName) ? status.PaymentStatusCode : status.DisplayName.Trim();
            PaymentStatuses.Add(status);
        }

        foreach (var discount in (await _discountRuleService.GetAllAsync()).Where(discount => discount.IsActive).GroupBy(discount => discount.DiscountRuleId).Select(group => group.First()))
        {
            Discounts.Add(new MobileDiscountOptionViewModel(discount, RefreshDiscountsAndTotals));
        }

        foreach (var limit in (await _discountLimitRuleService.GetAllAsync()).GroupBy(limit => limit.DiscountLimitRuleId).Select(group => group.First()))
        {
            DiscountLimits.Add(limit);
        }

        SelectedCategory = Categories.FirstOrDefault();
        SelectedProduct = FilteredProducts.FirstOrDefault();
    }

    private void ApplyInternalProductionState()
    {
        if (!IsInternalProduction)
        {
            return;
        }

        SelectedCustomer = null;
        SelectedPaymentStatus = PaymentStatuses.FirstOrDefault(status =>
            CodesEqual(status.PaymentStatusCode, OrderCatalog.NotApplicablePaymentStatusCode));
        SelectedPaymentMethod = null;
        foreach (var discount in Discounts)
        {
            discount.IsSelected = false;
        }
    }

    private async Task LoadOrderAsync(int orderId)
    {
        var order = await _orderService.GetCustomOrderByIdAsync(orderId);
        if (order == null)
        {
            Message = $"No se encontro el pedido #{orderId}.";
            return;
        }

        _editingOrderId = order.OrderId;
        _editingCreatedAtUtc = order.CreatedAt;
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(IsEditing));

        IsInternalProduction = order.IsInternalProduction;
        SelectedCustomer = Customers.FirstOrDefault(customer => customer.CustomerId == order.CustomerId);
        SelectedOrderStatus = FindOrderStatus(order.OrderStatusCode);
        SelectedPaymentStatus = FindPaymentStatus(order.PaymentStatusCode);
        SelectedPaymentMethod = PaymentMethods.FirstOrDefault(method => CodesEqual(method.Code, order.PaymentMethod));
        ProductionStartDate = ToLocalDisplayDate(order.ProductionStartDate);
        ProductionEndDate = ToLocalDisplayDate(order.ProductionEndDate);
        DeliveryDate = ToLocalDisplayDate(order.DeliveryDate);
        DeliveryTime = ToLocalDisplayTime(order.DeliveryDate);
        Comments = order.Comments ?? string.Empty;
        OnPropertyChanged(nameof(ProductionStartDate));
        OnPropertyChanged(nameof(ProductionEndDate));
        OnPropertyChanged(nameof(DeliveryDate));
        OnPropertyChanged(nameof(DeliveryTime));

        OrderItems.Clear();
        foreach (var item in order.OrderItems.OrderBy(item => ResolveCategoryName(GetProduct(item.ProductId)?.CategoryId ?? 0)).ThenBy(item => GetProduct(item.ProductId)?.Name))
        {
            var product = GetProduct(item.ProductId);
            if (product == null)
            {
                continue;
            }

            AddLine(product, item.Quantity);
        }

        foreach (var option in Discounts)
        {
            option.IsSelected = order.OrderDiscounts.Any(discount => discount.DiscountRuleId == option.DiscountRuleId);
        }

        RefreshDiscountsAndTotals();
    }

    private void AddSelectedProduct()
    {
        if (SelectedProduct == null)
        {
            Message = "Selecciona un producto.";
            return;
        }

        var existing = OrderItems.FirstOrDefault(item => item.ProductId == SelectedProduct.ProductId);
        if (existing != null)
        {
            existing.Quantity += 1;
            MoveLineToTop(existing);
        }
        else
        {
            AddLine(SelectedProduct, 1, insertOnTop: true);
        }

        RefreshDiscountsAndTotals();
    }

    private void AddLine(ProductDto product, int quantity, bool insertOnTop = false)
    {
        var line = new MobileOrderLineViewModel(RemoveLine)
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            CategoryName = ResolveCategoryName(product.CategoryId),
            ProductName = product.Name,
            UnitPrice = product.SalePrice,
            Quantity = Math.Max(1, quantity)
        };
        line.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MobileOrderLineViewModel.Quantity))
            {
                RefreshDiscountsAndTotals();
            }
        };

        if (insertOnTop)
        {
            OrderItems.Insert(0, line);
        }
        else
        {
            OrderItems.Add(line);
        }
    }

    private async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        if (!Validate(out var validationMessage))
        {
            Message = validationMessage;
            return;
        }

        IsSaving = true;
        try
        {
            RefreshDiscountsAndTotals();
            var order = new OrderDto
            {
                OrderId = _editingOrderId ?? 0,
                BranchId = DefaultBranchId,
                UserId = DefaultUserId,
                CustomerId = IsInternalProduction ? null : SelectedCustomer!.CustomerId,
                Pieces = TotalPieces,
                DiscountAmount = DiscountAmount,
                TotalAmount = Total,
                ProductionStartDate = ToUtcFromLocalDate(ProductionStartDate),
                ProductionEndDate = ToUtcFromLocalDate(ProductionEndDate),
                DeliveryDate = ToUtcFromLocalDateTime(DeliveryDate, DeliveryTime),
                OrderStatusCode = SelectedOrderStatus!.OrderStatusCode,
                PaymentStatusCode = IsInternalProduction ? OrderCatalog.NotApplicablePaymentStatusCode : SelectedPaymentStatus!.PaymentStatusCode,
                PaymentMethod = IsInternalProduction ? OrderCatalog.InternalPaymentMethodCode : SelectedPaymentMethod!.Code,
                SalesChannel = OrderCatalog.CustomOrderSalesChannelCode,
                Comments = string.IsNullOrWhiteSpace(Comments) ? null : Comments.Trim(),
                IsDirectSale = false,
                IsInternalProduction = IsInternalProduction,
                CreatedAt = _editingOrderId.HasValue ? _editingCreatedAtUtc : DateTime.UtcNow,
                UpdatedAt = _editingOrderId.HasValue ? DateTime.UtcNow : null,
                OrderItems = OrderItems.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = IsInternalProduction ? 0 : item.UnitPrice,
                    DiscountPerUnit = IsInternalProduction ? 0 : item.DiscountPerUnit,
                    DiscountAmount = IsInternalProduction ? 0 : item.DiscountAmount
                }).ToList()
            };

            foreach (var selectedDiscount in GetSelectedDiscounts())
            {
                order.OrderDiscounts.Add(new OrderDiscountDto
                {
                    DiscountRuleId = selectedDiscount.DiscountRuleId,
                    AppliedAmount = OrderItems.Sum(item => DiscountCalculator.CalculateDiscountPerUnit(
                        new DiscountLineInput(item.ProductId, item.CategoryId, item.Quantity, item.UnitPrice),
                        new[] { selectedDiscount },
                        DiscountLimits,
                        TotalPieces,
                        Subtotal,
                        DateOnly.FromDateTime(DateTime.Today)) * item.Quantity)
                });
            }

            var success = _editingOrderId.HasValue
                ? await _orderService.UpdateAsync(order)
                : (await _orderService.CreateAsync(order)).HasValue;

            if (!success)
            {
                Message = "No fue posible guardar el pedido.";
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            Message = "Ocurrio un error al guardar el pedido.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool Validate(out string message)
    {
        if (!IsInternalProduction && SelectedCustomer == null)
        {
            message = "Selecciona un cliente.";
            return false;
        }

        if (!OrderItems.Any())
        {
            message = "Agrega al menos un producto.";
            return false;
        }

        if (SelectedOrderStatus == null || (!IsInternalProduction && (SelectedPaymentStatus == null || SelectedPaymentMethod == null)))
        {
            message = "Selecciona estatus del pedido, estatus de pago y metodo de pago.";
            return false;
        }

        if (ProductionEndDate.Date < ProductionStartDate.Date)
        {
            message = "La fecha final no puede ser menor que la fecha inicial.";
            return false;
        }

        if (DeliveryDate.Date < ProductionEndDate.Date)
        {
            message = "La entrega no puede ser menor que el fin de produccion.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void ResetForm()
    {
        _editingOrderId = null;
        _editingCreatedAtUtc = default;
        ProductionStartDate = DateTime.Today;
        ProductionEndDate = DateTime.Today;
        DeliveryDate = DateTime.Today;
        DeliveryTime = DateTime.Now.TimeOfDay;
        Comments = string.Empty;
        ProductSearchText = string.Empty;
        SelectedCustomer = null;
        SelectedOrderStatus = OrderStatuses.FirstOrDefault();
        SelectedPaymentStatus = PaymentStatuses.FirstOrDefault();
        SelectedPaymentMethod = null;
        IsInternalProduction = false;
        SelectedCategory = Categories.FirstOrDefault();
        OrderItems.Clear();
        foreach (var discount in Discounts)
        {
            discount.IsSelected = false;
        }

        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(ProductionStartDate));
        OnPropertyChanged(nameof(ProductionEndDate));
        OnPropertyChanged(nameof(DeliveryDate));
        OnPropertyChanged(nameof(DeliveryTime));
        RefreshTotals();
    }

    private void RemoveLine(MobileOrderLineViewModel line)
    {
        OrderItems.Remove(line);
        RefreshDiscountsAndTotals();
    }

    private void MoveLineToTop(MobileOrderLineViewModel line)
    {
        var index = OrderItems.IndexOf(line);
        if (index > 0)
        {
            OrderItems.Move(index, 0);
        }
    }

    private void FilterProducts()
    {
        var selectedCategoryId = SelectedCategory?.CategoryId;
        var search = ProductSearchText.Trim();
        var products = Products
            .Where(product => !selectedCategoryId.HasValue || product.CategoryId == selectedCategoryId)
            .Where(product => string.IsNullOrWhiteSpace(search) || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(product => product.Name)
            .ToList();

        FilteredProducts.Clear();
        foreach (var product in products)
        {
            FilteredProducts.Add(product);
        }

        SelectedProduct = FilteredProducts.FirstOrDefault();
    }

    private void RefreshDiscountsAndTotals()
    {
        var selectedDiscounts = GetSelectedDiscounts().ToList();
        foreach (var item in OrderItems)
        {
            item.DiscountPerUnit = DiscountCalculator.CalculateDiscountPerUnit(
                new DiscountLineInput(item.ProductId, item.CategoryId, item.Quantity, item.UnitPrice),
                selectedDiscounts,
                DiscountLimits,
                TotalPieces,
                Subtotal,
                DateOnly.FromDateTime(DateTime.Today));
        }

        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(TotalPieces));
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalPiecesText));
        OnPropertyChanged(nameof(SubtotalText));
        OnPropertyChanged(nameof(DiscountAmountText));
        OnPropertyChanged(nameof(TotalText));
    }

    private IEnumerable<DiscountRuleDto> GetSelectedDiscounts()
    {
        if (IsInternalProduction)
        {
            return Enumerable.Empty<DiscountRuleDto>();
        }

        return Discounts.Where(option => option.IsSelected).Select(option => option.Discount);
    }

    private ProductDto? GetProduct(int productId)
    {
        return Products.FirstOrDefault(product => product.ProductId == productId);
    }

    private string ResolveCategoryName(int categoryId)
    {
        return _categoryNamesById.TryGetValue(categoryId, out var name) ? name : string.Empty;
    }

    private OrderStatusDto? FindOrderStatus(string? code)
    {
        return OrderStatuses.FirstOrDefault(status => CodesEqual(status.OrderStatusCode, code));
    }

    private PaymentStatusDto? FindPaymentStatus(string? code)
    {
        return PaymentStatuses.FirstOrDefault(status => CodesEqual(status.PaymentStatusCode, code));
    }

    private static DateTime ToUtcFromLocalDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Local).ToUniversalTime();
    }

    private static DateTime ToUtcFromLocalDateTime(DateTime date, TimeSpan time)
    {
        return DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Local).ToUniversalTime();
    }

    private static DateTime ToLocalDisplayDate(DateTime? utcDateTime)
    {
        return ToLocalDisplayDateTime(utcDateTime).Date;
    }

    private static TimeSpan ToLocalDisplayTime(DateTime? utcDateTime)
    {
        var dateTime = ToLocalDisplayDateTime(utcDateTime);
        return dateTime == DateTime.MinValue ? DateTime.Now.TimeOfDay : dateTime.TimeOfDay;
    }

    private static DateTime ToLocalDisplayDateTime(DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue)
        {
            return DateTime.Today;
        }

        var value = utcDateTime.Value.Kind == DateTimeKind.Utc
            ? utcDateTime.Value
            : DateTime.SpecifyKind(utcDateTime.Value, DateTimeKind.Utc);
        return value.ToLocalTime();
    }

    private static string Normalize(string value)
    {
        return string.Join(' ', value.Trim().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool CodesEqual(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
