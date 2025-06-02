using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestper.Models
{
    public class Bitacora
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdTicket { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; }

        public string? Descripcion { get; set; }
        public string? Accion { get; set; }
        public string? Estado { get; set; }
        public string? Prioridad { get; set; }
        public int? IdSoporteAsignado { get; set; }

        // Relaciones
        [ForeignKey("IdTicket")]
        public virtual Ticket? Ticket { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("IdSoporteAsignado")]
        public virtual Usuario? SoporteAsignado { get; set; }
    }
}