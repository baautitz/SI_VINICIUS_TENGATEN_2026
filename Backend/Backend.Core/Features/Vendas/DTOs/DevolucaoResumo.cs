using Backend.Core.Features.Vendas.Entities.Enums;

namespace Backend.Core.Features.Vendas.DTOs;

public sealed record DevolucaoResumo(int Id, int VendaId, string ClienteNome, DateTime DataDevolucao, decimal ValorTotal, string Motivo, StatusDevolucao Status);
