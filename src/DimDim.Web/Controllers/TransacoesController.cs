using DimDim.Web.Data;
using DimDim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DimDim.Web.Controllers;

/// <summary>CRUD da tabela Transacoes (cada transação pertence a um cliente).</summary>
public class TransacoesController : Controller
{
    private readonly DimDimContext _db;
    private readonly ILogger<TransacoesController> _logger;

    public TransacoesController(DimDimContext db, ILogger<TransacoesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ---------------------------- READ (lista) ----------------------------
    public async Task<IActionResult> Index(int? clienteId)
    {
        var consulta = _db.Transacoes.AsNoTracking().Include(t => t.Cliente).AsQueryable();

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(t => t.ClienteId == clienteId.Value);
            ViewData["ClienteFiltro"] = await _db.Clientes
                .Where(c => c.Id == clienteId.Value)
                .Select(c => c.Nome)
                .FirstOrDefaultAsync();
        }

        return View(await consulta.OrderByDescending(t => t.DataTransacao).ToListAsync());
    }

    // ---------------------------- READ (detalhe) ----------------------------
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var transacao = await _db.Transacoes
            .AsNoTracking()
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transacao is null) return NotFound();
        return View(transacao);
    }

    // ---------------------------- CREATE ----------------------------
    public async Task<IActionResult> Create(int? clienteId)
    {
        await CarregarClientesAsync(clienteId);
        return View(new Transacao
        {
            ClienteId = clienteId ?? 0,
            DataTransacao = Horario.AgoraSemSegundos(),
            Tipo = TipoTransacao.Pagamento,
            Status = StatusTransacao.Concluida
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("ClienteId,Descricao,Valor,Tipo,Status,DataTransacao")] Transacao transacao)
    {
        if (transacao.ClienteId > 0)
        {
            var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == transacao.ClienteId);
            if (cliente is null)
            {
                ModelState.AddModelError(nameof(Transacao.ClienteId), "Selecione um cliente válido.");
            }
            else if (!cliente.Ativo)
            {
                ModelState.AddModelError(nameof(Transacao.ClienteId), "Este cliente está inativo e não pode receber novas transações.");
            }
        }

        if (!ModelState.IsValid)
        {
            await CarregarClientesAsync(transacao.ClienteId);
            return View(transacao);
        }

        _db.Transacoes.Add(transacao);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Transação {TransacaoId} criada para o cliente {ClienteId} (tipo {Tipo}, valor {Valor})",
            transacao.Id, transacao.ClienteId, transacao.Tipo, transacao.Valor);
        TempData["Mensagem"] = "Transação registrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------- UPDATE ----------------------------
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var transacao = await _db.Transacoes.FindAsync(id);
        if (transacao is null) return NotFound();

        await CarregarClientesAsync(transacao.ClienteId);
        return View(transacao);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,ClienteId,Descricao,Valor,Tipo,Status,DataTransacao")] Transacao form)
    {
        if (id != form.Id) return NotFound();

        var transacao = await _db.Transacoes.FindAsync(id);
        if (transacao is null) return NotFound();

        if (form.ClienteId > 0 && !await _db.Clientes.AnyAsync(c => c.Id == form.ClienteId))
        {
            ModelState.AddModelError(nameof(Transacao.ClienteId), "Selecione um cliente válido.");
        }

        if (!ModelState.IsValid)
        {
            await CarregarClientesAsync(form.ClienteId);
            return View(form);
        }

        transacao.ClienteId = form.ClienteId;
        transacao.Descricao = form.Descricao;
        transacao.Valor = form.Valor;
        transacao.Tipo = form.Tipo;
        transacao.Status = form.Status;
        transacao.DataTransacao = form.DataTransacao;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Transação {TransacaoId} atualizada (status {Status}, valor {Valor})",
            transacao.Id, transacao.Status, transacao.Valor);
        TempData["Mensagem"] = "Transação atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------- DELETE ----------------------------
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();

        var transacao = await _db.Transacoes
            .AsNoTracking()
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transacao is null) return NotFound();
        return View(transacao);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transacao = await _db.Transacoes.FindAsync(id);
        if (transacao is not null)
        {
            _db.Transacoes.Remove(transacao);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Transação {TransacaoId} excluída", id);
            TempData["Mensagem"] = "Transação excluída com sucesso.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------- auxiliar ----------------------------
    private async Task CarregarClientesAsync(int? selecionado)
    {
        var clientes = await _db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome })
            .ToListAsync();

        ViewBag.Clientes = new SelectList(clientes, "Id", "Nome", selecionado);
    }
}
