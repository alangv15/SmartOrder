using SmartOrder.Mobile.Shared;
using System.Windows.Input;

namespace SmartOrder.Mobile.Modules.Orders.ViewModels;

public sealed class MobileOrderLineViewModel : ObservableObject
{
    private int _quantity;
    private decimal _discountPerUnit;

    public MobileOrderLineViewModel(Action<MobileOrderLineViewModel> removeAction)
    {
        RemoveCommand = new Command(() => removeAction(this));
    }

    public int ProductId { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public ICommand RemoveCommand { get; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            var normalized = Math.Max(1, value);
            if (SetProperty(ref _quantity, normalized))
            {
                RefreshTotals();
            }
        }
    }

    public decimal DiscountPerUnit
    {
        get => _discountPerUnit;
        set
        {
            if (SetProperty(ref _discountPerUnit, value))
            {
                RefreshTotals();
            }
        }
    }

    public decimal Subtotal => UnitPrice * Quantity;
    public decimal DiscountAmount => DiscountPerUnit * Quantity;
    public decimal Total => Subtotal - DiscountAmount;
    public string PriceText => UnitPrice.ToString("C2");
    public string SubtotalText => Subtotal.ToString("C2");
    public string DiscountText => DiscountAmount.ToString("C2");
    public string TotalText => Total.ToString("C2");

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(SubtotalText));
        OnPropertyChanged(nameof(DiscountText));
        OnPropertyChanged(nameof(TotalText));
    }
}
