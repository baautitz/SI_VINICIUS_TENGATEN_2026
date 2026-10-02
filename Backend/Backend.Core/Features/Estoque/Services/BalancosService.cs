using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;

namespace Backend.Core.Features.Estoque.Services;

public sealed class BalancosService : BaseService
{
    private readonly IBalancosRepository _balancosRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IUsuariosRepository _usuariosRepository;
    private readonly MovimentacoesEstoquesService _movimentacoesService;
    private readonly IUnitOfWork _unitOfWork;

    public BalancosService(
        IBalancosRepository balancosRepository,
        ISkusRepository skusRepository,
        IUsuariosRepository usuariosRepository,
        MovimentacoesEstoquesService movimentacoesService,
        IUnitOfWork unitOfWork)
    {
        _balancosRepository = balancosRepository;
        _skusRepository = skusRepository;
        _usuariosRepository = usuariosRepository;
        _movimentacoesService = movimentacoesService;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Balancos>> ObterBalancos(int pagina = 1, int tamanhoPagina = 20)
        => _balancosRepository.ObterBalancos(pagina, tamanhoPagina);

    public Task<Balancos?> ObterBalancoPorId(int id)
        => _balancosRepository.ObterBalancoPorId(id);

    public async Task<Resultado<Balancos>> CriarBalanco(CriarBalancoCommand command)
    {
        if (command.Observacao?.Length > 500)
            return Resultado<Balancos>.Falha(new ResultadoErro("OBSERVACAO_EXCEDE_LIMITE", "Observação deve ter no máximo 500 caracteres.", "Observacao"));

        var usuario = command.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(command.UsuarioId.Value) : null;
        if (command.UsuarioId.HasValue && usuario == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        var skus = command.Skus?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        foreach (var codigo in skus ?? new List<string>())
        {
            if (await _skusRepository.ObterSkuPorSku(codigo) == null)
                return Resultado<Balancos>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{codigo}' não existe.", "Skus"));
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var criado = await _balancosRepository.CriarBalanco(new Balancos(command.Observacao, usuario), skus);
                _unitOfWork.Commit();
                return Resultado<Balancos>.Sucesso(criado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<Balancos>> InformarContagem(int id, InformarContagemCommand command)
    {
        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        balanco.ExigirAberto();

        foreach (var linha in command.Itens)
        {
            var item = balanco.Itens.FirstOrDefault(i => string.Equals(i.Sku, linha.Sku, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                return Resultado<Balancos>.Falha(new ResultadoErro("ITEM_INEXISTENTE", $"O SKU '{linha.Sku}' não faz parte deste balanço.", "Itens"));

            item.InformarContagem(linha.QuantidadeContada);
        }

        await _balancosRepository.AtualizarBalanco(balanco);
        return Resultado<Balancos>.Sucesso(balanco);
    }

    // Fecha o balanço: cada diferença vira uma movimentação (ENTRADA para sobra, SAIDA para falta) com origem BALANCO.
    public async Task<Resultado<Balancos>> Fechar(int id)
    {
        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var entrada = new MovimentacoesEstoques(TipoMovimentacaoEstoque.ENTRADA, OrigemMovimentacaoEstoque.BALANCO, id, null, $"Sobra apurada no balanço #{id}", balanco.Usuario);
                var saida = new MovimentacoesEstoques(TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.BALANCO, id, null, $"Falta apurada no balanço #{id}", balanco.Usuario);

                foreach (var item in balanco.Itens.Where(i => i.QuantidadeContada.HasValue))
                {
                    var sku = await _skusRepository.ObterSkuPorSku(item.Sku);
                    if (sku == null) continue;

                    item.AtualizarQuantidadeSistema(sku.Estoque);
                    var diferenca = item.Diferenca!.Value;
                    if (diferenca > 0) entrada.AdicionarItem(sku, diferenca, sku.CustoMedio);
                    else if (diferenca < 0) saida.AdicionarItem(sku, -diferenca, sku.CustoMedio);
                }

                balanco.Fechar();

                foreach (var mov in new[] { entrada, saida }.Where(m => m.MovimentacoesEstoquesItens.Any()))
                {
                    var resultado = await _movimentacoesService.Registrar(mov);
                    if (!resultado.Success)
                    {
                        _unitOfWork.Rollback();
                        return Resultado<Balancos>.Falha(resultado.Errors!);
                    }
                }

                await _balancosRepository.AtualizarBalanco(balanco);
                _unitOfWork.Commit();
                return Resultado<Balancos>.Sucesso(balanco);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<Balancos>> Cancelar(int id)
    {
        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        balanco.Cancelar();
        await _balancosRepository.AtualizarBalanco(balanco);
        return Resultado<Balancos>.Sucesso(balanco);
    }
}
