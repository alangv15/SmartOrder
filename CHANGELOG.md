# Historial de cambios

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto sigue [Versionado Semántico](https://semver.org/lang/es/).

## [1.1.0] - 2026-09-19

### Agregado

- Administración de conceptos de costo, recetas base y recetas de producto.
- Consulta de productos con información faltante para calcular su costo.
- Reporte de rentabilidad de los últimos seis meses con ingresos, efectivo, tarjeta, costos, utilidad bruta y margen bruto.
- Presentación horizontal del reporte mensual, con los meses ordenados del más antiguo al más reciente.
- Configuración y herramientas de impresión para tickets y pedidos.
- Versión de la aplicación visible en el login y en el menú lateral, obtenida directamente de los metadatos del paquete.

### Cambiado

- El reporte acumulado de ventas ahora integra ventas de mostrador y pedidos.
- Ventas y pedidos conservan la información de precio, receta, costo unitario, costo total y utilidad correspondiente a la operación.
- La aplicación de Windows se publica como paquete autónomo x64 con .NET y Windows App SDK incluidos.
- La versión global se actualizó a 1.1.0.

### Corregido

- Receta de focaccia de tomate y queso: 25 g de manchego y 37.5 g de tomate por pieza.
- Eliminado el aviso recurrente de instalación del runtime en los equipos de operación.

## [1.0.0] - 2026-04-07

### Agregado

- Primera versión operativa de SmartOrder para Windows.
- Inicio de sesión y control de acceso por usuarios, roles y permisos.
- Operación de ventas de mostrador y pedidos.
- Catálogos de sucursales, categorías, productos y clientes.
- Reportes básicos de ventas.

