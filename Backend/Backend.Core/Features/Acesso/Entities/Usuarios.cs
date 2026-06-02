using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Acesso.Entities;

public class Usuarios
{
    private readonly List<Sessoes> _sessoes = new();

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string CpfCnpj { get; private set; }
    public string Email { get; private set; }
    public string? Telefone { get; private set; }
    public string Usuario { get; private set; }
    public string Senha { get; private set; }
    public bool Ativo { get; private set; }

    public IReadOnlyCollection<Sessoes> Sessoes => _sessoes.AsReadOnly();

    public Usuarios(string nome, string cpfCnpj, string email, string usuario, string senha, string? telefone = null)
    {
        nome = TextNormalization.Normalize(nome);
        cpfCnpj = new DocumentoGenerico(cpfCnpj).Valor;
        email = TextNormalization.Normalize(email);
        usuario = TextNormalization.Normalize(usuario);
        telefone = TextNormalization.NormalizeOrNull(telefone);

        ValidarUsuario(nome, cpfCnpj, email, usuario, senha);

        Nome = nome;
        CpfCnpj = cpfCnpj;
        Email = email;
        Usuario = usuario;
        Senha = senha;
        Telefone = telefone;
        Ativo = true;
    }

    public Usuarios(int id, string nome, string cpfCnpj, string email, string usuario, string senha, string? telefone = null, bool ativo = true)
        : this(nome, cpfCnpj, email, usuario, senha, telefone)
    {
        Id = id;
        if (!ativo)
            Desativar();
    }

    public void AdicionarSessao(Sessoes sessao)
    {
        if (sessao == null)
            throw new DomainException("Sessão é obrigatória.");

        _sessoes.Add(sessao);
    }

    public void Atualizar(string nome, string cpfCnpj, string email, string usuario, string? telefone = null)
    {
        nome = TextNormalization.Normalize(nome);
        cpfCnpj = new DocumentoGenerico(cpfCnpj).Valor;
        email = TextNormalization.Normalize(email);
        usuario = TextNormalization.Normalize(usuario);
        telefone = TextNormalization.NormalizeOrNull(telefone);

        ValidarUsuario(nome, cpfCnpj, email, usuario, Senha);

        Nome = nome;
        CpfCnpj = cpfCnpj;
        Email = email;
        Usuario = usuario;
        Telefone = telefone;
    }

    public void AlterarSenha(string senhaNova)
    {
        if (string.IsNullOrWhiteSpace(senhaNova))
        {
            throw new DomainException("A senha não pode ser vazia.");
        }

        Senha = senhaNova;
    }

    public void Ativar()
    {
        Ativo = true;
    }

    public void Desativar()
    {
        Ativo = false;
    }

    public Sessoes IniciarSessao(string token, DateTime dataExpiracao)
    {
        if (!Ativo)
        {
            throw new DomainException("Usuário inativo não pode iniciar sessão.");
        }

        var sessao = new Sessoes(this, token, dataExpiracao);
        _sessoes.Add(sessao);
        return sessao;
    }

    public void EncerrarSessao(Sessoes sessao)
    {
        if (!_sessoes.Contains(sessao))
        {
            throw new DomainException("Sessão não pertence ao usuário.");
        }

        sessao.Encerrar();
    }

    private static void ValidarUsuario(string nome, string cpfCnpj, string email, string usuario, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(cpfCnpj))
        {
            throw new DomainException("CPF/CNPJ é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new DomainException("Usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new DomainException("Senha é obrigatória.");
        }
    }
}
