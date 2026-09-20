# SmartOrder Desktop

Aplicación de punto de venta para Windows utilizada para ventas de mostrador, pedidos, clientes, impresión, configuración, seguridad y reportes operativos de SmartOrder.

**Versión actual:** 1.1.0  
**Rama de entrega:** `release/1.1.0-costos-reportes`

## Funcionalidad principal

- Registro de ventas de mostrador y pedidos.
- Administración de productos, categorías, sucursales y clientes.
- Catálogo de conceptos de costo, recetas base y recetas de producto.
- Cálculo y consulta del costo de productos.
- Reportes de ventas acumuladas y rentabilidad mensual.
- Administración de usuarios, roles y permisos.
- Impresión de tickets y documentos de pedido.

## Estructura de la solución

| Proyecto | Responsabilidad |
| --- | --- |
| `SmartOrder` | Interfaz .NET MAUI para Windows y composición de la aplicación. |
| `SmartOrder.Business` | Servicios de negocio y comunicación con la API. |
| `SmartOrder.Entities` | Contratos, DTO y modelos compartidos. |
| `SmartOrder.Mobile` | Trabajo relacionado con la variante móvil. |

## Requisitos de desarrollo

- Windows 10 versión 1809 o posterior, o Windows 11.
- .NET SDK 10.
- Carga de trabajo de .NET MAUI para Windows.
- WebView2 Runtime.
- Acceso a una instancia de SmartOrder API.

La distribución de producción para Windows x64 es autónoma e incluye .NET y Windows App SDK. Los equipos de operación no necesitan instalar el runtime de .NET por separado.

## Configuración

1. Copiar `SmartOrder/appsettings.example.json` como `SmartOrder/appsettings.json`.
2. Configurar `ApiSettings:BaseUrl` con la URL de la API.
3. Revisar `PrinterSettings` para la impresora de tickets del equipo.

No se deben incluir contraseñas, tokens ni datos sensibles en archivos versionados. Mantenga la configuración de cada ambiente fuera de Git.

## Compilación y ejecución

```powershell
dotnet restore SmartOrder.sln
dotnet build SmartOrder.sln -c Release
dotnet run --project SmartOrder/SmartOrder.csproj -f net10.0-windows10.0.19041.0
```

## Publicación para Windows x64

```powershell
dotnet publish SmartOrder/SmartOrder.csproj `
  -c Release `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  --self-contained true `
  -p:WindowsPackageType=None `
  -p:WindowsAppSDKSelfContained=true `
  -p:PublishSingleFile=false
```

Antes de publicar en producción:

1. Confirmar que `appsettings.json` apunta al ambiente correcto.
2. Generar un respaldo de la versión instalada.
3. Cerrar las instancias abiertas de SmartOrder.
4. Copiar la publicación completa; no copiar únicamente el ejecutable.
5. Verificar inicio de sesión, venta, pedido, impresión y reportes.

## Versionado

El proyecto utiliza versionado semántico: `MAJOR.MINOR.PATCH`. La versión se define de forma central en `Directory.Build.props`; la versión visible de MAUI se establece en `SmartOrder/SmartOrder.csproj`. El login y el menú leen la versión instalada mediante `AppInfo`, evitando textos duplicados.

Consulte [CHANGELOG.md](CHANGELOG.md) para conocer los cambios de cada entrega.

## Repositorios relacionados

- `SmartOrderAPI`: servicios HTTP y reglas del servidor.
- `SmartOrderDB`: esquema, migraciones y cargas de datos.

