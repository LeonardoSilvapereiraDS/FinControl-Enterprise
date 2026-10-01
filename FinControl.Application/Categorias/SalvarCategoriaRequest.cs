using FinControl.Domain.Enums;

namespace FinControl.Application.Categorias;

public sealed record SalvarCategoriaRequest(
    string Nome,
    TipoCategoria Tipo);
