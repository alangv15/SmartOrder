namespace SmartOrder.Entities.Configuration.Models;

public sealed record CostingOption(string Code, string Name);

public static class CostingCatalog
{
    public const string RawMaterial = "RawMaterial";
    public const string Packaging = "Packaging";
    public const string Resale = "Resale";
    public const string Service = "Service";

    public static IReadOnlyList<CostingOption> Types { get; } = Array.AsReadOnly(new[]
    {
        new CostingOption(RawMaterial, "Materia prima"),
        new CostingOption(Packaging, "Empaque"),
        new CostingOption(Resale, "Reventa"),
        new CostingOption(Service, "Servicio")
    });

    public static IReadOnlyList<CostingOption> Units { get; } = Array.AsReadOnly(new[]
    {
        new CostingOption("Gram", "Gramo"),
        new CostingOption("Piece", "Pieza"),
        new CostingOption("Application", "Aplicacion")
    });

    public static bool IsValidCombination(string type, string unit) => type switch
    {
        RawMaterial => unit == "Gram",
        Packaging or Resale => unit == "Piece",
        Service => unit == "Application",
        _ => false
    };

    public static bool IsAllowedInBaseRecipe(string type) =>
        type is RawMaterial or Packaging or Service;

    public static string TypeName(string code) => Types.FirstOrDefault(item => item.Code == code)?.Name ?? code;
    public static string UnitName(string code) => Units.FirstOrDefault(item => item.Code == code)?.Name ?? code;
}
