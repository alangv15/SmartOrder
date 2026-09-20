using System.ComponentModel;
using System.Windows.Input;

namespace SmartOrder.Modules.Orders.ViewModels
{
    public class OrderItemViewModel : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _discountPerUnit;

        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal UnitCost { get; set; }
        public int? ProductRecipeId { get; set; }
        public int? ProductPriceId { get; set; }
        public DateTime? CostCalculatedAt { get; set; }
        public bool HasCurrentCost => ProductRecipeId.HasValue && UnitCost > 0;
        public decimal DiscountPerUnit
        {
            get => _discountPerUnit;
            set
            {
                if (_discountPerUnit != value)
                {
                    _discountPerUnit = value;
                    OnPropertyChanged(nameof(DiscountPerUnit));
                    OnPropertyChanged(nameof(DiscountAmount));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                var normalizedValue = value < 1 ? 1 : value;
                if (_quantity != normalizedValue)
                {
                    _quantity = normalizedValue;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(DiscountAmount));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public decimal Subtotal => Price * Quantity;
        public decimal DiscountAmount => DiscountPerUnit * Quantity;
        public decimal Total => Subtotal - DiscountAmount;

        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }
        public ICommand RemoveCommand { get; }

        public OrderItemViewModel(Action<OrderItemViewModel> removeAction)
        {
            IncreaseCommand = new Command(() => Quantity++);
            DecreaseCommand = new Command(() =>
            {
                if (Quantity > 1)
                {
                    Quantity--;
                }
            });
            RemoveCommand = new Command(() => removeAction(this));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
