using System.ComponentModel.DataAnnotations;

namespace DimDim.Web.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do cliente.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
    public string? Telefone { get; set; }

    [Display(Name = "Cadastrado em")]
    public DateTime DataCadastro { get; set; }

    [Display(Name = "Cliente ativo")]
    public bool Ativo { get; set; } = true;

    // Relacionamento: um cliente possui várias transações.
    public List<Transacao> Transacoes { get; set; } = new();
}
