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

            var tickets = await ticketsQuery
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            ViewBag.Total = tickets.Count;
            ViewBag.Nuevos = tickets.Count(t => t.IdEstado == 1);
            ViewBag.EnProgreso = tickets.Count(t => t.IdEstado == 2);
            ViewBag.Cerrados = tickets.Count(t => t.IdEstado == 3);

            ViewBag.Departamentos = await _context.Departamentos.ToListAsync();
            ViewBag.Prioridades = await _context.Prioridades.ToListAsync();
            
            var bitacoras = await _context.Bitacora
                .Include(b => b.Usuario)
                .OrderByDescending(b => b.FechaCreacion)
                .ToListAsync();

            ViewBag.Bitacoras = bitacoras;

            return View("~/Views/Home/IndexAdmin.cshtml", tickets);
        }

        [HttpPost]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Usuario");
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Prioridad)
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
        
        public async Task<IActionResult> Bitacora()
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "1")
                return RedirectToAction("Login", "Usuario");

            var bitacoras = await _context.Bitacora
                .Include(b => b.Usuario)
                .Include(b => b.SoporteAsignado)
                .OrderByDescending(b => b.FechaCreacion)
                .ToListAsync();

            return View("~/Views/Home/BitacoraAdmin.cshtml", bitacoras);
        }

        [HttpPost]
        public async Task<IActionResult> EditarBitacora(int id, string descripcion)
        {
            string rol = HttpContext.Session.GetString("UsuarioRol");
            if (!(rol == "1" || rol == "2" || rol == "3"))
                return Unauthorized();

            var bitacora = await _context.Bitacora.FindAsync(id);
            if (bitacora == null)
                return NotFound();

            bitacora.Descripcion = descripcion;
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Bitácora actualizada correctamente.";
            return RedirectToAction("Bitacora");
        }
        
        public async Task<IActionResult> DetalleBitacora(int id)
        {
            var bitacora = await _context.Bitacora
                .Include(b => b.Usuario)
                .Include(b => b.SoporteAsignado)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bitacora == null)
                return NotFound();

            var trabajadores = await _context.Usuarios
                .Where(u => u.IdRol == 2)
                .ToListAsync();

            ViewBag.Trabajadores = trabajadores;

            return View("~/Views/Home/DetalleBitacora.cshtml", bitacora);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDetalleBitacora(Bitacora model)
        {
            var bitacora = await _context.Bitacora.FindAsync(model.Id);
            if (bitacora == null)
                return NotFound();

            bitacora.Descripcion = model.Descripcion;
            bitacora.IdSoporteAsignado = model.IdSoporteAsignado;
            bitacora.Prioridad = model.Prioridad;
            bitacora.Estado = model.Estado;

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Bitácora actualizada correctamente.";
            return RedirectToAction("Bitacora");
        }
    }
}
