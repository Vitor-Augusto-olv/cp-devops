namespace DimDim.Web.Models;

public class HomeViewModel
{
    public int TotalClientes { get; set; }
    public int TotalTransacoes { get; set; }
    public decimal ValorConcluido { get; set; }
    public List<Transacao> Recentes { get; set; } = new();
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool MostrarRequestId => !string.IsNullOrEmpty(RequestId);
}
