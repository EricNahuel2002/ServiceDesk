namespace ServiceDesk.Domain;

public enum TicketStatus : byte
{
    Nuevo = 1,
    EnEspera = 2,
    EnProgreso = 3,
    Resuelto = 4,
    Cancelado = 5
}