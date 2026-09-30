using System.Diagnostics;
using DimDim.Web.Data;
using DimDim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DimDim.Web.Controllers;

public class HomeController : Controller
{
    private readonly DimDimContext _db;

    public HomeController(DimDimContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var modelo = new HomeViewModel
        {
            TotalClientes = await _db.Clientes.CountAsync(),
            TotalTransacoes = await _db.Transacoes.CountAsync(),
            ValorConcluido = await _db.Transacoes
                .Where(t => t.Status == StatusTransacao.Concluida)
                .SumAsync(t => t.Valor),
            Recentes = await _db.Transacoes
                .AsNoTracking()
                .Include(t => t.Cliente)
                .OrderByDescending(t => t.DataTransacao)
                .Take(6)
                .ToListAsync()
        };

        return View(modelo);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
