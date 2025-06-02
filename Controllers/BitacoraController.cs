using Microsoft.AspNetCore.Mvc;
using Gestper.Models;
using Gestper.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Gestper.Controllers
{
    public class BitacoraController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BitacoraController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bitacora/Bitacora
        public async Task<IActionResult> Bitacora()
        {
            if (HttpContext.Session.GetString("UsuarioRol") == null)
                return RedirectToAction("Login", "Usuario");

            var bitacoras = await _context.Bitacora
                .OrderByDescending(b => b.FechaCreacion)
                .ToListAsync();

            string rol = HttpContext.Session.GetString("UsuarioRol") ?? "";
            ViewBag.Rol = rol;

            return View("~/Views/Home/Bitacora.cshtml", bitacoras);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarBitacora(string descripcion)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if ((rol == "1" || rol == "2") && usuarioId.HasValue)
            {
                var nuevaBitacora = new Bitacora
                {
                    IdUsuario = usuarioId.Value,
                    FechaCreacion = DateTime.Now,
                    Descripcion = descripcion,
                    Accion = "Registro de Bitácora"
                };

                _context.Bitacora.Add(nuevaBitacora);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "Bitácora registrada correctamente.";
            }

            return RedirectToAction("Bitacora");
        }
    }
}



