using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class EnderecoViewModel
    {
        [Display(Name = "CEP")]
        [StringLength(9, ErrorMessage = "CEP inválido")]
        public string Cep { get; set; }

        [Display(Name = "Endereço")]
        [StringLength(200, ErrorMessage = "O endereço não pode ter mais de 200 caracteres")]
        public string Endereco { get; set; }

        [Display(Name = "Número")]
        [StringLength(10, ErrorMessage = "O número não pode ter mais de 10 caracteres")]
        public string Numero { get; set; }

        [Display(Name = "Bairro")]
        [StringLength(100, ErrorMessage = "O bairro não pode ter mais de 100 caracteres")]
        public string Bairro { get; set; }

        [Display(Name = "Complemento")]
        [StringLength(100, ErrorMessage = "O complemento não pode ter mais de 100 caracteres")]
        public string Complemento { get; set; }
    }
}