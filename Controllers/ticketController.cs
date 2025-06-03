using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Gestper.Models;
using Gestper.Data;
using Microsoft.EntityFrameworkCore;
using Gestper.Services;


namespace Gestper.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public TicketController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int ObtenerIdUsuarioActual()
        {
            var correo = HttpContext.Session.GetString("UsuarioCorreo");
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);
            return usuario?.IdUsuario ?? 0;
        }

        // Método centralizado para cargar los combos
        private void CargarCombos(Ticket ticket = null)
        {
            ViewBag.Estados = new SelectList(_context.Estados.ToList(), "IdEstado", "NombreEstado", ticket?.IdEstado);
            ViewBag.Categorias = new SelectList(_context.Categorias.ToList(), "IdCategoria", "Nombre", ticket?.IdCategoria);
            ViewBag.Prioridades = new SelectList(_context.Prioridades.ToList(), "IdPrioridad", "NombrePrioridad", ticket?.IdPrioridad);
            ViewBag.Departamentos = new SelectList(_context.Departamentos.ToList(), "IdDepartamento", "Nombre", ticket?.IdDepartamento);
        }

        public IActionResult MisTickets()
        {
            int idUsuario = ObtenerIdUsuarioActual();
            if (idUsuario == 0) return Unauthorized();

            var ticketsCliente = _context.Tickets
                .Include(t => t.Estado)
                .Include(t => t.Categoria)
                .Include(t => t.Prioridad)
                .Include(t => t.Departamento)
                .Where(t => t.IdUsuario == idUsuario)
                .ToList();

            if (!ticketsCliente.Any())
            {
                return RedirectToAction("Create", "Ticket");
            }

            // Siempre cargamos los combos antes de enviar la vista
            CargarCombos();
            return View("Views/CRUD/crud.ticket.cshtml", ticketsCliente);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Estado)
                .Include(t => t.Prioridad)
                .Include(t => t.Seguimientos)
                .ThenInclude(s => s.Usuario)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
                return NotFound();

            return View(ticket);
        }
        
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Estado)
                .Include(t => t.Categoria)
                .Include(t => t.Prioridad)
                .Include(t => t.Departamento)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
                return NotFound();

            var trabajadores = await _context.Usuarios
                .Where(u => u.IdRol == 2 && u.IdDepartamento == ticket.IdDepartamento)
                .ToListAsync();

            ViewBag.Trabajadores = trabajadores;

            CargarCombos(ticket);
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ticket ticket)
        {
            if (id != ticket.IdTicket) return NotFound();

            if (ModelState.IsValid)
            {
                var ticketExistente = await _context.Tickets.FindAsync(id);
                if (ticketExistente == null) return NotFound();

                try
                {
                    ticketExistente.Titulo = ticket.Titulo;
                    ticketExistente.Descripcion = ticket.Descripcion;
                    ticketExistente.IdCategoria = ticket.IdCategoria;
                    ticketExistente.IdDepartamento = ticket.IdDepartamento;
                    ticketExistente.IdPrioridad = ticket.IdPrioridad;
                    ticketExistente.IdEstado = ticket.IdEstado;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tickets.Any(e => e.IdTicket == id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction("MisTickets");
            }

            CargarCombos(ticket);
            return View(ticket);
        }

        public IActionResult Create()
        {
            CargarCombos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.FechaCreacion = DateTime.Now;
                ticket.IdEstado = 1;
                ticket.IdPrioridad = 4;

                var correo = HttpContext.Session.GetString("UsuarioCorreo");
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);

                if (usuario == null)
                {
                    TempData["LoginError"] = "Debe iniciar sesión para crear un ticket.";
                    return RedirectToAction("Index", "Home");
                }

                ticket.IdUsuario = usuario.IdUsuario;

                var tecnico = await _context.Usuarios
                    .Where(u => u.IdRol == 2)
                    .OrderBy(u => _context.Tickets.Count(t => t.IdSoporteAsignado == u.IdUsuario && t.IdEstado != 3))
                    .FirstOrDefaultAsync();

                if (tecnico != null)
                {
                    ticket.IdSoporteAsignado = tecnico.IdUsuario;
                }

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ticket creado exitosamente.";
                return RedirectToAction("MisTickets");
            }

            CargarCombos(ticket);
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction("MisTickets");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarSeguimiento(int idTicket, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                TempData["Error"] = "El comentario no puede estar vacío.";
                return RedirectToAction("Details", new { id = idTicket });
            }

            var ticket = await _context.Tickets.FindAsync(idTicket);
            if (ticket == null || ticket.IdEstado == 4)
            {
                TempData["Error"] = "No se puede agregar seguimiento a este ticket.";
                return RedirectToAction("Details", new { id = idTicket });
            }

            var usuarioCorreo = HttpContext.Session.GetString("UsuarioCorreo");
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuarioCorreo);

            if (usuario == null)
            {
                TempData["Error"] = "Debe iniciar sesión para agregar comentarios.";
                return RedirectToAction("Index", "Home");
            }

            var seguimiento = new Seguimiento
            {
                IdTicket = idTicket,
                IdUsuario = usuario.IdUsuario,
                Fecha = DateTime.Now,
                Comentario = comentario
            };

            _context.Seguimientos.Add(seguimiento);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = idTicket });
        }
    }
}
