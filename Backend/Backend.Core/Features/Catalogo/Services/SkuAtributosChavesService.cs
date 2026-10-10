using System;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators.Commands;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class SkuAtributosChavesService : BaseService
{
    private readonly ISkuAtributosChavesRepository _atributosRepository;

    public SkuAtributosChavesService(ISkuAtributosChavesRepository atributosRepository)
    {
        _atributosRepository = atributosRepository;
    }

    public Task<ResultadoPaginado<SkuAtributosChaves>> ObterAtributos(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _atributosRepository.ObterAtributos(pagina, tamanhoPagina);
        }
        return _atributosRepository.PesquisarAtributos(search, pagina, tamanhoPagina);
    }

    public Task<SkuAtributosChaves?> ObterAtributoPorId(int id)
        => _atributosRepository.ObterAtributoPorId(id);

    public async Task<Resultado<SkuAtributosChaves>> CriarAtributo(CriarSkuAtributosChavesCommand command)
    {
        var validation = new CriarSkuAtributosChavesCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<SkuAtributosChaves>.Falha(validation.ToResultadoErros());

        if (await _atributosRepository.ExisteChave(command.Chave))
            return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um atributo com este nome.", "Chave"));

        var entidade = new SkuAtributosChaves(command.Chave);

        if (command.Valores != null)
        {
            foreach (var item in command.Valores)
            {
                if (!string.IsNullOrWhiteSpace(item?.Valor))
                {
                    var valor = new SkuAtributosValores(0, 0, item.Valor);
                    if (entidade.SkuAtributosValores.Any(x => x.Valor.Equals(valor.Valor, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    entidade.AdicionarValor(valor);
                }
            }
        }

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _atributosRepository.CriarAtributo(entidade);
            return Resultado<SkuAtributosChaves>.Sucesso(criado);
        });
    }

    public async Task<Resultado<SkuAtributosChaves>> AtualizarAtributo(int id, AtualizarSkuAtributosChavesCommand command)
    {
        var validation = new AtualizarSkuAtributosChavesCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<SkuAtributosChaves>.Falha(validation.ToResultadoErros());

        var existente = await _atributosRepository.ObterAtributoPorId(id);
        if (existente is null)
            return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("ATRIBUTO_NAO_ENCONTRADO", "Atributo não encontrado."));

        if (await _atributosRepository.ExisteChave(command.Chave, id))
            return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro atributo com este nome.", "Chave"));

        existente.Atualizar(command.Chave);

        // Valores pedidos: com Id (de um valor deste atributo) mantém/renomeia o mesmo registro; sem Id,
        // vale o texto (reaproveita o existente de mesmo nome ou cria um novo).
        var pedidos = new List<(int? Id, string Valor)>();
        foreach (var item in command.Valores ?? new List<AtributoValorItem>())
        {
            if (string.IsNullOrWhiteSpace(item?.Valor)) continue;
            var normalizado = new SkuAtributosValores(0, 0, item.Valor).Valor;
            if (pedidos.Any(p => p.Valor.Equals(normalizado, StringComparison.OrdinalIgnoreCase)))
                return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("DUPLICIDADE", $"Valor duplicado: {normalizado}.", "Valores"));
            var idDoAtributo = item.Id is > 0 && existente.SkuAtributosValores.Any(v => v.Id == item.Id) ? item.Id : null;
            pedidos.Add((idDoAtributo, normalizado));
        }

        var finais = new List<SkuAtributosValores>();
        var mantidos = new HashSet<int>();
        foreach (var (valorId, valor) in pedidos)
        {
            var atual = valorId is not null
                ? existente.SkuAtributosValores.First(v => v.Id == valorId)
                : existente.SkuAtributosValores.FirstOrDefault(v => v.Valor.Equals(valor, StringComparison.OrdinalIgnoreCase));
            if (atual is not null) mantidos.Add(atual.Id);
            finais.Add(new SkuAtributosValores(atual?.Id ?? 0, existente.Id, valor));
        }

        // Valor que sai da lista só pode sair se nenhum SKU estiver vinculado a ele.
        var removidos = existente.SkuAtributosValores.Where(v => !mantidos.Contains(v.Id)).ToList();
        if (removidos.Count > 0)
        {
            var vinculos = await _atributosRepository.ContarSkusPorValor(removidos.Select(v => v.Id));
            var emUso = removidos.Where(v => vinculos.ContainsKey(v.Id)).ToList();
            if (emUso.Count > 0)
            {
                var detalhe = string.Join(", ", emUso.Select(v => $"{v.Valor} ({vinculos[v.Id]} SKU(s))"));
                return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro(
                    "VALOR_VINCULADO_A_SKU",
                    $"Não é possível remover valores vinculados a SKUs: {detalhe}. Desvincule ou desative os SKUs antes.",
                    "Valores"));
            }
        }

        existente.LimparValores();
        foreach (var valor in finais) existente.AdicionarValor(valor);

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _atributosRepository.AtualizarAtributo(id, existente);
            return Resultado<SkuAtributosChaves>.Sucesso(atualizado);
        });
    }

    public async Task<Resultado<bool>> DeletarAtributo(int id)
    {
        if (await _atributosRepository.ObterAtributoPorId(id) is null)
            return Resultado<bool>.Falha(new ResultadoErro("ATRIBUTO_NAO_ENCONTRADO", "Atributo não encontrado."));

        var emUso = await _atributosRepository.ContarSkusPorChave(id);
        if (emUso > 0)
            return Resultado<bool>.Falha(new ResultadoErro(
                "ATRIBUTO_EM_USO",
                $"Não é possível excluir: o atributo tem valores vinculados a {emUso} SKU(s)."));

        return Resultado<bool>.Sucesso(await _atributosRepository.DeletarAtributo(id));
    }
}
