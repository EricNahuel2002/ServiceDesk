namespace ServiceDesk.Domain.Identity;

public static class Roles
{
    public const string Cliente = "Cliente";

    public const string Tecnico = "Tecnico";

    public const string Administrador = "Administrador";

    public static readonly string[] All =
    [
        Cliente,
        Tecnico,
        Administrador
    ];
}
