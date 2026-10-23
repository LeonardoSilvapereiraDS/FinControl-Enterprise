using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FinControl.WinForms.Desktop;

internal sealed partial class DesktopForm
{
    private void RenderImport()
    {
        var bar = PageHeader("Importar extrato", "Traga seu extrato CSV ou OFX. Confira a prévia antes de confirmar a importação.");
        AddAction(bar, "Selecionar arquivo", ImportStatement, write: true, primary: true);
        var help = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None,
            Text = "COMO IMPORTAR\r\n\r\n1. Selecione um arquivo CSV (UTF-8 ou Windows-1252) ou OFX de até 2 MB.\r\n2. Escolha a conta e as categorias de entrada e saída.\r\n3. Confira as primeiras 50 linhas e o número de duplicatas.\r\n4. Confirme para salvar.\r\n\r\nCabeçalho CSV: data;descricao;valor;id\r\nExemplo: 23/10/2026;Pagamento recebido;150,00;banco-001\r\n\r\nValores negativos são despesas. O campo id é opcional.\r\nLançamentos importados do extrato são considerados pagos.\r\n\r\nA detecção de duplicatas evita importar novamente o mesmo movimento na mesma conta." };
        content.Controls.Add(help); help.BringToFront();
    }
    private async Task ImportStatement()
    {
        using var file = new OpenFileDialog { Filter = "Extratos (*.csv;*.ofx)|*.csv;*.ofx", CheckFileExists = true };
        if (file.ShowDialog(this) != DialogResult.OK) return;
        if (new FileInfo(file.FileName).Length > 2_000_000) throw new InvalidOperationException("Selecione um extrato de até 2 MB.");
        var bytes = await File.ReadAllBytesAsync(file.FileName);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); text = Encoding.GetEncoding(1252).GetString(bytes); }
        using var form = new EditorDialog("Preparar importação", System.IO.Path.GetFileName(file.FileName));
        form.Select("contaId", "Conta de destino", Accounts())
            .Select("categoriaReceitaId", "Categoria das receitas", state.Items("categories").Where(c => c.Flag("ativa") && c.Int("tipo") == 1).Select(c => new Option(c.Int("id"), c.Text("nome"))))
            .Select("categoriaDespesaId", "Categoria das despesas", state.Items("categories").Where(c => c.Flag("ativa") && c.Int("tipo") == 2).Select(c => new Option(c.Int("id"), c.Text("nome"))));
        JsonObject? payload = null; JsonNode? preview = null;
        form.OnSave(async values =>
        {
            values["conteudo"] = text; values["formato"] = System.IO.Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant(); values["confirmar"] = false;
            preview = await client.Post(client.Path("import"), values); payload = values;
        }, "Ver prévia");
        if (form.ShowDialog(this) != DialogResult.OK || payload == null || preview == null) return;
        using var review = new Form { Text = "Conferir extrato", StartPosition = FormStartPosition.CenterParent, Size = new Size(850, 560), MinimumSize = new Size(620, 400), Font = Font, ShowInTaskbar = false };
        var heading = new Label { Dock = DockStyle.Top, Height = 60, Padding = new Padding(16), Text = $"Total: {preview.Int("total")}   •   Novos: {preview.Int("novos")}   •   Duplicados: {preview.Int("duplicados")}\nPrévia dos primeiros 50 lançamentos novos." };
        var list = DesktopUi.Grid(); list.Columns.Add("data", "Data"); list.Columns.Add("descricao", "Descrição"); list.Columns.Add("valor", "Valor");
        foreach (var row in preview.Items("lancamentos")) list.Rows.Add(row.Date("data").ToString("dd/MM/yyyy"), row.Text("descricao"), Currency(row.Money("valor")));
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 65, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft };
        var confirm = DesktopUi.Button("Confirmar importação", async () =>
        {
            payload["confirmar"] = true; var result = await client.Post(client.Path("import"), payload);
            MessageBox.Show(review, $"{result.Int("novos")} lançamento(s) importado(s).", "Importação concluída"); review.DialogResult = DialogResult.OK; review.Close();
        }, true);
        confirm.Enabled = preview.Int("novos") > 0; buttons.Controls.Add(confirm);
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true, Height = 38 });
        review.Controls.Add(list); review.Controls.Add(heading); review.Controls.Add(buttons); DesktopTheme.Apply(review);
        if (review.ShowDialog(this) == DialogResult.OK) await RefreshState();
    }
    private void RenderSettings()
    {
        var bar = PageHeader("Configurações", "Gerencie seu espaço, os acessos e as cópias dos dados financeiros.");
        AddAction(bar, "Novo espaço", CreateWorkspace, primary: true);
        AddAction(bar, "Editar espaço", EditWorkspace, admin: true);
        AddAction(bar, "Equipe e permissões", Members, admin: true);
        AddAction(bar, "Alterar senha", ChangePassword);
        AddAction(bar, "Salvar backup", SaveBackup, admin: true);
        AddAction(bar, "Restaurar backup", RestoreBackup);
        var description = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None,
            Text = $"ESPAÇO ATUAL\r\n\r\nNome: {state["workspace"].Text("nome")}\r\nPerfil: {state["workspace"].Text("perfil")}\r\nSeu acesso: {state.Text("role")}\r\n\r\nAPARÊNCIA\r\n\r\nUse o seletor no topo para escolher Claro, Escuro ou Sistema. A preferência é salva neste computador.\r\n\r\nDADOS E BACKUP\r\n\r\nA versão Windows usa o mesmo banco local da versão web. O backup exporta os dados financeiros do espaço selecionado, sem senhas. A restauração cria outro espaço e preserva os atuais.\r\n\r\nGuarde o arquivo de backup em um local seguro; ele contém informações financeiras.\r\n\r\nFUNCIONAMENTO\r\n\r\nO serviço inicia e encerra junto com esta janela. As recorrências são processadas com o aplicativo aberto e recuperam os vencimentos anteriores ao abrir novamente." };
        content.Controls.Add(description); description.BringToFront();
    }
    private async Task CreateWorkspace()
    {
        using var form = new EditorDialog("Novo espaço financeiro"); form.TextField("nome", "Nome do espaço").Select("perfil", "Perfil", Profiles());
        form.OnSave(async values => { var response = await client.Post("/api/workspaces", values); client.WorkspaceId = response.Int("id"); });
        if (form.ShowDialog(this) == DialogResult.OK) await LoadSession();
    }
    private async Task EditWorkspace()
    {
        using var form = new EditorDialog("Editar espaço"); form.TextField("nome", "Nome", state["workspace"].Text("nome")).Select("perfil", "Perfil", Profiles(), state["workspace"].Text("perfil"));
        form.OnSave(async values => { await client.Put(client.Path("settings"), values); });
        if (form.ShowDialog(this) == DialogResult.OK) await LoadSession();
    }
    private async Task ChangePassword()
    {
        using var form = new EditorDialog("Alterar senha", "Use de 10 a 128 caracteres. Outras sessões serão encerradas.");
        form.TextField("senhaAtual", "Senha atual", password: true).TextField("novaSenha", "Nova senha", password: true).TextField("confirmacao", "Repita a nova senha", password: true);
        form.OnSave(async values => { if (values.Text("novaSenha") != values.Text("confirmacao")) throw new InvalidOperationException("As senhas não coincidem."); values.Remove("confirmacao"); await client.Put("/api/auth/password", values); });
        if (form.ShowDialog(this) == DialogResult.OK) await RefreshState();
    }
    private async Task SaveBackup()
    {
        using var file = new SaveFileDialog { Filter = "Backup FinControl (*.json)|*.json", FileName = $"FinControl-backup-{DateTime.Now:yyyyMMdd-HHmm}.json", OverwritePrompt = true };
        if (file.ShowDialog(this) != DialogResult.OK) return;
        var backup = await client.Get(client.Path("backup"));
        await File.WriteAllTextAsync(file.FileName, backup.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        status.Text = "Backup financeiro salvo com sucesso.";
    }
    private async Task RestoreBackup()
    {
        using var file = new OpenFileDialog { Filter = "Backup FinControl (*.json)|*.json" };
        if (file.ShowDialog(this) != DialogResult.OK) return;
        if (new FileInfo(file.FileName).Length > 8 * 1024 * 1024) throw new InvalidOperationException("O backup excede o limite de 8 MB.");
        var backup = JsonNode.Parse(await File.ReadAllTextAsync(file.FileName)) ?? throw new InvalidOperationException("Arquivo vazio.");
        if (!DesktopUi.Confirm(this, "Restaurar este backup em um novo espaço? Os espaços atuais serão preservados.")) return;
        var result = await client.Post("/api/restore", backup); client.WorkspaceId = result.Int("id"); await LoadSession();
        MessageBox.Show(this, "Backup restaurado em outro espaço financeiro.", "Restauração concluída");
    }
    private async Task Members()
    {
        using var window = new Form { Text = "Equipe e permissões", Font = Font, Size = new Size(850, 540), MinimumSize = new Size(650, 400), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false };
        var table = DesktopUi.Grid(); foreach (var name in new[] { "Nome", "E-mail", "Permissão" }) table.Columns.Add(name, name);
        async Task Reload()
        {
            var members = await client.Get(client.Path("members")); table.Rows.Clear();
            foreach (var member in members.AsArray().OfType<JsonNode>()) { int i = table.Rows.Add(member.Text("nome"), member.Text("email"), member.Text("papel")); table.Rows[i].Tag = member; }
        }
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(12) };
        bar.Controls.Add(DesktopUi.Button("Adicionar / alterar acesso", async () =>
        {
            using var form = new EditorDialog("Permissão de acesso", "A pessoa precisa ter criado uma conta neste FinControl. Informar um e-mail já associado atualiza sua permissão.");
            form.TextField("email", "E-mail").Select("papel", "Permissão", new[] { "Leitor", "Editor", "Administrador" }.Select(x => new Option(x, x)));
            form.OnSave(async values => { await client.Post(client.Path("members"), values); });
            if (form.ShowDialog(window) == DialogResult.OK) await Reload();
        }, true));
        bar.Controls.Add(DesktopUi.Button("Remover acesso", async () =>
        {
            var row = table.CurrentRow?.Tag as JsonNode ?? throw new InvalidOperationException("Selecione um membro.");
            if (!DesktopUi.Confirm(window, $"Remover o acesso de {row.Text("nome")}?")) return;
            await client.Delete(client.Path($"members/{row.Int("id")}")); await Reload();
        }));
        window.Controls.Add(table); window.Controls.Add(bar); await Reload(); DesktopTheme.Apply(window); window.ShowDialog(this); await RefreshState();
    }
}
