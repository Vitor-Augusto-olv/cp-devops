using System.ComponentModel.DataAnnotations;

namespace DimDim.Web.Models;

public enum TipoTransacao
{
    Pagamento,
    Recebimento,
    [Display(Name = "Transferência")]
    Transferencia
}

public enum StatusTransacao
{
    Pendente,
    [Display(Name = "Concluída")]
    Concluida,
    Cancelada
}

public class Transacao
{
    public int Id { get; set; }

    // Chave estrangeira para Clientes.
    [Display(Name = "Cliente")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um cliente.")]
    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    [Required(ErrorMessage = "Informe uma descrição.")]
    [StringLength(150, ErrorMessage = "A descrição deve ter no máximo 150 caracteres.")]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; } = string.Empty;

    [Range(0.01, 999999999.99, ErrorMessage = "Informe um valor maior que zero.")]
    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
    public decimal Valor { get; set; }

    public TipoTransacao Tipo { get; set; }

    public StatusTransacao Status { get; set; } = StatusTransacao.Concluida;

    [Display(Name = "Data e hora")]
    [DataType(DataType.DateTime)]
    public DateTime DataTransacao { get; set; }
}
