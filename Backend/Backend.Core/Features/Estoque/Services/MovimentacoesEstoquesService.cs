using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.DTOs;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Validators;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Vendas.Repositories;
using FluentValidation;

namespace Backend.Core.Features.Estoque.Services;

public sealed class MovimentacoesEstoquesService : BaseService
{
    private readonly IMovimentacoesEstoquesRepository _movimentacoesRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IUsuariosRepository _usuariosRepository;
    private readonly INfesRepository _nfesRepository;
    private readonly IVendasRepository _vendasRepository;

    public MovimentacoesEstoquesService(
        IMovimentacoesEstoquesRepository movimentacoesRepository,
        ISkusRepository skusRepository,
        IUsuariosRepository usuariosRepository,
        INfesRepository nfesRepository,
        IVendasRepository vendasRepository)
    {
        _movimentacoesRepository = movimentacoesRepository;
        _skusRepository = skusRepository;
        _usuariosRepository = usuariosRepository;
        _nfesRepository = nfesRepository;
        _vendasRepository = vendasRepository;
    }

    public Task<ResultadoPaginado<MovimentacoesEstoquesResumo>> ObterMovimentacoes(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _movimentacoesRepository.ObterMovimentacoesResumo(pagina, tamanhoPagina);
        }
        return _movimentacoesRepository.PesquisarMovimentacoes(search, pagina, tamanhoPagina);
    }

    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
        => _movimentacoesRepository.ObterMovimentacaoPorId(id);

    public async Task<Resultado<MovimentacoesEstoques>> CriarMovimentacao(CreateMovimentacaoDto dto)
    {
        var validation = new CreateMovimentacaoDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<MovimentacoesEstoques>.Falha(validation.ToResultadoErros());

        // Validate references
        var usuario = dto.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(dto.UsuarioId.Value) : null;
        if (dto.UsuarioId.HasValue && usuario == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        var nfe = dto.NfeId.HasValue ? await _nfesRepository.ObterNfePorId(dto.NfeId.Value) : null;
        if (dto.NfeId.HasValue && nfe == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("NFE_INEXISTENTE", "A NF-e informada não existe.", "NfeId"));

        var venda = dto.VendaId.HasValue ? await _vendasRepository.ObterVendaPorId(dto.VendaId.Value) : null;
        if (dto.VendaId.HasValue && venda == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));

        Enum.TryParse<TipoMovimentacaoEstoque>(dto.TipoMovimentacao, true, out var tipoMovimentacao);

        var movimentacao = new MovimentacoesEstoques(tipoMovimentacao, usuario, nfe, venda, dto.Observacao, StatusMovimentacaoEstoque.RASCUNHO);

        foreach (var itemDto in dto.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(itemDto.Sku);
            if (sku == null)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{itemDto.Sku}' não existe.", "Itens"));

            movimentacao.AdicionarItem(sku, itemDto.Quantidade, itemDto.CustoUnitario ?? 0);
        }

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _movimentacoesRepository.CriarMovimentacao(movimentacao);
            return Resultado<MovimentacoesEstoques>.Sucesso(criado);
        });
    }

    public async Task<Resultado<MovimentacoesEstoques>> AtualizarMovimentacao(int id, UpdateMovimentacaoDto dto)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (existente.Status != StatusMovimentacaoEstoque.RASCUNHO)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_BLOQUEADA", "Apenas movimentações em rascunho podem ser editadas."));

        var validation = new UpdateMovimentacaoDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<MovimentacoesEstoques>.Falha(validation.ToResultadoErros());

        // Validate references
        var usuario = dto.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(dto.UsuarioId.Value) : null;
        if (dto.UsuarioId.HasValue && usuario == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        var nfe = dto.NfeId.HasValue ? await _nfesRepository.ObterNfePorId(dto.NfeId.Value) : null;
        if (dto.NfeId.HasValue && nfe == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("NFE_INEXISTENTE", "A NF-e informada não existe.", "NfeId"));

        var venda = dto.VendaId.HasValue ? await _vendasRepository.ObterVendaPorId(dto.VendaId.Value) : null;
        if (dto.VendaId.HasValue && venda == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));

        Enum.TryParse<TipoMovimentacaoEstoque>(dto.TipoMovimentacao, true, out var tipoMovimentacao);

        // Update basic info
        existente.AtualizarObservacao(dto.Observacao);
        
        // Re-construct items
        var itensAtuais = existente.MovimentacoesEstoquesItens.ToList();
        foreach (var item in itensAtuais)
        {
            existente.RemoverItem(item);
        }

        foreach (var itemDto in dto.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(itemDto.Sku);
            if (sku == null)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{itemDto.Sku}' não existe.", "Itens"));

            existente.AdicionarItem(sku, itemDto.Quantidade, itemDto.CustoUnitario ?? 0);
        }

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _movimentacoesRepository.AtualizarMovimentacao(id, existente);
            return Resultado<MovimentacoesEstoques>.Sucesso(atualizado);
        });
    }

    public async Task<Resultado<MovimentacoesEstoques>> ConfirmarMovimentacao(int id)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (existente.Status != StatusMovimentacaoEstoque.RASCUNHO)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_STATUS_INVALIDO", $"Apenas movimentações em rascunho podem ser efetivadas. Status atual: {existente.Status}"));

        // Proactive inventory check for SAIDA, VENDA
        if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.SAIDA || existente.TipoMovimentacao == TipoMovimentacaoEstoque.VENDA)
        {
            var itensLista = existente.MovimentacoesEstoquesItens.ToList();
            for (int i = 0; i < itensLista.Count; i++)
            {
                var item = itensLista[i];
                var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
                if (sku == null)
                    return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{item.Sku.Sku}' não existe.", $"itens.{i}.sku"));

                if (sku.Estoque < item.Quantidade)
                    return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("ESTOQUE_INSUFICIENTE", $"Estoque insuficiente para o SKU '{sku.Sku}'. Disponível: {sku.Estoque:0.####}, Solicitado: {item.Quantidade:0.####}", $"itens.{i}.quantidade"));
            }
        }

        foreach (var item in existente.MovimentacoesEstoquesItens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
            if (sku != null)
            {
                // Save state for rollback
                item.DefinirQuantidadesECustosAnteriores(sku.Estoque, sku.CustoMedio);

                if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA)
                {
                    sku.RegistrarEntradaDeEstoque(item.Quantidade, item.CustoUnitario);
                }
                else if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.SAIDA || existente.TipoMovimentacao == TipoMovimentacaoEstoque.VENDA)
                {
                    sku.AjustarEstoque(-item.Quantidade);
                }
                else if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.BALANCO)
                {
                    sku.AjustarEstoque(item.Quantidade - sku.Estoque); // Set exactly to the counted amount
                }

                // Persist the SKU modifications
                await _skusRepository.AtualizarSku(sku.Sku, sku);
            }
        }

        existente.Confirmar();

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _movimentacoesRepository.AtualizarMovimentacao(id, existente);
            return Resultado<MovimentacoesEstoques>.Sucesso(atualizado);
        });
    }

    public async Task<Resultado<MovimentacoesEstoques>> CancelarMovimentacao(int id)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (existente.Status != StatusMovimentacaoEstoque.CONFIRMADA)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_STATUS_INVALIDO", $"Apenas movimentações confirmadas podem ser canceladas. Status atual: {existente.Status}"));

        // Proactive inventory check for returning/cancelling an ENTRADA movement (reverting stock could make it negative if we spent it)
        if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA)
        {
            var itensLista = existente.MovimentacoesEstoquesItens.ToList();
            for (int i = 0; i < itensLista.Count; i++)
            {
                var item = itensLista[i];
                var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
                if (sku == null)
                    return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{item.Sku.Sku}' não existe.", $"itens.{i}.sku"));

                if (sku.Estoque < item.Quantidade)
                    return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("ESTOQUE_INSUFICIENTE", $"Não é possível estornar esta entrada. A dedução das mercadorias deixaria o SKU '{sku.Sku}' com saldo negativo. Disponível: {sku.Estoque:0.####}, Necessário deduzir: {item.Quantidade:0.####}", $"itens.{i}.quantidade"));
            }
        }

        foreach (var item in existente.MovimentacoesEstoquesItens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
            if (sku != null)
            {
                if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA)
                {
                    sku.ReverterEntradaDeEstoque(item.Quantidade, item.CustoMedioAnterior ?? 0);
                }
                else if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.SAIDA || existente.TipoMovimentacao == TipoMovimentacaoEstoque.VENDA)
                {
                    sku.AjustarEstoque(item.Quantidade);
                }
                else if (existente.TipoMovimentacao == TipoMovimentacaoEstoque.BALANCO)
                {
                    sku.AjustarEstoque((item.QuantidadeAnterior ?? 0) - sku.Estoque); // Set back to exactly what it was before the count
                }

                await _skusRepository.AtualizarSku(sku.Sku, sku);
            }
        }

        existente.Cancelar();

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _movimentacoesRepository.AtualizarMovimentacao(id, existente);
            return Resultado<MovimentacoesEstoques>.Sucesso(atualizado);
        });
    }

    public async Task<bool> DeletarMovimentacao(int id)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null) return false;

        // If confirmed, user cannot delete (must use Cancelar/Estornar). Only draft can be deleted.
        if (existente.Status != StatusMovimentacaoEstoque.RASCUNHO)
            throw new InvalidOperationException("Movimentações efetivadas não podem ser excluídas, apenas estornadas.");

        return await _movimentacoesRepository.DeletarMovimentacao(id);
    }
}
