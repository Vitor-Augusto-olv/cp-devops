using DimDim.Web.Data;
using DimDim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DimDim.Web.Controllers;

/// <summary>CRUD da tabela Clientes.</summary>
public class ClientesController : Controller
{
    private readonly DimDimContext _db;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(DimDimContext db, ILogger<ClientesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ---------------------------- READ (lista) ----------------------------
    public async Task<IActionResult> Index(string? busca)
    {
        var consulta = _db.Clientes.AsNoTracking().Include(c => c.Transacoes).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            consulta = consulta.Where(c => c.Nome.Contains(busca) || c.Email.Contains(busca));
        }

        ViewData["Busca"] = busca;
        return View(await consulta.OrderBy(c => c.Nome).ToListAsync());
    }

    // ---------------------------- READ (detalhe) ----------------------------
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var cliente = await _db.Clientes
            .AsNoTracking()
            .Include(c => c.Transacoes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null) return NotFound();
        return View(cliente);
    }

    // ---------------------------- CREATE ----------------------------
    public IActionResult Create()
    {
        return View(new Cliente { Ativo = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nome,Email,Telefone,Ativo")] Cliente cliente)
    {
        if (!string.IsNullOrWhiteSpace(cliente.Email))
        {
            cliente.Email = cliente.Email.Trim().ToLowerInvariant();
            if (await _db.Clientes.AnyAsync(c => c.Email == cliente.Email))
            {
                ModelState.AddModelError(nameof(Cliente.Email), "Já existe um cliente com este e-mail.");
            }
        }

        if (!ModelState.IsValid) return View(cliente);

        cliente.DataCadastro = Horario.Agora();
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cliente {ClienteId} criado", cliente.Id);
        TempData["Mensagem"] = "Cliente cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------- UPDATE ----------------------------
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        return View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Email,Telefone,Ativo")] Cliente form)
    {
        if (id != form.Id) return NotFound();

        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(form.Email))
        {
            form.Email = form.Email.Trim().ToLowerInvariant();
            if (await _db.Clientes.AnyAsync(c => c.Email == form.Email && c.Id != id))
            {
                ModelState.AddModelError(nameof(Cliente.Email), "Já existe outro cliente com este e-mail.");
            }
        }

        if (!ModelState.IsValid)
        {
            form.DataCadastro = cliente.DataCadastro;
            return View(form);
        }

        cliente.Nome = form.Nome;
        cliente.Email = form.Email;
        cliente.Telefone = form.Telefone;
        cliente.Ativo = form.Ativo;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cliente {ClienteId} atualizado", cliente.Id);
        TempData["Mensagem"] = "Cliente atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------- DELETE ----------------------------
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var cliente = await _db.Clientes
            .AsNoTracking()
            .Include(c => c.Transacoes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null) return NotFound();
        return View(cliente);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cliente = await _db.Clientes.Include(c => c.Transacoes).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return RedirectToAction(nameof(Index));

        // Regra do relacionamento 1:N: cliente com transações não pode ser excluído.
        if (cliente.Transacoes.Count > 0)
        {
            ModelState.AddModelError(string.Empty,
                $"Não é possível excluir: o cliente possui {cliente.Transacoes.Count} transação(ões). Exclua as transações primeiro.");
            return View(cliente);
        }

        _db.Clientes.Remove(cliente);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cliente {ClienteId} excluído", id);
        TempData["Mensagem"] = "Cliente excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
