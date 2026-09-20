using System.Runtime.InteropServices;

namespace SmartOrder.Shared.Printing;

public sealed class WindowsPrinterTicketWriter
{
    public Task WriteAsync(string printerName, byte[] payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("No se configuro el nombre de la impresora.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        WriteRaw(printerName.Trim(), payload);
        return Task.CompletedTask;
    }

    private static void WriteRaw(string printerName, byte[] payload)
    {
        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
        {
            ThrowLastWin32Error($"No se pudo abrir la impresora {printerName}.");
        }

        try
        {
            var documentInfo = new DocInfo
            {
                DocumentName = "SmartOrder Ticket",
                OutputFile = null,
                DataType = "RAW"
            };

            if (!StartDocPrinter(printerHandle, 1, documentInfo))
            {
                ThrowLastWin32Error("No se pudo iniciar el documento de impresion.");
            }

            try
            {
                if (!StartPagePrinter(printerHandle))
                {
                    ThrowLastWin32Error("No se pudo iniciar la pagina de impresion.");
                }

                try
                {
                    var unmanagedPayload = Marshal.AllocCoTaskMem(payload.Length);
                    try
                    {
                        Marshal.Copy(payload, 0, unmanagedPayload, payload.Length);
                        if (!WritePrinter(printerHandle, unmanagedPayload, payload.Length, out var bytesWritten)
                            || bytesWritten != payload.Length)
                        {
                            ThrowLastWin32Error("No se pudo enviar el ticket completo a la impresora.");
                        }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(unmanagedPayload);
                    }
                }
                finally
                {
                    EndPagePrinter(printerHandle);
                }
            }
            finally
            {
                EndDocPrinter(printerHandle);
            }
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    private static void ThrowLastWin32Error(string message)
    {
        throw new InvalidOperationException($"{message} Codigo Windows: {Marshal.GetLastWin32Error()}.");
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr printerHandle, int level, [In] DocInfo documentInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr printerHandle, IntPtr buffer, int count, out int written);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printerHandle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        public string? DocumentName;
        public string? OutputFile;
        public string? DataType;
    }
}

