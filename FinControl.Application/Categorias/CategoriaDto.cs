using FinControl.Domain.Enums;

namespace FinControl.Application.Categorias;

public sealed record CategoriaDto(
    int Id,
    string Nome,
    TipoCategoria Tipo,
    bool Ativa);
