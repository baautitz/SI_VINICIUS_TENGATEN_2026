using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
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

        const string sqlCount = "SELECT COUNT(*) FROM veiculos;";
        const string sqlData = @"
            SELECT v.id AS Id, v.placa AS Placa, v.rntrc AS Rntrc, v.renavam AS Renavam,
                   v.tipo_veiculo AS TipoVeiculo, v.marca_modelo AS MarcaModelo, v.ativo AS Ativo, v.criado_em AS CriadoEm,
                   v.observacao AS Observacao,
                   t.id AS TransportadoraId, t.id AS Id, t.nome_razaosocial AS NomeRazaosocial,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            LEFT JOIN paises p ON p.id = e.pais_id
            ORDER BY v.placa
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Veiculos, Transportadoras, Estados, Paises, Veiculos>(
            sqlData,
            (veiculo, transportadora, estado, pais) =>
            {
                if (transportadora is not null)
                {
                    veiculo.VincularTransportadora(transportadora);
                }
                if (estado is not null)
                {
                    if (pais is not null)
                    {
                        estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                    }
                    veiculo.VincularEstado(estado);
                }
                return veiculo;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "TransportadoraId,EstadoId,PaisId"
        );

        return new ResultadoPaginado<Veiculos>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<Veiculos?> ObterVeiculoPorId(int id)
    {
        const string sql = @"
            SELECT v.id AS Id, v.placa AS Placa, v.rntrc AS Rntrc, v.renavam AS Renavam,
                   v.tipo_veiculo AS TipoVeiculo, v.marca_modelo AS MarcaModelo, v.ativo AS Ativo, v.criado_em AS CriadoEm,
                   v.observacao AS Observacao,
                   t.id AS TransportadoraId, t.nome_razaosocial AS NomeRazaosocial,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            LEFT JOIN paises p ON p.id = e.pais_id
            WHERE v.id = @Id;";

        var result = await _session.Connection.QueryAsync<Veiculos, Transportadoras, Estados, Paises, Veiculos>(
            sql,
            (veiculo, transportadora, estado, pais) =>
            {
                if (transportadora is not null)
                {
                    veiculo.VincularTransportadora(transportadora);
                }
                if (estado is not null)
                {
                    if (pais is not null)
                    {
                        estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                    }
                    veiculo.VincularEstado(estado);
                }
                return veiculo;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "TransportadoraId,EstadoId,PaisId"
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
                EstadoId = veiculo.Estado.Id,
                TransportadoraId = veiculo.Transportadora?.Id,
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
                EstadoId = veiculo.Estado.Id,
                TransportadoraId = veiculo.Transportadora?.Id,
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

    public async Task<ResultadoPaginado<Veiculos>> PesquisarVeiculos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = @"
            SELECT COUNT(*) FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            WHERE v.placa ILIKE @Termo OR v.marca_modelo ILIKE @Termo OR t.nome_razaosocial ILIKE @Termo OR e.uf ILIKE @Termo;";

        const string sqlData = @"
            SELECT v.id AS Id, v.placa AS Placa, v.rntrc AS Rntrc, v.renavam AS Renavam,
                   v.tipo_veiculo AS TipoVeiculo, v.marca_modelo AS MarcaModelo, v.ativo AS Ativo, v.criado_em AS CriadoEm,
                   v.observacao AS Observacao,
                   t.id AS TransportadoraId, t.nome_razaosocial AS NomeRazaosocial,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM veiculos v
            LEFT JOIN transportadoras t ON t.id = v.transportadora_id
            LEFT JOIN estados e ON e.id = v.estado_id
            LEFT JOIN paises p ON p.id = e.pais_id
            WHERE v.placa ILIKE @Termo OR v.marca_modelo ILIKE @Termo OR t.nome_razaosocial ILIKE @Termo OR e.uf ILIKE @Termo
            ORDER BY v.placa
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var result = await _session.Connection.QueryAsync<Veiculos, Transportadoras, Estados, Paises, Veiculos>(
            sqlData,
            (veiculo, transportadora, estado, pais) =>
            {
                if (transportadora is not null)
                {
                    veiculo.VincularTransportadora(transportadora);
                }
                if (estado is not null)
                {
                    if (pais is not null)
                    {
                        estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                    }
                    veiculo.VincularEstado(estado);
                }
                return veiculo;
            },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "TransportadoraId,EstadoId,PaisId"
        );

        return new ResultadoPaginado<Veiculos>(result.ToList(), total, pagina, tamanhoDaPagina);
    }
}
