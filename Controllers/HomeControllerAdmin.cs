using Microsoft.AspNetCore.Mvc;
using Gestper.Models;
using Gestper.Data;
using Microsoft.EntityFrameworkCore;

namespace Gestper.Controllers
{
    public class HomeControllerAdmin : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeControllerAdmin(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? idDepartamento, int? idEstado, int? idPrioridad, int? idBusqueda, DateTime? fechaFiltro)
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "1")
                return RedirectToAction("Login", "Usuario");

            var ticketsQuery = _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Prioridad)
                .Include(t => t.SoporteAsignado)
                .AsQueryable();

            if (idDepartamento.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.IdDepartamento == idDepartamento);

            if (idEstado.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.IdEstado == idEstado);

            if (idPrioridad.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.IdPrioridad == idPrioridad);

            if (idBusqueda.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.IdTicket == idBusqueda);

            if (fechaFiltro.HasValue)
                ticketsQuery = ticketsQuery.Where(t => t.FechaCreacion.Date == fechaFiltro.Value.Date);

            var tickets = await ticketsQuery.OrderByDescending(t => t.FechaCreacion).ToListAsync();

            ViewBag.Total = tickets.Count;
            ViewBag.Nuevos = tickets.Count(t => t.IdEstado == 1);
            ViewBag.EnProgreso = tickets.Count(t => t.IdEstado == 2);
            ViewBag.Cerrados = tickets.Count(t => t.IdEstado == 3);

            ViewBag.Departamentos = await _context.Departamentos.ToListAsync();
            ViewBag.Prioridades = await _context.Prioridades.ToListAsync();

            return View("~/Views/Home/IndexAdmin.cshtml", tickets);
        }

        [HttpPost]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Usuario");
        }

        // EDITAR TICKET ADMIN + INCLUIMOS SEGUIMIENTOS
        public async Task<IActionResult> Detalle(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Prioridad)
                .Include(t => t.Departamento)
                .Include(t => t.Estado)
                .Include(t => t.Seguimientos)
                    .ThenInclude(s => s.Usuario)
                .Include(t => t.SoporteAsignado)
                .FirstOrDefaultAsync(t => t.IdTicket == id);

            if (ticket == null)
                return NotFound();

            var trabajadores = await _context.Usuarios
                .Where(u => u.IdRol == 2 && u.IdDepartamento == ticket.IdDepartamento)
                .ToListAsync();

            ViewBag.Trabajadores = trabajadores;
            ViewBag.Estados = await _context.Estados.ToListAsync();

            return View("~/Views/Home/DetalleTicket.cshtml", ticket);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(Ticket ticket)
        {
            var ticketExistente = await _context.Tickets.FindAsync(ticket.IdTicket);
            if (ticketExistente == null)
                return NotFound();

            ticketExistente.Titulo = ticket.Titulo;
            ticketExistente.Descripcion = ticket.Descripcion;
            ticketExistente.IdSoporteAsignado = ticket.IdSoporteAsignado;
            ticketExistente.IdPrioridad = ticket.IdPrioridad;
            ticketExistente.IdEstado = ticket.IdEstado;

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Ticket actualizado correctamente";
            return RedirectToAction("Index", "HomeControllerAdmin");
        }

        // MÉTODO PARA AGREGAR SEGUIMIENTOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarSeguimiento(int idTicket, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                TempData["Error"] = "El comentario no puede estar vacío.";
                return RedirectToAction("Detalle", new { id = idTicket });
            }

            var ticket = await _context.Tickets.FindAsync(idTicket);
            if (ticket == null || ticket.IdEstado == 4)
            {
                TempData["Error"] = "No se puede agregar seguimiento a este ticket.";
                return RedirectToAction("Detalle", new { id = idTicket });
            }

            var usuarioCorreo = HttpContext.Session.GetString("UsuarioCorreo");
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuarioCorreo);

            if (usuario == null)
            {
                TempData["Error"] = "Debe iniciar sesión para agregar comentarios.";
                return RedirectToAction("Index", "HomeControllerAdmin");
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

            return RedirectToAction("Detalle", new { id = idTicket });
        }
    }
}
