using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestper.Models
{
    public class Seguimiento
    {
        [Key]
        public int IdSeguimiento { get; set; }

        [Required]
        public int IdTicket { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "El comentario es obligatorio.")]
        public string Comentario { get; set; }

        [ForeignKey("IdTicket")]
        public virtual Ticket Ticket { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }
    }
}