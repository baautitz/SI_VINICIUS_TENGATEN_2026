using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Validators.Commands;
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
    private readonly IUnitOfWork _unitOfWork;

    public MovimentacoesEstoquesService(
        IMovimentacoesEstoquesRepository movimentacoesRepository,
        ISkusRepository skusRepository,
        IUsuariosRepository usuariosRepository,
        INfesRepository nfesRepository,
        IVendasRepository vendasRepository,
        IUnitOfWork unitOfWork)
    {
        _movimentacoesRepository = movimentacoesRepository;
        _skusRepository = skusRepository;
        _usuariosRepository = usuariosRepository;
        _nfesRepository = nfesRepository;
        _vendasRepository = vendasRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoes(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _movimentacoesRepository.ObterMovimentacoes(pagina, tamanhoPagina);
        }
        return _movimentacoesRepository.PesquisarMovimentacoes(search, pagina, tamanhoPagina);
    }

    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
        => _movimentacoesRepository.ObterMovimentacaoPorId(id);

    public async Task<Resultado<MovimentacoesEstoques>> CriarMovimentacao(CriarMovimentacaoCommand command)
    {
        var validation = new CriarMovimentacaoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<MovimentacoesEstoques>.Falha(validation.ToResultadoErros());

        var usuario = command.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(command.UsuarioId.Value) : null;
        if (command.UsuarioId.HasValue && usuario == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        var nfe = command.NfeId.HasValue ? await _nfesRepository.ObterNfePorId(command.NfeId.Value) : null;
        if (command.NfeId.HasValue && nfe == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("NFE_INEXISTENTE", "A NF-e informada não existe.", "NfeId"));

        var venda = command.VendaId.HasValue ? await _vendasRepository.ObterVendaPorId(command.VendaId.Value) : null;
        if (command.VendaId.HasValue && venda == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));

        Enum.TryParse<TipoMovimentacaoEstoque>(command.TipoMovimentacao, true, out var tipoMovimentacao);

        var movimentacao = new MovimentacoesEstoques(tipoMovimentacao, usuario, nfe, venda, command.Observacao, StatusMovimentacaoEstoque.RASCUNHO);

        foreach (var itemCommand in command.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
            if (sku == null)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{itemCommand.Sku}' não existe.", "Itens"));
            
            // Replaced ObterResumoPorSku with ObterSkuCompleto (or similar as per new interface)
            var skusCompleto = await _skusRepository.ObterSkuCompleto(itemCommand.Sku);
            // Assuming we need to get product details somehow, but let's assume we can get from skusCompleto if it has product relation or we fetch separately.
            // For now, keeping the logic as close as possible to the entity.
            var resumo = await _skusRepository.ObterProdutoPorSku(itemCommand.Sku);

            movimentacao.AdicionarItem(sku, itemCommand.Quantidade, itemCommand.CustoUnitario ?? 0, resumo?.Produto ?? "Produto Desconhecido", "UN");
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var criado = await _movimentacoesRepository.CriarMovimentacao(movimentacao);
                _unitOfWork.Commit();
                return Resultado<MovimentacoesEstoques>.Sucesso(criado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }
    
    // ... (rest of methods)
    
    public async Task<Resultado<MovimentacoesEstoques>> AtualizarMovimentacao(int id, AtualizarMovimentacaoCommand command)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (existente.Status != StatusMovimentacaoEstoque.RASCUNHO)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_BLOQUEADA", "Apenas movimentações em rascunho podem ser editadas."));

        var validation = new AtualizarMovimentacaoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<MovimentacoesEstoques>.Falha(validation.ToResultadoErros());

        var usuario = command.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(command.UsuarioId.Value) : null;
        if (command.UsuarioId.HasValue && usuario == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        var nfe = command.NfeId.HasValue ? await _nfesRepository.ObterNfePorId(command.NfeId.Value) : null;
        if (command.NfeId.HasValue && nfe == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("NFE_INEXISTENTE", "A NF-e informada não existe.", "NfeId"));

        var venda = command.VendaId.HasValue ? await _vendasRepository.ObterVendaPorId(command.VendaId.Value) : null;
        if (command.VendaId.HasValue && venda == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));

        Enum.TryParse<TipoMovimentacaoEstoque>(command.TipoMovimentacao, true, out var tipoMovimentacao);

        existente.AtualizarObservacao(command.Observacao);
        
        var itensAtuais = existente.MovimentacoesEstoquesItens.ToList();
        foreach (var item in itensAtuais)
        {
            existente.RemoverItem(item);
        }

        foreach (var itemCommand in command.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
            if (sku == null)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{itemCommand.Sku}' não existe.", "Itens"));
            
            var resumo = await _skusRepository.ObterProdutoPorSku(itemCommand.Sku);

            existente.AdicionarItem(sku, itemCommand.Quantidade, itemCommand.CustoUnitario ?? 0, resumo?.Produto ?? "Produto Desconhecido", "UN");
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var atualizado = await _movimentacoesRepository.AtualizarMovimentacao(id, existente);
                _unitOfWork.Commit();
                return Resultado<MovimentacoesEstoques>.Sucesso(atualizado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<MovimentacoesEstoques>> ConfirmarMovimentacao(int id)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (existente.Status != StatusMovimentacaoEstoque.RASCUNHO)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_STATUS_INVALIDO", $"Apenas movimentações em rascunho podem ser efetivadas. Status atual: {existente.Status}"));

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

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                foreach (var item in existente.MovimentacoesEstoquesItens)
                {
                    var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
                    if (sku != null)
                    {
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
                            sku.AjustarEstoque(item.Quantidade - sku.Estoque);
                        }

                        await _skusRepository.AtualizarSku(sku.Sku, sku);
                    }
                }

                existente.Confirmar();
                var atualizado = await _movimentacoesRepository.AtualizarMovimentacao(id, existente);
                
                _unitOfWork.Commit();
                return Resultado<MovimentacoesEstoques>.Sucesso(atualizado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<MovimentacoesEstoques>> CancelarMovimentacao(int id)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (existente.Status != StatusMovimentacaoEstoque.CONFIRMADA)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_STATUS_INVALIDO", $"Apenas movimentações confirmadas podem ser canceladas. Status atual: {existente.Status}"));

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

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

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
                            sku.AjustarEstoque((item.QuantidadeAnterior ?? 0) - sku.Estoque);
                        }

                        await _skusRepository.AtualizarSku(sku.Sku, sku);
                    }
                }

                existente.Cancelar();
                var atualizado = await _movimentacoesRepository.AtualizarMovimentacao(id, existente);
                
                _unitOfWork.Commit();
                return Resultado<MovimentacoesEstoques>.Sucesso(atualizado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<bool> DeletarMovimentacao(int id)
    {
        var existente = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (existente == null) return false;

        if (existente.Status != StatusMovimentacaoEstoque.RASCUNHO)
            throw new InvalidOperationException("Movimentações efetivadas não podem ser excluídas, apenas estornadas.");

        return await _movimentacoesRepository.DeletarMovimentacao(id);
    }
}
