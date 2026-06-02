using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Logistica.DTOs;
using Backend.Core.Features.Logistica.Entities;
using Backend.Core.Features.Logistica.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Logistica;

public class VeiculosRepository : IVeiculosRepository
{
    private readonly DbSession _session;

    public VeiculosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Veiculos>> ObterVeiculos(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM veiculos;

            SELECT id AS Id, placa AS Placa, estado_id AS EstadoId, transportadora_id AS TransportadoraId, rntrc AS Rntrc, renavam AS Renavam,
                   tipo_veiculo AS TipoVeiculo, marca_modelo AS MarcaModelo, ativo AS Ativo, criado_em AS CriadoEm,
                   observacao AS Observacao
            FROM veiculos
            ORDER BY placa
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Veiculos>();

        return new ResultadoPaginado<Veiculos>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Veiculos?> ObterVeiculoPorId(int id)
    {
        const string sql = @"
            SELECT v.id AS Id, v.placa AS Placa, v.estado_id AS EstadoId, v.transportadora_id AS TransportadoraIdVal, v.rntrc AS Rntrc, v.renavam AS Renavam,
                   v.tipo_veiculo AS TipoVeiculo, v.marca_modelo AS MarcaModelo, v.ativo AS Ativo, v.criado_em AS CriadoEm,
                   v.observacao AS Observacao,
                   t.id AS TransportadoraId, t.nome_razaosocial AS NomeRazaosocial,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf
            FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            WHERE v.id = @Id;";

        var result = await _session.Connection.QueryAsync<Veiculos, Transportadoras, Estados, Veiculos>(
            sql,
            (veiculo, transportadora, estado) =>
            {
                if (transportadora is not null)
                {
                    veiculo.VincularTransportadora(transportadora);
                }
                if (estado is not null)
                {
                    veiculo.VincularEstado(estado);
                }
                return veiculo;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "TransportadoraId,EstadoId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Veiculos> CriarVeiculo(Veiculos veiculo)
    {
        const string sql = @"
            INSERT INTO veiculos (placa, estado_id, transportadora_id, rntrc, renavam,
                                 tipo_veiculo, marca_modelo, ativo, criado_em, observacao)
            VALUES (@Placa, @EstadoId, @TransportadoraId, @Rntrc, @Renavam,
                    @TipoVeiculo, @MarcaModelo, @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                veiculo.Placa,
                veiculo.EstadoId,
                veiculo.TransportadoraId,
                veiculo.Rntrc,
                veiculo.Renavam,
                veiculo.TipoVeiculo,
                veiculo.MarcaModelo,
                veiculo.Ativo,
                CriadoEm = DateTime.UtcNow,
                veiculo.Observacao
            },
            transaction: _session.Transaction
        );

        veiculo.Id = idGerado;
        return veiculo;
    }

    public async Task<Veiculos> AtualizarVeiculo(int id, Veiculos veiculo)
    {
        const string sql = @"
            UPDATE veiculos
            SET placa = @Placa, estado_id = @EstadoId, transportadora_id = @TransportadoraId,
                rntrc = @Rntrc, renavam = @Renavam, tipo_veiculo = @TipoVeiculo,
                marca_modelo = @MarcaModelo, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                veiculo.Placa,
                veiculo.EstadoId,
                veiculo.TransportadoraId,
                veiculo.Rntrc,
                veiculo.Renavam,
                veiculo.TipoVeiculo,
                veiculo.MarcaModelo,
                veiculo.Ativo,
                AtualizadoEm = DateTime.UtcNow,
                veiculo.Observacao
            },
            transaction: _session.Transaction
        );

        veiculo.Id = id;
        return veiculo;
    }

    public async Task<bool> DeletarVeiculo(int id)
    {
        const string sql = "DELETE FROM veiculos WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql, new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<VeiculosResumo>> ObterVeiculosResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM veiculos;

            SELECT v.id, v.placa AS Placa, e.uf AS EstadoSigla, v.marca_modelo AS MarcaModelo, 
                   t.nome_razaosocial AS TransportadoraNome, v.ativo AS Ativo
            FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            ORDER BY v.placa
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<VeiculosResumo>();

        return new ResultadoPaginado<VeiculosResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<VeiculosResumo>> PesquisarVeiculos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            WHERE v.placa ILIKE @Termo OR v.marca_modelo ILIKE @Termo OR t.nome_razaosocial ILIKE @Termo OR e.uf ILIKE @Termo;

            SELECT v.id, v.placa AS Placa, e.uf AS EstadoSigla, v.marca_modelo AS MarcaModelo, 
                   t.nome_razaosocial AS TransportadoraNome, v.ativo AS Ativo
            FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            WHERE v.placa ILIKE @Termo OR v.marca_modelo ILIKE @Termo OR t.nome_razaosocial ILIKE @Termo OR e.uf ILIKE @Termo
            ORDER BY v.placa
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<VeiculosResumo>();

        return new ResultadoPaginado<VeiculosResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
