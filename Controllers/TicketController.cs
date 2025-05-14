using junimo_v3.Models;
using junimo_v3.Services;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet("/ticket/create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("/ticket/create")]
        public async Task<IActionResult> Create(Ticket ticket)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ticket.userId = userId;
            await _ticketService.CreateTicket(ticket);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/tickets")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            
            var tickets = await _ticketService.GetUserTicketsAsync(userId);
            return View(tickets);
        }

        [HttpGet("/ticket/details/{id:int}")]
        [Authorize(Roles = "User")]
        public async Task <IActionResult> TicketDetails(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ticket = await _ticketService.GetUserTicketByIdAsync(userId, id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        [HttpPost("/ticket/details/{id:int}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> TicketDetails(int id, string status)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var ticket = await _ticketService.GetUserTicketByIdAsync(userId, id);

            if (ticket == null) return NotFound();

            if (status == "Closed")
            {
                ticket.status = "Closed";
                ticket.resolvedDate = DateOnly.FromDateTime(DateTime.Today);
                await _ticketService.UpdateTicketAsync(ticket);
            }

            return RedirectToAction(nameof(TicketDetails), new { id });
        }

    }
}
