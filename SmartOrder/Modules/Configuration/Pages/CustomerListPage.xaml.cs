using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class CustomerListPage : ContentPage
{
    private readonly CustomerService _customerService;

    public CustomerListPage(CustomerService customerService)
    {
        InitializeComponent();
        _customerService = customerService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers = await _customerService.GetAllAsync();
            CustomerList.ItemsSource = customers
                .OrderBy(customer => customer.FullName)
                .Select(customer => new CustomerListItemViewModel(customer))
                .ToList();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CustomerListPage.LoadCustomersAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los clientes",
                Message = "Ocurrio un error al consultar los clientes. Revisa la conexion con el API."
            });
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CustomerFormPage(_customerService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CustomerListItemViewModel selected)
        {
            await Navigation.PushAsync(new CustomerFormPage(_customerService, selected.CustomerId));
            CustomerList.SelectedItem = null;
        }
    }

    private sealed class CustomerListItemViewModel
    {
        public CustomerListItemViewModel(CustomerDto customer)
        {
            CustomerId = customer.CustomerId;
            FullName = customer.FullName;
            Phone = customer.Phone;
            EmailSummary = string.IsNullOrWhiteSpace(customer.Email) ? "Sin correo registrado" : customer.Email;
            AddressSummary = BuildAddressSummary(customer);
            CreatedAt = customer.CreatedAt;
            IsActive = customer.IsActive;
        }

        public int CustomerId { get; }
        public string FullName { get; }
        public string Phone { get; }
        public string EmailSummary { get; }
        public string AddressSummary { get; }
        public DateTime CreatedAt { get; }
        public bool IsActive { get; }

        private static string BuildAddressSummary(CustomerDto customer)
        {
            var parts = new[]
            {
                customer.Address,
                customer.Neighborhood,
                customer.City,
                customer.State,
                customer.PostalCode
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            var summary = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(summary) ? "Sin direccion registrada" : summary;
        }
    }
}
