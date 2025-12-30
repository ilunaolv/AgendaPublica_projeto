using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class EventoViewModel
    {
        [Required(ErrorMessage = "Título é obrigatório")]
        [Display(Name = "Nome do Evento")]
        public string Titulo { get; set; }

        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Secretaria é obrigatória")]
        [Display(Name = "Secretaria Responsável")]
        public int CdSecretaria { get; set; }

        [Required(ErrorMessage = "Tipo de evento é obrigatório")]
        [Display(Name = "Tipo de Evento")]
        public int CdTipoEvento { get; set; }

        [Display(Name = "Nível de Prioridade")]
        public int? CdNivelPrioridade { get; set; } = 1;

        [Required(ErrorMessage = "Visualização do evento é obrigatória")]
        [Display(Name = "Visualização do Evento")]
        public bool VisualizacaoInterna { get; set; } = true; // true = Privado, false = Público

        [Required(ErrorMessage = "Data de início é obrigatória")]
        [Display(Name = "Data e Hora de Início")]
        public DateTime DataInicio { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Data de fim é obrigatória")]
        [Display(Name = "Data e Hora de Término")]
        public DateTime DataFim { get; set; } = DateTime.Now.AddHours(2);

        public EnderecoViewModel Endereco { get; set; } = new EnderecoViewModel();

        [Display(Name = "Banner")]
        public string BannerBase64 { get; set; }
    }
}