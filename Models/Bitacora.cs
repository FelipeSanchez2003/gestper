using System;
using System.ComponentModel.DataAnnotations;

namespace Gestper.Models
{
    public class Bitacora
    {
        public int Id { get; set; }

        [Required]
        public string Usuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public string Descripcion { get; set; }

        public string Accion { get; set; }
    }
}