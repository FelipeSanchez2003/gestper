using Microsoft.AspNetCore.Mvc;
using Gestper.Data;
using Gestper.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using System;

namespace Gestper.Controllers
{
    public class CRUDController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CRUDController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Verificación de sesión
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioCorreo")))
            {
                return RedirectToAction("Login", "Usuario");
            }

            return RedirectToAction("Perfil");
        }
        
        public async Task<IActionResult> Perfil()
        {
            var usuarioCorreo = HttpContext.Session.GetString("UsuarioCorreo");
            if (string.IsNullOrEmpty(usuarioCorreo))
            {
                return RedirectToAction("Login", "Usuario");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == usuarioCorreo);

            if (usuario == null)
            {
                return RedirectToAction("Error", "Home");
            }
            
            return View("crud.perfil", usuario);
        }

        // GET: CRUD/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var usuarioCorreo = HttpContext.Session.GetString("UsuarioCorreo");
            if (string.IsNullOrEmpty(usuarioCorreo))
            {
                return RedirectToAction("Login", "Usuario");
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == usuarioCorreo);

            if (usuario == null)
            {
                return RedirectToAction("Error", "Home");
            }
            
            return View("~/Views/Perfil/Editar.cshtml", usuario);       
        }

        // POST: CRUD/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Usuario usuario)
        {
            // Validación manual adicional para correo
            if (!IsValidEmail(usuario.Correo))
            {
                ModelState.AddModelError("Correo", "El formato del correo electrónico no es válido");
            }

            // Validación manual adicional para teléfono
            if (!string.IsNullOrEmpty(usuario.Telefono) && !IsValidPhone(usuario.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono debe contener solo números");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener el usuario actual de la base de datos para preservar campos que no deben cambiar
                    var usuarioActual = await _context.Usuarios.FindAsync(usuario.IdUsuario);
                    if (usuarioActual == null)
                    {
                        return NotFound();
                    }

                    // Verificar si el correo ya existe para otro usuario
                    if (usuarioActual.Correo != usuario.Correo)
                    {
                        var existeCorreo = await _context.Usuarios
                            .AnyAsync(u => u.Correo == usuario.Correo && u.IdUsuario != usuario.IdUsuario);
                        
                        if (existeCorreo)
                        {
                            ViewBag.MensajeError = "Este correo ya está registrado por otro usuario";
                            return View("~/Views/Perfil/Editar.cshtml", usuario);
                            
                        }
                    }

                    // Actualizar solo los campos permitidos
                    usuarioActual.Nombre = usuario.Nombre;
                    usuarioActual.Apellido = usuario.Apellido;
                    usuarioActual.Correo = usuario.Correo;
                    usuarioActual.Telefono = usuario.Telefono;
                    
                    _context.Update(usuarioActual);
                    await _context.SaveChangesAsync();
                    
                    // Actualizar la sesión con el nuevo correo
                    HttpContext.Session.SetString("UsuarioCorreo", usuarioActual.Correo);
                    
                    TempData["MensajeExito"] = "Perfil actualizado correctamente";
                    return RedirectToAction("Perfil");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IdUsuario))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Capturar cualquier otra excepción
                    ViewBag.MensajeError = "Error al actualizar el perfil: " + ex.Message;
                    return View("~/Views/Perfil/Editar.cshtml", usuario);
                    
                }
            }
            
            return View("~/Views/Perfil/Editar.cshtml", usuario);
            
        }

        public async Task<IActionResult> TicketsCreados(int? estado)
        {
            var correo = HttpContext.Session.GetString("UsuarioCorreo");
            if (string.IsNullOrEmpty(correo))
            {
                return RedirectToAction("Login", "Usuario");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
            if (usuario == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var query = _context.Tickets
                .Include(t => t.Estado)
                .Include(t => t.Categoria)
                .Include(t => t.Prioridad)
                .Include(t => t.Departamento)
                .Where(t => t.IdUsuario == usuario.IdUsuario);

            // Filtro por ID Estado (si no es 0 o null)
            if (estado.HasValue && estado.Value != 0)
            {
                query = query.Where(t => t.IdEstado == estado.Value);
            }

            var tickets = await query.ToListAsync();

            ViewBag.Estados = new SelectList(await _context.Estados.ToListAsync(), "IdEstado", "NombreEstado");
            ViewBag.EstadoFiltro = estado ?? 0;
            ViewBag.TotalPaginas = 1;
            ViewBag.PaginaActual = 1;

            return View("Views/CRUD/crud.ticket.cshtml", tickets);
        }

        // Métodos de validación
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
                
            try
            {
                // Validación usando expresión regular
                var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return true; // Teléfono puede ser opcional
                
            // Solo números y posiblemente un + al inicio
            return Regex.IsMatch(phone, @"^\+?[0-9]{6,15}$");
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}