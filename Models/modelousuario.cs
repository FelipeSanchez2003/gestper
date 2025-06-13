using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestper.Models;

public class Usuario
{
    [Key]
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; }

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string Apellido { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
        ErrorMessage = "El formato del correo electrónico no es válido")]
    public string Correo { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Contrasena { get; set; }

    [RegularExpression(@"^\+?[0-9]{6,15}$", 
        ErrorMessage = "El teléfono debe contener solo números (entre 6 y 15 dígitos)")]
    public string Telefono { get; set; }

    public int IdRol { get; set; }

    public bool Activo { get; set; }
    
    public int? IdDepartamento { get; set; }
    [ForeignKey("IdDepartamento")]
    public Departamento? Departamento { get; set; }
}