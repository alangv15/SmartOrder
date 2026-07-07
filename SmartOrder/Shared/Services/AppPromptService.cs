namespace SmartOrder.Shared.Services;

public sealed class AppPromptOptions
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string Accept { get; init; } = "Aceptar";
    public string Cancel { get; init; } = "Cancelar";
    public string Placeholder { get; init; } = string.Empty;
    public string InitialValue { get; init; } = string.Empty;
    public int MaxLength { get; init; } = -1;
    public Keyboard Keyboard { get; init; } = Keyboard.Default;
}

public static class AppPromptService
{
    public static async Task<string?> PromptAsync(AppPromptOptions options)
    {
        var currentPage = GetCurrentPage();
        if (currentPage == null)
        {
            return null;
        }

        return await MainThread.InvokeOnMainThreadAsync(() =>
            currentPage.DisplayPromptAsync(
                options.Title,
                options.Message,
                options.Accept,
                options.Cancel,
                options.Placeholder,
                options.MaxLength,
                options.Keyboard,
                options.InitialValue));
    }

    private static Page? GetCurrentPage()
    {
        var rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        return UnwrapPage(rootPage);
    }

    private static Page? UnwrapPage(Page? page)
    {
        return page switch
        {
            Microsoft.Maui.Controls.Shell shell => shell.CurrentPage,
            NavigationPage navigationPage => navigationPage.CurrentPage,
            TabbedPage tabbedPage => tabbedPage.CurrentPage,
            FlyoutPage flyoutPage => UnwrapPage(flyoutPage.Detail),
            _ => page
        };
    }
}
