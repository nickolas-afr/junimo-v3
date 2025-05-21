using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class TicketService : ITicketService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public TicketService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task CreateTicket(Ticket ticket)
        {
            _repositoryWrapper.Ticket.Create(ticket);
            await _repositoryWrapper.SaveAsync();
        }
        
        public async Task<IEnumerable<Ticket>> GetUserTicketsAsync(string userId)
        {
            var tickets = await _repositoryWrapper.Ticket.GetAllAsync();
            return tickets.Where(t => t.userId == userId);
        }

        public async Task<Ticket> GetUserTicketByIdAsync(string userId, int ticketId)
        {
            var tickets = await _repositoryWrapper.Ticket.GetAllAsync();
            return tickets.Where(t => t.userId == userId && t.ticketId == ticketId).FirstOrDefault();
        }

        public async Task UpdateTicketAsync(Ticket ticket)
        {
            _repositoryWrapper.Ticket.Update(ticket);
            await _repositoryWrapper.SaveAsync();
        }
        
        // Implement the missing methods for admin functionality

        public async Task<IEnumerable<Ticket>> GetAllTicketsAsync()
        {
            // Get all tickets including user details for displaying username
            var tickets = await _repositoryWrapper.Ticket.FindAll()
                .Include(t => t.User)
                .ToListAsync();
            
            return tickets;
        }

        public async Task<Ticket> GetTicketByIdAsync(int ticketId)
        {
            // Get single ticket by ID including user details
            var ticket = await _repositoryWrapper.Ticket.FindByCondition(t => t.ticketId == ticketId)
                .Include(t => t.User)
                .FirstOrDefaultAsync();
                
            return ticket;
        }
    }
}
