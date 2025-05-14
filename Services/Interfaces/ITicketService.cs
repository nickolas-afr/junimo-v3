using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface ITicketService
    {
        Task CreateTicket(Ticket ticket);

        Task<IEnumerable<Ticket>> GetUserTicketsAsync(string userId);
        Task<Ticket> GetUserTicketByIdAsync(string userId, int ticketId);
        Task UpdateTicketAsync(Ticket ticket);
    }
}
