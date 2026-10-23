using System.Text.Json.Nodes;

namespace FinControl.WinForms.Desktop;

internal sealed partial class DesktopForm
{
    // Executado apenas com --smoke-test; Program isola o banco antes de iniciar o host.
    private static string SmokeOutput => Environment.GetEnvironmentVariable("FINCONTROL_SMOKE_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "FinControl-desktop-evidence");
    private static void WriteSmokeFailure(Exception ex) { Directory.CreateDirectory(SmokeOutput); File.WriteAllText(Path.Combine(SmokeOutput, "failure.txt"), ex.ToString()); }
    private async Task RunSmoke()
    {
        Directory.CreateDirectory(SmokeOutput);
        var checks = new List<string>();
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        await client.Post("/api/auth/register", new { nome = "Equipe Aurora", email = "desktop@example.test", senha = "Uma frase segura 2026!", perfil = "Empresa", espaco = "Aurora Studio" });
        await LoadSession();
        var account = state.Items("accounts").First().Int("id");
        var income = state.Items("categories").First(x => x.Int("tipo") == 1).Int("id");
        var expense = state.Items("categories").First(x => x.Int("tipo") == 2).Int("id");
        await client.Put(client.Path($"accounts/{account}"), new { nome = "Conta principal", tipoConta = 1, saldoInicial = 12500m });
        for (int offset = -5; offset <= 0; offset++)
        {
            var date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(offset);
            foreach (var t in new[] { ("Serviços de design", 7800m + offset * 100, 1, income), ("Aluguel do escritório", 1850m, 2, expense), ("Plataformas e ferramentas", 429.90m, 2, expense) })
                await client.Post(client.Path("transactions"), new { descricao = t.Item1, valor = t.Item2, data = date, tipo = t.Item3, categoriaId = t.Item4, contaId = account, observacao = "Dados fictícios de teste", pago = true, recorrente = false });
        }
        await client.Post(client.Path("transactions"), new { descricao = "Projeto a receber", valor = 1800m, data = DateTime.Today, tipo = 1, categoriaId = income, contaId = account, observacao = "Demonstração", pago = false, recorrente = false });
        await client.Post(client.Path("goals"), new { nome = "Reserva do negócio", descricao = "Capital de giro", valorObjetivo = 30000m, valorAtual = 12400m, dataInicio = DateTime.Today, dataLimite = DateTime.Today.AddYears(1) });
        await client.Post(client.Path("budgets"), new { categoriaId = expense, valorLimite = 3000m, mes = DateTime.Today.Month, ano = DateTime.Today.Year });
        await client.Post(client.Path("recurring"), new { descricao = "Assinatura mensal", valor = 99m, tipo = 2, categoriaId = expense, contaId = account, frequencia = "Mensal", inicio = DateTime.Today.AddMonths(1), fim = (DateTime?)null });
        await RefreshState();
        Check(state.Items("transactions").Count() == 19, "Os lançamentos chegam à interface nativa");
        Check(state["dashboard"].Money("saldoGeral") == 44120.60m, "O saldo ignora o recebimento pendente");
        ready = true; sidebar.Enabled = topbar.Enabled = true;
        foreach (var mode in new[] { "Claro", "Escuro" })
        {
            DesktopTheme.Set(mode);
            foreach (var size in new[] { new Size(1280, 800), new Size(1000, 680) })
            {
                ClientSize = size;
                foreach (var name in Pages)
                {
                    page = name; RenderPage(); PerformLayout(); Update(); await Task.Delay(35);
                    Check(content.Controls.Count >= 2, $"Tela {name}, {mode}, {size.Width}px construída");
                    Check(content.Controls.Cast<Control>().All(c => c.Width > 0 && c.Height > 0), $"Área útil de {name}, {mode}, {size.Width}px");
                    if (name is "Receitas" or "Despesas") Check(grid!.Rows.Cast<DataGridViewRow>().All(r => ((JsonNode)r.Tag!).Int("tipo") == (name == "Receitas" ? 1 : 2)), $"Filtro {name}");
                    if (size.Width == 1280 && name is "Visão geral" or "Lançamentos" or "Configurações")
                    {
                        using var image = new Bitmap(Width, Height); DrawToBitmap(image, new Rectangle(Point.Empty, Size));
                        image.Save(Path.Combine(SmokeOutput, $"winforms-{name.Replace(' ', '-')}-{mode}.png"));
                    }
                }
            }
        }
        using (var editor = new EditorDialog("Teste de formulário"))
        {
            editor.TextField("senha", "Senha", " frase com espaços ", password: true).Number("valor", "Valor", 150.25m)
                .Select("tipo", "Tipo", Kinds(), 2).Select("categoriaId", "Categoria", Categories())
                .Select("contaId", "Conta", Accounts()).DateField("data", "Data");
            editor.BindCategories("tipo", "categoriaId", state.Items("categories"));
            editor.OnSave(_ => Task.CompletedTask);
            editor.Show(this); editor.PerformLayout(); editor.Update();
            Check(editor.Values.Text("senha") == " frase com espaços ", "Senha preserva espaços");
            Check(editor.Values.Money("valor") == 150.25m, "Moeda mantém precisão decimal");
            Check(state.Items("categories").Single(c => c.Int("id") == editor.Values.Int("categoriaId")).Int("tipo") == 2, "Categoria acompanha o tipo da transação");
            using var image = new Bitmap(editor.Width, editor.Height); editor.DrawToBitmap(image, new Rectangle(Point.Empty, editor.Size)); image.Save(Path.Combine(SmokeOutput, "winforms-formulario.png")); editor.Close();
        }
        var backup = await client.Get(client.Path("backup")); var restored = await client.Post("/api/restore", backup);
        var restoredState = await client.Get($"/api/w/{restored.Int("id")}/state");
        Check(restoredState["dashboard"].Money("saldoGeral") == state["dashboard"].Money("saldoGeral"), "Backup restaura saldo em espaço separado");
        DesktopTheme.Set("Sistema"); File.WriteAllLines(Path.Combine(SmokeOutput, "checks.txt"), checks);
        Environment.ExitCode = 0; Close();
    }
}
