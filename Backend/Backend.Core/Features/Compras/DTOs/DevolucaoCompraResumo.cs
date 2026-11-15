using Backend.Core.Features.Compras.Entities.Enums;

namespace Backend.Core.Features.Compras.DTOs;

public sealed record DevolucaoCompraResumo(int Id, int CompraId, string FornecedorNome, DateTime DataDevolucao, decimal ValorTotal, string Motivo, StatusDevolucaoCompra Status);
