using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gestper.Models;
using Gestper.Data;              // Para ApplicationDbContext
using Microsoft.AspNetCore.Http; // Para Session
using System.Linq;

namespace Gestper.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // Inyectamos ApplicationDbContext junto con ILogger
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        
        public IActionResult Bitacora()
        {
            // Traemos todas las entradas ordenadas por fecha descendente para la vista
            var bitacoras = _context.Bitacoras.OrderByDescending(b => b.Fecha).ToList();
            return View(bitacoras);
        }

        [HttpPost]
        public IActionResult AgregarBitacora(string descripcion)
        {
            var rol = HttpContext.Session.GetString("UsuarioRol");
            var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Usuario Desconocido";

            // Solo admins y trabajadores pueden agregar a la bitácora
            if (rol == "1" || rol == "2")
            {
                var nuevaEntrada = new Bitacora
                {
                    Usuario = nombreUsuario,
                    Fecha = DateTime.Now,
                    Descripcion = descripcion,
                    Accion = "Registro agregado"
                };

                _context.Bitacoras.Add(nuevaEntrada);
                _context.SaveChanges();
            }

            // Redirigimos de nuevo a la vista Bitacora para refrescar
            return RedirectToAction("Bitacora");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
