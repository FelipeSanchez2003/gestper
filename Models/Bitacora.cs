using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestper.Models
{
    [Table("Bitacora")]
    public class Bitacora
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; }

        [Required]
        [StringLength(100)]
        public string Accion { get; set; }
        
        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; }
        
        public int? IdSoporteAsignado { get; set; }

        [ForeignKey("IdSoporteAsignado")]
        public Usuario SoporteAsignado { get; set; }
        
        public string Prioridad { get; set; }
        
        public string Estado { get; set; }
    }
}