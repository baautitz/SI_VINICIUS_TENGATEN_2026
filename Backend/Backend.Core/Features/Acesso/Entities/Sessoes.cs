using Backend.Core.Common.Exceptions;
namespace Backend.Core.Features.Acesso.Entities;

public class Sessoes
{
    public long Id { get; private set; }
    public string Token { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }

    public Usuarios Usuario { get; private set; }

    public Sessoes(Usuarios usuario, string token, DateTime dataExpiracao)
    {
        if (usuario == null)
        {
            throw new DomainException("Usuário obrigatório para sessão.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Token de sessão é obrigatório.");
        }

        if (dataExpiracao <= DateTime.UtcNow)
        {
            throw new DomainException("Data de expiração deve ser no futuro.");
        }

        Usuario = usuario;
        Token = token;
        DataCriacao = DateTime.UtcNow;
        DataExpiracao = dataExpiracao;
        Ativo = true;
    }

    public Sessoes(long id, Usuarios usuario, string token, DateTime dataCriacao, DateTime? dataExpiracao, bool ativo)
    {
        if (usuario == null)
        {
            throw new DomainException("Usuário obrigatório para sessão.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Token de sessão é obrigatório.");
        }

        Id = id;
        Usuario = usuario;
        Token = token;
        DataCriacao = dataCriacao;
        DataExpiracao = dataExpiracao;
        Ativo = ativo;
    }

    public void Encerrar()
    {
        Ativo = false;
    }

    public void Prolongar(DateTime novaExpiracao)
    {
        if (novaExpiracao <= DateTime.UtcNow)
        {
            throw new DomainException("Nova data de expiração deve ser no futuro.");
        }

        if (!Ativo)
        {
            throw new DomainException("Não é possível prolongar sessão inativa.");
        }

        DataExpiracao = novaExpiracao;
    }
}
