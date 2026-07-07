using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class CustomerFormPage : ContentPage
{
    private readonly CustomerService _customerService;
    private readonly int _customerId;
    private CustomerDto _customer = new();

    public CustomerFormPage(CustomerService customerService, int customerId = 0)
    {
        InitializeComponent();
        _customerService = customerService;
        _customerId = customerId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_customerId > 0)
        {
            await LoadCustomerAsync();
        }
    }

    private async Task LoadCustomerAsync()
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(_customerId);
            if (customer is null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Cliente no encontrado",
                    Message = "No fue posible cargar la informacion del cliente seleccionado."
                });
                await Navigation.PopAsync();
                return;
            }

            _customer = customer;
            PageTitleLabel.Text = "Editar cliente";
            DeactivateButton.IsVisible = customer.IsActive;

            FullNameEntry.Text = customer.FullName;
            PhoneEntry.Text = customer.Phone;
            EmailEntry.Text = customer.Email;
            AddressEntry.Text = customer.Address;
            NeighborhoodEntry.Text = customer.Neighborhood;
            CityEntry.Text = customer.City;
            StateEntry.Text = customer.State;
            PostalCodeEntry.Text = customer.PostalCode;
            IsActiveSwitch.IsToggled = customer.IsActive;
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CustomerFormPage.LoadCustomerAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir el cliente",
                Message = "Ocurrio un error al consultar los datos del cliente."
            });
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!TryBuildCustomer(out var message))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Faltan datos",
                Message = message
            });
            return;
        }

        try
        {
            var success = false;
            var errors = Enumerable.Empty<string>();

            if (_customerId == 0)
            {
                var response = await _customerService.CreateAsync(_customer);
                success = response?.Success == true;
                errors = response?.Errors ?? Enumerable.Empty<string>();

                if (response?.Data > 0)
                {
                    _customer.CustomerId = response.Data;
                }
            }
            else
            {
                var response = await _customerService.UpdateAsync(_customer);
                success = response?.Success == true;
                errors = response?.Errors ?? Enumerable.Empty<string>();
            }

            if (success)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Cliente guardado",
                    Message = _customerId == 0
                        ? $"El cliente se creo correctamente con folio {_customer.CustomerId}."
                        : "Los cambios del cliente se guardaron correctamente."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo guardar",
                Message = errors.FirstOrDefault() ?? "El API no devolvio una respuesta valida al guardar el cliente."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CustomerFormPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar el cliente",
                Message = "Ocurrio un error inesperado al guardar el cliente."
            });
        }
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_customerId <= 0)
        {
            return;
        }

        try
        {
            var response = await _customerService.DeactivateAsync(_customerId);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Cliente desactivado",
                    Message = "El cliente dejo de estar disponible para nuevos pedidos."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo desactivar",
                Message = response?.Errors.FirstOrDefault() ?? "No fue posible desactivar el cliente."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CustomerFormPage.OnDeactivateClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al desactivar",
                Message = "Ocurrio un error inesperado al desactivar el cliente."
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private bool TryBuildCustomer(out string message)
    {
        if (string.IsNullOrWhiteSpace(FullNameEntry.Text))
        {
            message = "Captura el nombre completo del cliente.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PhoneEntry.Text))
        {
            message = "Captura el telefono del cliente.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            message = "Captura el correo del cliente.";
            return false;
        }

        _customer.FullName = FullNameEntry.Text.Trim();
        _customer.Phone = PhoneEntry.Text.Trim();
        _customer.Email = EmailEntry.Text.Trim();
        _customer.Address = GetTrimmedValue(AddressEntry.Text);
        _customer.Neighborhood = GetTrimmedValue(NeighborhoodEntry.Text);
        _customer.City = GetTrimmedValue(CityEntry.Text);
        _customer.State = GetTrimmedValue(StateEntry.Text);
        _customer.PostalCode = GetTrimmedValue(PostalCodeEntry.Text);
        _customer.IsActive = IsActiveSwitch.IsToggled;

        message = string.Empty;
        return true;
    }

    private static string? GetTrimmedValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
