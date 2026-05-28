using Backend.Core.Common;

namespace Backend.Core.Features.UnidadeMedida.Entities;

public class UnidadesMedida
{
    public int Id { get; set; }
    public string Sigla { get; private set; }
    public string Descricao { get; private set; }
    public string Categoria { get; private set; }
    public bool Ativo { get; private set; }

    public UnidadesMedida(string sigla, string descricao, string categoria, bool ativo = true)
    {
        sigla = TextNormalization.Normalize(sigla);
        descricao = TextNormalization.Normalize(descricao);
        categoria = TextNormalization.Normalize(categoria);

        if (string.IsNullOrWhiteSpace(sigla))
            throw new DomainException("Sigla da unidade de medida é obrigatória.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da unidade de medida é obrigatória.");

        if (string.IsNullOrWhiteSpace(categoria))
            throw new DomainException("Categoria da unidade de medida é obrigatória.");

        Sigla = sigla;
        Descricao = descricao;
        Categoria = categoria;
        Ativo = ativo;
    }

    public void AtualizarDescricao(string descricao)
    {
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da unidade de medida é obrigatória.");

        Descricao = descricao;
    }

    public void AtualizarCategoria(string categoria)
    {
        categoria = TextNormalization.Normalize(categoria);

        if (string.IsNullOrWhiteSpace(categoria))
            throw new DomainException("Categoria da unidade de medida é obrigatória.");

        Categoria = categoria;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
