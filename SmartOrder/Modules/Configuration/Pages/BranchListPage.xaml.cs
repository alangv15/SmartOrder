using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class BranchListPage : ContentPage
{
    private readonly BranchService _branchService;

    public BranchListPage(BranchService branchService)
    {
        InitializeComponent();
        _branchService = branchService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var branches = await _branchService.GetAllAsync();
            BranchList.ItemsSource = branches
                .Select(branch => new BranchListItemViewModel(branch))
                .ToList();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("BranchListPage.LoadBranchesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar las sucursales",
                Message = "Ocurrió un error al consultar las sucursales. Revisa la conexión con el API."
            });
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BranchFormPage(_branchService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is BranchListItemViewModel selected)
        {
            await Navigation.PushAsync(new BranchFormPage(_branchService, selected.BranchId));
            BranchList.SelectedItem = null;
        }
    }

    private sealed class BranchListItemViewModel
    {
        public BranchListItemViewModel(BranchDto branch)
        {
            BranchId = branch.BranchId;
            Name = branch.Name;
            Code = branch.Code;
            IsActive = branch.IsActive;
            CreatedAt = branch.CreatedAt;
            AddressSummary = BuildAddressSummary(branch);
            ContactSummary = BuildContactSummary(branch);
        }

        public int BranchId { get; }
        public string Name { get; }
        public string Code { get; }
        public bool IsActive { get; }
        public DateTime CreatedAt { get; }
        public string AddressSummary { get; }
        public string ContactSummary { get; }

        private static string BuildAddressSummary(BranchDto branch)
        {
            var parts = new[]
            {
                branch.Address,
                branch.City,
                branch.State,
                branch.PostalCode
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(", ", parts).Trim();
        }

        private static string BuildContactSummary(BranchDto branch)
        {
            var parts = new[]
            {
                branch.Phone,
                branch.Email
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" | ", parts).Trim();
        }
    }
}
