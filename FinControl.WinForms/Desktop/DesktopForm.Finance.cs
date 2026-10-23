using System.Text.Json.Nodes;

namespace FinControl.WinForms.Desktop;

internal sealed partial class DesktopForm
{
    private static Option[] Kinds() => [new(1, "Receita"), new(2, "Despesa")];
    private IEnumerable<Option> Accounts(int current = 0) => state.Items("accounts").Where(a => a.Flag("ativa") || a.Int("id") == current).Select(a => new Option(a.Int("id"), a.Text("nome") + (a.Flag("ativa") ? "" : " (inativa)")));
    private IEnumerable<Option> Categories(int current = 0) => state.Items("categories").Where(c => c.Flag("ativa") || c.Int("id") == current).Select(c => new Option(c.Int("id"), c.Text("nome")));
    private string CategoryName(int id) => state.Items("categories").FirstOrDefault(c => c.Int("id") == id).Text("nome", "Categoria");
    private static string AccountKind(int id) => id switch { 1 => "Conta corrente", 2 => "Poupança", 3 => "Dinheiro", 4 => "Carteira digital", 5 => "Investimento", _ => "Outro" };

    private void RenderTransactions()
    {
        var bar = PageHeader(page, "Receitas e despesas do mês selecionado. Pendências só entram no saldo após confirmação.");
        AddAction(bar, "Novo lançamento", () => EditTransaction(), write: true, primary: true);
        AddAction(bar, "Editar", () => EditTransaction(Selected()), write: true);
        AddAction(bar, "Pago / pendente", async () =>
        {
            var row = Selected();
            await client.Patch(client.Path($"transactions/{row.Int("id")}/paid"), new { pago = !row.Flag("pago") }); await RefreshState();
        }, write: true);
        AddAction(bar, "Excluir", async () =>
        {
            var row = Selected(); if (!DesktopUi.Confirm(this, $"Excluir o lançamento “{row.Text("descricao") }”? Esta ação altera o saldo.")) return;
            await client.Delete(client.Path($"transactions/{row.Int("id")}")); await RefreshState();
        }, write: true);
        AddAction(bar, "Exportar CSV", ExportGrid);
        search = new TextBox { PlaceholderText = "Pesquisar descrição, conta ou categoria", Width = 290, AccessibleName = "Pesquisar lançamentos", Margin = new Padding(0, 8, 10, 4) };
        payment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, AccessibleName = "Situação do pagamento", Margin = new Padding(0, 8, 8, 4) };
        payment.Items.AddRange(["Todos", "Pagos", "Pendentes"]); payment.SelectedIndex = 0;
        bar.Controls.Add(search); bar.Controls.Add(payment);
        AttachGrid(["Descrição", "Data", "Tipo", "Categoria", "Conta", "Valor", "Situação"], TransactionRows());
        void Filter()
        {
            grid!.Rows.Clear(); foreach (var (node, cells) in TransactionRows()) { int i = grid.Rows.Add(cells); grid.Rows[i].Tag = node; }
            ColorTransactions();
        }
        search.TextChanged += (_, _) => Filter(); payment.SelectedIndexChanged += (_, _) => Filter();
        grid!.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0 && Editable) await Execute(() => EditTransaction(Selected())); };
        ColorTransactions();
    }
    private IEnumerable<(JsonNode, object[])> TransactionRows()
    {
        var query = Transactions().Where(t => page != "Receitas" || t.Int("tipo") == 1).Where(t => page != "Despesas" || t.Int("tipo") == 2);
        var term = search?.Text.Trim() ?? "";
        if (term.Length > 0) query = query.Where(t => $"{t.Text("descricao")} {t.Text("categoriaNome")} {t.Text("contaNome")}".Contains(term, StringComparison.CurrentCultureIgnoreCase));
        if (payment?.SelectedIndex == 1) query = query.Where(t => t.Flag("pago"));
        if (payment?.SelectedIndex == 2) query = query.Where(t => !t.Flag("pago"));
        return query.OrderByDescending(t => t.Date("data")).Select(t => (t, new object[] { t.Text("descricao"), t.Date("data").ToString("dd/MM/yyyy"), t.Int("tipo") == 1 ? "Receita" : "Despesa", t.Text("categoriaNome"), t.Text("contaNome"), Currency(t.Money("valor")), t.Flag("pago") ? "Pago" : t.Date("data") < DateTime.Today ? "Em atraso" : "Pendente" }));
    }
    private void ColorTransactions()
    {
        if (grid == null) return;
        foreach (DataGridViewRow row in grid.Rows) if (row.Tag is JsonNode t) row.Cells[5].Style.ForeColor = t.Int("tipo") == 1 ? DesktopTheme.Accent : DesktopTheme.Negative;
    }
    private async Task EditTransaction(JsonNode? row = null)
    {
        using var form = new EditorDialog(row == null ? "Novo lançamento" : "Editar lançamento");
        var kind = row?.Int("tipo") ?? (page == "Receitas" ? 1 : 2);
        form.TextField("descricao", "Descrição", row.Text("descricao"))
            .Select("tipo", "Tipo", Kinds(), kind).Number("valor", "Valor (R$)", row.Money("valor"))
            .DateField("data", "Data / vencimento", row?.Date("data"))
            .Select("contaId", "Conta", Accounts(row.Int("contaId")), row.Int("contaId"))
            .Select("categoriaId", "Categoria", Categories(row.Int("categoriaId")), row.Int("categoriaId"))
            .Check("pago", "Pagamento confirmado", row?.Flag("pago") ?? true)
            .TextField("observacao", "Observação", row.Text("observacao"), multiline: true);
        form.BindCategories("tipo", "categoriaId", state.Items("categories"), row.Int("categoriaId"));
        form.OnSave(async values =>
        {
            values["recorrente"] = row.Flag("recorrente");
            if (row == null) await client.Post(client.Path("transactions"), values);
            else await client.Put(client.Path($"transactions/{row.Int("id")}"), values);
        });
        if (form.ShowDialog(this) == DialogResult.OK) await RefreshState();
    }
    private string Resource => page switch { "Contas" => "accounts", "Categorias" => "categories", "Orçamentos" => "budgets", "Metas" => "goals", _ => "recurring" };
    private void RenderRecords()
    {
        var bar = PageHeader(page, page switch
        {
            "Contas" => "Carteiras e contas bancárias. Desativar uma conta preserva seu histórico e saldo.",
            "Categorias" => "Organize receitas e despesas para entender a origem de cada valor.",
            "Orçamentos" => "Limites por categoria e mês, comparados aos pagamentos confirmados.",
            "Metas" => "Acompanhe sua reserva, seus objetivos e os prazos de cada conquista.",
            _ => "Regras automáticas geram lançamentos pendentes enquanto o aplicativo está aberto."
        });
        AddAction(bar, "Adicionar", () => EditRecord(), write: true, primary: true);
        if (page != "Recorrências") AddAction(bar, "Editar", () => EditRecord(Selected()), write: true);
        if (page is "Contas" or "Categorias" or "Recorrências") AddAction(bar, "Ativar / desativar", async () =>
        {
            var item = Selected();
            if (!DesktopUi.Confirm(this, $"{(item.Flag("ativa") ? "Desativar" : "Ativar")} este registro?")) return;
            await client.Patch(client.Path($"{Resource}/{item.Int("id")}"), new { ativa = !item.Flag("ativa") }); await RefreshState();
        }, write: true);
        else AddAction(bar, "Excluir", async () =>
        {
            var item = Selected(); if (!DesktopUi.Confirm(this, "Excluir o registro selecionado?")) return;
            await client.Delete(client.Path($"{Resource}/{item.Int("id")}")); await RefreshState();
        }, write: true);
        if (page == "Recorrências") AddAction(bar, "Gerar pendências", async () =>
        {
            var result = await client.Post(client.Path("recurring/generate"), new { });
            await RefreshState(); MessageBox.Show(this, $"{result.Int("gerados")} lançamento(s) gerado(s).", "Recorrências");
        }, write: true);
        AddAction(bar, "Exportar CSV", ExportGrid);
        var rows = state.Items(Resource);
        switch (page)
        {
            case "Contas": AttachGrid(["Nome", "Tipo", "Saldo inicial", "Saldo atual", "Situação"], rows.Select(r => (r, new object[] { r.Text("nome"), AccountKind(r.Int("tipoConta")), Currency(r.Money("saldoInicial")), Currency(r.Money("saldo")), r.Flag("ativa") ? "Ativa" : "Inativa" }))); break;
            case "Categorias": AttachGrid(["Nome", "Tipo", "Situação"], rows.Select(r => (r, new object[] { r.Text("nome"), r.Int("tipo") == 1 ? "Receita" : "Despesa", r.Flag("ativa") ? "Ativa" : "Inativa" }))); break;
            case "Metas": AttachGrid(["Objetivo", "Acumulado", "Meta", "Progresso", "Prazo"], rows.Select(r => (r, new object[] { r.Text("nome"), Currency(r.Money("valorAtual")), Currency(r.Money("valorObjetivo")), r.Money("percentualProgresso").ToString("N1") + "%", r.Date("dataLimite").ToString("dd/MM/yyyy") }))); break;
            case "Orçamentos":
                AttachGrid(["Categoria", "Limite", "Realizado", "Disponível", "Utilização"], rows.Where(r => r.Int("mes") == month.Value.Month && r.Int("ano") == month.Value.Year).Select(r =>
                {
                    var used = Transactions().Where(t => t.Int("categoriaId") == r.Int("categoriaId") && t.Flag("pago")).Sum(t => t.Money("valor"));
                    var limit = r.Money("valorLimite");
                    return (r, new object[] { CategoryName(r.Int("categoriaId")), Currency(limit), Currency(used), Currency(limit - used), (limit == 0 ? 0 : used / limit * 100).ToString("N1") + "%" });
                })); break;
            default: AttachGrid(["Descrição", "Valor", "Frequência", "Próxima data", "Situação"], rows.Select(r => (r, new object[] { r.Text("descricao"), Currency(r.Money("valor")), r.Text("frequencia"), r.Date("proximaData").ToString("dd/MM/yyyy"), r.Flag("ativa") ? "Ativa" : "Inativa" }))); break;
        }
    }
    private async Task EditRecord(JsonNode? row = null)
    {
        using var form = new EditorDialog((row == null ? "Adicionar • " : "Editar • ") + page);
        switch (page)
        {
            case "Contas": form.TextField("nome", "Nome da conta", row.Text("nome"))
                .Select("tipoConta", "Tipo", new[] { 1, 2, 3, 4, 5, 99 }.Select(x => new Option(x, AccountKind(x))), row?.Int("tipoConta") ?? 1)
                .Number("saldoInicial", "Saldo inicial (R$)", row.Money("saldoInicial"), signed: true); break;
            case "Categorias": form.TextField("nome", "Nome", row.Text("nome")).Select("tipo", "Tipo", Kinds(), row?.Int("tipo") ?? 2); break;
            case "Metas": form.TextField("nome", "Objetivo", row.Text("nome"))
                .TextField("descricao", "Descrição", row.Text("descricao"))
                .Number("valorObjetivo", "Valor do objetivo (R$)", row.Money("valorObjetivo"))
                .Number("valorAtual", "Valor acumulado (R$)", row.Money("valorAtual"))
                .DateField("dataInicio", "Data de início", row?.Date("dataInicio"))
                .DateField("dataLimite", "Prazo", row?.Date("dataLimite") ?? DateTime.Today.AddYears(1)); break;
            case "Orçamentos": form.Select("categoriaId", "Categoria", Categories(row.Int("categoriaId")), row.Int("categoriaId"))
                .Number("valorLimite", "Limite mensal (R$)", row.Money("valorLimite"))
                .Number("mes", "Mês (1 a 12)", row?.Int("mes") ?? month.Value.Month, decimals: 0)
                .Number("ano", "Ano", row?.Int("ano") ?? month.Value.Year, decimals: 0);
                if (row != null) form.Lock("categoriaId"); break;
            default: form.TextField("descricao", "Descrição").Select("tipo", "Tipo", Kinds(), 2)
                .Number("valor", "Valor (R$)").Select("contaId", "Conta", Accounts())
                .Select("categoriaId", "Categoria", Categories())
                .Select("frequencia", "Frequência", new[] { "Semanal", "Mensal", "Anual" }.Select(x => new Option(x, x)), "Mensal")
                .DateField("inicio", "Primeiro vencimento").Check("temFim", "Definir data final")
                .DateField("fim", "Data final (se marcada)", DateTime.Today.AddYears(1))
                .Note("Os lançamentos são gerados como pendentes. Regras anteriores a hoje também serão processadas. Para alterar uma regra, desative-a e crie outra.");
                form.BindCategories("tipo", "categoriaId", state.Items("categories")); break;
        }
        var resource = Resource;
        form.OnSave(async values =>
        {
            if (resource == "recurring") { if (!values.Flag("temFim")) values["fim"] = null; values.Remove("temFim"); }
            if (resource == "budgets") { values["mes"] = decimal.ToInt32(values.Money("mes")); values["ano"] = decimal.ToInt32(values.Money("ano")); }
            if (row == null) await client.Post(client.Path(resource), values);
            else await client.Put(client.Path($"{resource}/{row.Int("id")}"), values);
        });
        if (form.ShowDialog(this) == DialogResult.OK) await RefreshState();
    }
}
