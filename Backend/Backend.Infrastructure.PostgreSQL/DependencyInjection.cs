using Backend.Core.Common;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Logistica.Repositories;
using Backend.Core.Features.Logistica.Services;
using Backend.Core.Features.Localizacao.Services;
using Backend.Core.Features.Catalogo.Services;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Services;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Backend.Infrastructure.PostgreSQL.Features.Acesso;
using Backend.Infrastructure.PostgreSQL.Features.Catalogo;
using Backend.Infrastructure.PostgreSQL.Features.Estoque;
using Backend.Infrastructure.PostgreSQL.Features.Financeiro;
using Backend.Infrastructure.PostgreSQL.Features.Localizacao;
using Backend.Infrastructure.PostgreSQL.Features.Logistica;
using Backend.Infrastructure.PostgreSQL.Features.NFe;
using Backend.Infrastructure.PostgreSQL.Features.Parceiros;
using Backend.Infrastructure.PostgreSQL.Features.Vendas;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Infrastructure.PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgreSQLInfrastructure(this IServiceCollection services, string connectionString)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped(provider => new DbSession(connectionString));
        services.AddScoped<IUnitOfWork, PostgreSQLUnitOfWork>();

        services.AddScoped<IPaisesRepository, PaisesRepository>();
        services.AddScoped<IEstadosRepository, EstadosRepository>();
        services.AddScoped<ICidadesRepository, CidadesRepository>();
        services.AddScoped<IBairrosRepository, BairrosRepository>();

        services.AddScoped<PaisesService>();
        services.AddScoped<EstadosService>();
        services.AddScoped<CidadesService>();
        services.AddScoped<BairrosService>();

        services.AddScoped<IUsuariosRepository, UsuariosRepository>();
        services.AddScoped<ISessoesRepository, SessoesRepository>();

        services.AddScoped<ICategoriasRepository, CategoriasRepository>();
        services.AddScoped<IMarcasRepository, MarcasRepository>();
        services.AddScoped<IProdutosRepository, ProdutosRepository>();
        services.AddScoped<ISkusRepository, SkusRepository>();
        services.AddScoped<IUnidadesMedidaRepository, UnidadesMedidaRepository>();
        services.AddScoped<UnidadesMedidaService>();

        services.AddScoped<IMovimentacoesEstoquesRepository, MovimentacoesEstoquesRepository>();

        services.AddScoped<IContasPagarRepository, ContasPagarRepository>();
        services.AddScoped<IContasReceberRepository, ContasReceberRepository>();

        services.AddScoped<INfesRepository, NfesRepository>();

        services.AddScoped<IClientesRepository, ClientesRepository>();
        services.AddScoped<IEmitentesRepository, EmitentesRepository>();
        services.AddScoped<IFornecedoresRepository, FornecedoresRepository>();

        services.AddScoped<ClientesService>();
        services.AddScoped<EmitentesService>();
        services.AddScoped<FornecedoresService>();

        services.AddScoped<IVendasRepository, VendasRepository>();

        services.AddScoped<ITransportadorasRepository, TransportadorasRepository>();
        services.AddScoped<TransportadorasService>();

        services.AddScoped<IVeiculosRepository, VeiculosRepository>();
        services.AddScoped<VeiculosService>();

        return services;
    }
}