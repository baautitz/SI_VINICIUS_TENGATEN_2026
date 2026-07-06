using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Parceiros;

public class TransportadorasRepository : ITransportadorasRepository
{
    private readonly DbSession _session;

    public TransportadorasRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Transportadoras>> ObterTransportadoras(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = "SELECT COUNT(*) FROM transportadoras;";
        const string sqlData = @"
            SELECT t.id AS Id, t.tipo_pessoa AS TipoPessoa, t.nome_razaosocial AS NomeRazaoSocial, t.cpf_cnpj AS CpfCnpj, t.rg_ie AS RgIe, t.apelido_nomefantasia AS ApelidoNomefantasia,
                   t.logradouro AS Logradouro, t.numero AS Numero, t.sexo AS Sexo, t.data_nascimento AS DataNascimento, t.telefone AS Telefone, t.email AS Email, t.rntrc AS Rntrc, t.ativo AS Ativo, t.criado_em AS CriadoEm,
                   t.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   pe.id AS PaisEstadoId, pe.id AS Id, pe.pais, pe.codigo_iso_pais, pe.ddi, pe.codigo_iso_moeda, pe.simbolo_moeda,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM transportadoras t
            INNER JOIN paises p ON p.id = t.nacionalidade_id
            LEFT JOIN bairros b ON b.id = t.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises pe ON pe.id = e.pais_id
            ORDER BY t.criado_em DESC, t.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Transportadoras, Bairros, Cidades, Estados, Paises, Paises, Transportadoras>(
            sqlData,
            (transportadora, bairro, cidade, estado, paisEstado, pais) =>
            {
                if (paisEstado is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, paisEstado);
                if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);

                transportadora.Atualizar(transportadora.TipoPessoa, transportadora.NomeRazaosocial, transportadora.CpfCnpj, pais!, transportadora.RgIe, transportadora.ApelidoNomefantasia, transportadora.Logradouro, transportadora.Numero, bairro, transportadora.Telefone, transportadora.Email, transportadora.Rntrc, transportadora.Observacao, transportadora.Sexo, transportadora.DataNascimento);
                return transportadora;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisEstadoId,PaisId"
        );

        return new ResultadoPaginado<Transportadoras>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Transportadoras?> ObterTransportadoraPorId(int id)
    {
        const string sql = @"
            SELECT t.id AS Id, t.tipo_pessoa AS TipoPessoa, t.nome_razaosocial AS NomeRazaoSocial, t.cpf_cnpj AS CpfCnpj, t.rg_ie AS RgIe, t.apelido_nomefantasia AS ApelidoNomefantasia,
                   t.logradouro AS Logradouro, t.numero AS Numero, t.sexo AS Sexo, t.data_nascimento AS DataNascimento, t.telefone AS Telefone, t.email AS Email, t.rntrc AS Rntrc, t.ativo AS Ativo, t.criado_em AS CriadoEm,
                   t.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   pe.id AS PaisEstadoId, pe.id AS Id, pe.pais, pe.codigo_iso_pais, pe.ddi, pe.codigo_iso_moeda, pe.simbolo_moeda,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda,
                   v.id AS VeiculoId, v.id AS Id, v.placa, v.rntrc, v.renavam, v.tipo_veiculo, v.marca_modelo, v.ativo, v.observacao,
                   ev.id AS VeiculoEstadoId, ev.id AS Id, ev.estado, ev.uf,
                   pv.id AS VeiculoPaisId, pv.id AS Id, pv.pais, pv.codigo_iso_pais, pv.ddi, pv.codigo_iso_moeda, pv.simbolo_moeda
            FROM transportadoras t
            INNER JOIN paises p ON p.id = t.nacionalidade_id
            LEFT JOIN bairros b ON b.id = t.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises pe ON pe.id = e.pais_id
            LEFT JOIN veiculos v ON v.transportadora_id = t.id
            LEFT JOIN estados ev ON ev.id = v.estado_id
            LEFT JOIN paises pv ON pv.id = ev.pais_id
            WHERE t.id = @Id;";

        var transportadoraDict = new Dictionary<int, Transportadoras>();
        var types = new[] { typeof(Transportadoras), typeof(Bairros), typeof(Cidades), typeof(Estados), typeof(Paises), typeof(Paises), typeof(Veiculos), typeof(Estados), typeof(Paises) };

        await _session.Connection.QueryAsync(
            sql,
            types,
            (objs) =>
            {
                var transportadora = (Transportadoras)objs[0];
                var bairro = (Bairros)objs[1];
                var cidade = (Cidades)objs[2];
                var estado = (Estados)objs[3];
                var paisEstado = (Paises)objs[4];
                var pais = (Paises)objs[5];
                var veiculo = (Veiculos)objs[6];
                var veiculoEstado = (Estados)objs[7];
                var veiculoPais = (Paises)objs[8];

                if (!transportadoraDict.TryGetValue(transportadora.Id, out var transportadoraEntry))
                {
                    transportadoraEntry = transportadora;
                    if (paisEstado is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, paisEstado);
                    if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                    if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);

                    transportadoraEntry.Atualizar(transportadoraEntry.TipoPessoa, transportadoraEntry.NomeRazaosocial, transportadoraEntry.CpfCnpj, pais!, transportadoraEntry.RgIe, transportadoraEntry.ApelidoNomefantasia, transportadoraEntry.Logradouro, transportadoraEntry.Numero, bairro, transportadoraEntry.Telefone, transportadoraEntry.Email, transportadoraEntry.Rntrc, transportadoraEntry.Observacao, transportadoraEntry.Sexo, transportadoraEntry.DataNascimento);

                    transportadoraDict.Add(transportadoraEntry.Id, transportadoraEntry);
                }

                if (veiculo is not null)
                {
                    if (veiculoPais is not null && veiculoEstado is not null) veiculoEstado.AtualizarResultado(veiculoEstado.Estado, veiculoEstado.Uf, veiculoPais);
                    if (veiculoEstado is not null) veiculo.VincularEstado(veiculoEstado);
                    transportadoraEntry.AdicionarVeiculo(veiculo);
                }

                return transportadoraEntry;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisEstadoId,PaisId,VeiculoId,VeiculoEstadoId,VeiculoPaisId"
        );

        return transportadoraDict.Values.SingleOrDefault();
    }

    public async Task<Transportadoras> CriarTransportadora(Transportadoras transportadora)
    {
        const string sql = @"
            INSERT INTO transportadoras (tipo_pessoa, nome_razaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia,
                                         logradouro, numero, sexo, data_nascimento, bairro_id, nacionalidade_id, telefone, email, rntrc,
                                         ativo, criado_em, observacao)
            VALUES (@TipoPessoa::tipo_pessoa, @NomeRazaosocial, @CpfCnpj, @RgIe, @ApelidoNomefantasia,
                    @Logradouro, @Numero, @Sexo, @DataNascimento, @BairroId, @NacionalidadeId, @Telefone, @Email, @Rntrc,
                    @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                TipoPessoa = transportadora.TipoPessoa.ToString(),
                transportadora.NomeRazaosocial,
                transportadora.CpfCnpj,
                transportadora.RgIe,
                transportadora.ApelidoNomefantasia,
                transportadora.Logradouro,
                transportadora.Numero,
                transportadora.Sexo,
                transportadora.DataNascimento,
                BairroId = transportadora.Bairro?.Id,
                NacionalidadeId = transportadora.Nacionalidade.Id,
                transportadora.Telefone,
                transportadora.Email,
                transportadora.Rntrc,
                transportadora.Ativo,
                CriadoEm = DateTime.UtcNow,
                transportadora.Observacao
            },
            transaction: _session.Transaction
        );

        transportadora.Id = idGerado;
        return transportadora;
    }

    public async Task<Transportadoras> AtualizarTransportadora(int id, Transportadoras transportadora)
    {
        const string sql = @"
            UPDATE transportadoras
            SET tipo_pessoa = @TipoPessoa::tipo_pessoa, nome_razaosocial = @NomeRazaosocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nomefantasia = @ApelidoNomefantasia,
                logradouro = @Logradouro, numero = @Numero, sexo = @Sexo, data_nascimento = @DataNascimento, bairro_id = @BairroId, nacionalidade_id = @NacionalidadeId,
                telefone = @Telefone, email = @Email, rntrc = @Rntrc, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                TipoPessoa = transportadora.TipoPessoa.ToString(),
                transportadora.NomeRazaosocial,
                transportadora.CpfCnpj,
                transportadora.RgIe,
                transportadora.ApelidoNomefantasia,
                transportadora.Logradouro,
                transportadora.Numero,
                transportadora.Sexo,
                transportadora.DataNascimento,
                BairroId = transportadora.Bairro?.Id,
                NacionalidadeId = transportadora.Nacionalidade.Id,
                transportadora.Telefone,
                transportadora.Email,
                transportadora.Rntrc,
                transportadora.Ativo,
                AtualizadoEm = DateTime.UtcNow,
                transportadora.Observacao
            },
            transaction: _session.Transaction
        );

        transportadora.Id = id;
        return transportadora;
    }

    public async Task<bool> DeletarTransportadora(int id)
    {
        const string sql = "DELETE FROM transportadoras WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql, new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<Transportadoras>> PesquisarTransportadoras(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = @"
            SELECT COUNT(*) FROM transportadoras
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;";

        const string sqlData = @"
            SELECT t.id AS Id, t.tipo_pessoa AS TipoPessoa, t.nome_razaosocial AS NomeRazaoSocial, t.cpf_cnpj AS CpfCnpj, t.rg_ie AS RgIe, t.apelido_nomefantasia AS ApelidoNomefantasia,
                   t.logradouro AS Logradouro, t.numero AS Numero, t.sexo AS Sexo, t.data_nascimento AS DataNascimento, t.telefone AS Telefone, t.email AS Email, t.rntrc AS Rntrc, t.ativo AS Ativo, t.criado_em AS CriadoEm,
                   t.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   pe.id AS PaisEstadoId, pe.id AS Id, pe.pais, pe.codigo_iso_pais, pe.ddi, pe.codigo_iso_moeda, pe.simbolo_moeda,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM transportadoras t
            INNER JOIN paises p ON p.id = t.nacionalidade_id
            LEFT JOIN bairros b ON b.id = t.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises pe ON pe.id = e.pais_id
            WHERE t.nome_razaosocial ILIKE @Termo OR t.cpf_cnpj ILIKE @Termo
               OR t.apelido_nomefantasia ILIKE @Termo OR t.email ILIKE @Termo
            ORDER BY t.criado_em DESC, t.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Transportadoras, Bairros, Cidades, Estados, Paises, Paises, Transportadoras>(
            sqlData,
            (transportadora, bairro, cidade, estado, paisEstado, pais) =>
            {
                if (paisEstado is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, paisEstado);
                if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);

                transportadora.Atualizar(transportadora.TipoPessoa, transportadora.NomeRazaosocial, transportadora.CpfCnpj, pais!, transportadora.RgIe, transportadora.ApelidoNomefantasia, transportadora.Logradouro, transportadora.Numero, bairro, transportadora.Telefone, transportadora.Email, transportadora.Rntrc, transportadora.Observacao, transportadora.Sexo, transportadora.DataNascimento);
                return transportadora;
            },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisEstadoId,PaisId"
        );

        return new ResultadoPaginado<Transportadoras>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteTransportadoraCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM transportadoras
            WHERE cpf_cnpj = @CpfCnpj AND nacionalidade_id = @NacionalidadeId";

        if (ignorarId.HasValue)
        {
            sql += " AND id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, NacionalidadeId = nacionalidadeId, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}

