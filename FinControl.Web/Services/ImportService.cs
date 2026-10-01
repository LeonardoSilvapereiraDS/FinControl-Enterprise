using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;
using FinControl.Infrastructure.Persistence;
using FinControl.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public sealed record ImportRow(DateTime Data, string Descricao, decimal Valor, string Chave);
public sealed class ImportService(FinControlDbContext db)
{
    public static List<ImportRow> Parse(string? content, string format, int account)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2_000_000) throw new RegraDeNegocioException("Envie um extrato CSV ou OFX de até 2 MB.");
        var raw = new List<(DateTime date, string description, decimal value, string? id)>();
        if (format.ToLowerInvariant() == "ofx")
        {
            var matches = Regex.Matches(content, @"<STMTTRN>(.*?)(?:</STMTTRN>|(?=<STMTTRN>)|(?=</BANKTRANLIST>))", RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
            foreach (Match match in matches)
            {
                string Field(string name) => Regex.Match(match.Groups[1].Value, "<"+name+@">([^<\r\n]+)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).Groups[1].Value.Trim();
                var d = Field("DTPOSTED");
                if (d.Length < 8 || !DateTime.TryParseExact(d[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw new RegraDeNegocioException("Data inválida no OFX.");
                var description = System.Net.WebUtility.HtmlDecode(Field("MEMO"));
                if (string.IsNullOrWhiteSpace(description)) description = System.Net.WebUtility.HtmlDecode(Field("NAME"));
                raw.Add((date, description, Money(Field("TRNAMT"), false), Field("FITID")));
            }
        }
        else if (format.ToLowerInvariant() == "csv")
        {
            var first = content.TrimStart('\uFEFF').Split('\n')[0];
            var separator = first.Contains(';') ? ';' : first.Contains('\t') ? '\t' : ',';
            var rows = Csv(content.TrimStart('\uFEFF'), separator);
            if (rows.Count < 2) throw new RegraDeNegocioException("O CSV deve conter cabeçalho e lançamentos.");
            string Normalize(string s) => string.Concat(s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
            var headers = rows[0].Select(Normalize).ToList();
            int di = headers.IndexOf("data"), vi = headers.IndexOf("valor"), de = headers.IndexOf("descricao"), id = headers.IndexOf("id");
            if (di < 0 || vi < 0 || de < 0) throw new RegraDeNegocioException("Cabeçalho esperado: data;descricao;valor;id (id é opcional).");
            foreach (var row in rows.Skip(1))
            {
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                if (row.Count <= Math.Max(di, Math.Max(vi, de))) throw new RegraDeNegocioException("Linha incompleta no CSV.");
                if (!DateTime.TryParseExact(row[di].Trim(), new[]{"yyyy-MM-dd", "dd/MM/yyyy"}, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw new RegraDeNegocioException("Data inválida. Use dd/MM/aaaa ou aaaa-MM-dd.");
                raw.Add((date, row[de].Trim(), Money(row[vi], true), id >= 0 && row.Count > id ? row[id].Trim() : null));
            }
        }
        else throw new RegraDeNegocioException("Formato não suportado. Use CSV ou OFX.");
        if (raw.Count == 0 || raw.Count > 5000) throw new RegraDeNegocioException("O extrato deve ter entre 1 e 5.000 lançamentos.");
        var occurrences = new Dictionary<string,int>();
        var result = new List<ImportRow>();
        foreach (var row in raw)
        {
            if (row.value == 0 || row.date.Year < 1900 || row.date.Year > 9998) throw new RegraDeNegocioException("O extrato contém valor zero ou data fora do intervalo permitido.");
            var desc = WebSupport.Text(row.description, "Descrição", 160);
            var canonical = $"{row.date:yyyy-MM-dd}|{row.value.ToString(CultureInfo.InvariantCulture)}|{desc}";
            occurrences[canonical] = occurrences.GetValueOrDefault(canonical) + 1;
            var source = string.IsNullOrWhiteSpace(row.id) ? canonical + "|" + occurrences[canonical] : "id|" + row.id;
            var key = account + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            result.Add(new(row.date, desc, row.value, key));
        }
        return result;
    }
    static decimal Money(string value, bool localized)
    {
        var s = value.Trim().Replace("R$", "").Trim();
        if (localized && s.Contains(',')) s = s.Replace(".", "").Replace(',', '.');
        if (!decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) || (amount > 999999999999.99m || amount < -999999999999.99m) || decimal.Round(amount,2) != amount)
            throw new RegraDeNegocioException("Valor monetário inválido no extrato.");
        return amount;
    }
    static List<List<string>> Csv(string text, char separator)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var field = new StringBuilder(); bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '"') { if (quoted && i + 1 < text.Length && text[i+1] == '"') { field.Append('"'); i++; } else quoted = !quoted; }
            else if (!quoted && (c == separator || c == '\n')) { row.Add(field.ToString().TrimEnd('\r')); field.Clear(); if (c == '\n') { rows.Add(row); row = new(); } }
            else field.Append(c);
        }
        if (quoted) throw new RegraDeNegocioException("Aspas não fechadas no CSV.");
        row.Add(field.ToString().TrimEnd('\r')); if (row.Any(x => x.Length > 0)) rows.Add(row); return rows;
    }
    public async Task<object> Import(int owner, ImportarRequest request)
    {
        var account = await db.Contas.SingleOrDefaultAsync(x => x.Id == request.ContaId && x.UsuarioId == owner && x.Ativa);
        var income = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaReceitaId && x.UsuarioId == owner && x.Ativa && x.Tipo == TipoCategoria.Receita);
        var expense = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaDespesaId && x.UsuarioId == owner && x.Ativa && x.Tipo == TipoCategoria.Despesa);
        if (account is null || income is null || expense is null) throw new RegraDeNegocioException("Escolha conta, categoria de receita e categoria de despesa válidas.");
        var rows = Parse(request.Conteudo, request.Formato, account.Id);
        var known = (await db.Importacoes.Where(x => x.UsuarioId == owner).Select(x => x.Chave).ToListAsync()).ToHashSet();
        var fresh = rows.Where(r => known.Add(r.Chave)).ToList();
        if (request.Confirmar)
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            foreach (var row in fresh)
            {
                var category = row.Valor > 0 ? income : expense;
                var t = new Transacao(row.Descricao, Math.Abs(row.Valor), row.Data, row.Valor > 0 ? TipoTransacao.Receita : TipoTransacao.Despesa, category.Id, category.Tipo, account.Id, owner, "Importado de extrato.");
                db.Transacoes.Add(t); await db.SaveChangesAsync();
                db.Importacoes.Add(new RegistroImportado { UsuarioId = owner, Chave = row.Chave, TransacaoId = t.Id });
            }
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        return new { total = rows.Count, novos = fresh.Count, duplicados = rows.Count - fresh.Count, confirmado = request.Confirmar, lancamentos = fresh.Take(50) };
    }
}
