using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface IPaisesRepository
{
  Task<ResultadoPaginado<PaisResumoDto>> ObterPaises(string? search, int pagina = 1, int tamanhoPagina = 20);
  Task<Paises?> ObterPaisPorId(int id);
  Task<Paises> CriarPais(Paises pais);
  Task<Paises> AtualizarPais(int id, Paises pais); 
  Task<bool> DeletarPais(int id);
  Task<bool> ExistePais(string siglaIso, string pais, int? ignorarId = null);
}

