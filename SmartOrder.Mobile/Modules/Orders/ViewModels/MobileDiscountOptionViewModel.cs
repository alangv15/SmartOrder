using SmartOrder.Entities.Orders.Models;
using SmartOrder.Mobile.Shared;

namespace SmartOrder.Mobile.Modules.Orders.ViewModels;

public sealed class MobileDiscountOptionViewModel : ObservableObject
{
    private bool _isSelected;

    public MobileDiscountOptionViewModel(DiscountRuleDto discount, Action selectionChanged)
    {
        Discount = discount;
        SelectionChanged = selectionChanged;
    }

    public DiscountRuleDto Discount { get; }
    public int DiscountRuleId => Discount.DiscountRuleId;
    public string Name => Discount.Name;
    private Action SelectionChanged { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                SelectionChanged();
            }
        }
    }
}
