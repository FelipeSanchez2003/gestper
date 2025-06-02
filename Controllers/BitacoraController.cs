using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gestper.Data;
using Gestper.Models;

namespace Gestper.Controllers
{
    public class BitacoraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BitacoraController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para obtener el rol actual
        private string? ObtenerRolUsuario()
        {
            return HttpContext.Session.GetString("UsuarioRol");
        }

        // INDEX (Todos los roles pueden ver)
        public async Task<IActionResult> Index()
        {
            var bitacoras = await _context.Bitacoras
                .Include(b => b.Ticket)
                .Include(b => b.Usuario)
                .Include(b => b.SoporteAsignado)
                .OrderByDescending(b => b.FechaCreacion)
                .ToListAsync();

            return View(bitacoras);
        }

        // DETALLE (Todos los roles pueden ver)
        public async Task<IActionResult> Details(int id)
        {
            var bitacora = await _context.Bitacoras
                .Include(b => b.Ticket)
                .Include(b => b.Usuario)
                .Include(b => b.SoporteAsignado)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bitacora == null)
                return NotFound();

            return View(bitacora);
        }

        // CREATE (Todos los roles pueden crear)
        public async Task<IActionResult> Create()
        {
            ViewBag.Tickets = await _context.Tickets.ToListAsync();
            ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
            ViewBag.Trabajadores = await _context.Usuarios.Where(u => u.IdRol == 2).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bitacora bitacora)
        {
            if (ModelState.IsValid)
            {
                bitacora.FechaCreacion = DateTime.Now;
                _context.Bitacoras.Add(bitacora);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tickets = await _context.Tickets.ToListAsync();
            ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
            ViewBag.Trabajadores = await _context.Usuarios.Where(u => u.IdRol == 2).ToListAsync();

            return View(bitacora);
        }

        // EDIT (Validación según rol y estado)
        public async Task<IActionResult> Edit(int id)
        {
            var bitacora = await _context.Bitacoras.FindAsync(id);
            if (bitacora == null) return NotFound();

            var rol = ObtenerRolUsuario();

            if (rol == "3" && bitacora.Estado != "Abierto")
            {
                TempData["Error"] = "Solo puede editar la bitácora si está abierta.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tickets = await _context.Tickets.ToListAsync();
            ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
            ViewBag.Trabajadores = await _context.Usuarios.Where(u => u.IdRol == 2).ToListAsync();

            return View(bitacora);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bitacora bitacora)
        {
            if (id != bitacora.Id)
                return NotFound();

            var bitacoraOriginal = await _context.Bitacoras.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (bitacoraOriginal == null)
                return NotFound();

            var rol = ObtenerRolUsuario();

            // Usuarios sólo pueden editar si está abierto
            if (rol == "3" && bitacoraOriginal.Estado != "Abierto")
            {
                TempData["Error"] = "No tiene permisos para editar esta bitácora.";
                return RedirectToAction(nameof(Index));
            }

            // Validación: solo admin/trabajador pueden cerrarla
            if (bitacora.Estado == "Cerrado" && rol == "3")
            {
                ModelState.AddModelError("", "No tiene permisos para cerrar la bitácora.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bitacora);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BitacoraExists(bitacora.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewBag.Tickets = await _context.Tickets.ToListAsync();
            ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
            ViewBag.Trabajadores = await _context.Usuarios.Where(u => u.IdRol == 2).ToListAsync();

            return View(bitacora);
        }

        // DELETE (opcional)
        public async Task<IActionResult> Delete(int id)
        {
            var bitacora = await _context.Bitacoras
                .Include(b => b.Ticket)
                .Include(b => b.Usuario)
                .Include(b => b.SoporteAsignado)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bitacora == null)
                return NotFound();

            return View(bitacora);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bitacora = await _context.Bitacoras.FindAsync(id);
            if (bitacora != null)
            {
                _context.Bitacoras.Remove(bitacora);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BitacoraExists(int id)
        {
            return _context.Bitacoras.Any(e => e.Id == id);
        }
    }
}
