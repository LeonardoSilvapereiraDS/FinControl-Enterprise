using System.Text.Json.Nodes;

namespace FinControl.WinForms.Desktop;

internal sealed record Option(object Value, string Label)
{
    public override string ToString() => Label;
}

/// <summary>Formulários nativos com rolagem, navegação por teclado e salvamento assíncrono.</summary>
internal sealed class EditorDialog : Form
{
    private readonly TableLayoutPanel fields = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(24, 16, 24, 20) };
    private readonly Dictionary<string, Control> inputs = new();
    private readonly FlowLayoutPanel actions = new() { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(16) };
    public JsonObject Values
    {
        get
        {
            var result = new JsonObject();
            foreach (var (key, control) in inputs)
                result[key] = control switch
                {
                    NumericUpDown number => JsonValue.Create(number.Value),
                    DateTimePicker date => JsonValue.Create(date.Value.Date.ToString("yyyy-MM-dd")),
                    CheckBox check => JsonValue.Create(check.Checked),
                    ComboBox combo when combo.SelectedItem is Option option => System.Text.Json.JsonSerializer.SerializeToNode(option.Value),
                    TextBox password when password.UseSystemPasswordChar => JsonValue.Create(password.Text),
                    _ => JsonValue.Create(control.Text.Trim())
                };
            return result;
        }
    }
    public EditorDialog(string title, string description = "")
    {
        Text = title; Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(540, 650); MinimumSize = new Size(440, 420);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; MinimizeBox = false; MaximizeBox = false;
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(fields); Controls.Add(scroll); Controls.Add(actions);
        fields.Controls.Add(DesktopUi.Label(title, 19, true));
        if (description.Length > 0) Note(description);
        Shown += (_, _) => { DesktopTheme.Apply(this); if (inputs.Count > 0) inputs.Values.First().Focus(); };
    }
    public EditorDialog Note(string text)
    {
        var label = DesktopUi.Label(text); label.MaximumSize = new Size(440, 0); label.Tag = "muted"; fields.Controls.Add(label); return this;
    }
    private EditorDialog Add(string key, string label, Control input)
    {
        var caption = DesktopUi.Label(label, 10, true); fields.Controls.Add(caption);
        input.Dock = DockStyle.Top; input.Margin = new Padding(0, 0, 0, 9); input.AccessibleName = label;
        fields.Controls.Add(input); inputs[key] = input; return this;
    }
    public EditorDialog TextField(string key, string label, string value = "", bool password = false, bool multiline = false)
        => Add(key, label, new TextBox { Text = value, UseSystemPasswordChar = password, Multiline = multiline, Height = multiline ? 72 : 30, MaxLength = password ? 128 : 1000 });
    public EditorDialog Number(string key, string label, decimal value = 0, bool signed = false, int decimals = 2)
        => Add(key, label, new NumericUpDown { DecimalPlaces = decimals, ThousandsSeparator = true, Minimum = signed ? -999999999999.99m : 0, Maximum = 999999999999.99m, Value = value, Height = 30 });
    public EditorDialog DateField(string key, string label, DateTime? value = null)
        => Add(key, label, new DateTimePicker { Format = DateTimePickerFormat.Short, Value = value ?? DateTime.Today, Width = 220 });
    public EditorDialog Check(string key, string label, bool value = false)
        => Add(key, label, new CheckBox { Text = label, Checked = value, AutoSize = true });
    public EditorDialog Select(string key, string label, IEnumerable<Option> options, object? selected = null)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, IntegralHeight = false, MaxDropDownItems = 12 };
        foreach (var option in options) combo.Items.Add(option);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        if (selected != null) for (var i = 0; i < combo.Items.Count; i++) if (((Option)combo.Items[i]!).Value.ToString() == selected.ToString()) combo.SelectedIndex = i;
        return Add(key, label, combo);
    }
    public void BindCategories(string typeKey, string categoryKey, IEnumerable<JsonNode> categories, int current = 0)
    {
        var type = (ComboBox)inputs[typeKey]; var category = (ComboBox)inputs[categoryKey];
        void Populate()
        {
            var kind = Convert.ToInt32((type.SelectedItem as Option)?.Value ?? 1);
            category.Items.Clear();
            foreach (var item in categories.Where(c => c.Int("tipo") == kind && (c.Flag("ativa") || c.Int("id") == current)))
                category.Items.Add(new Option(item.Int("id"), item.Text("nome")));
            if (category.Items.Count > 0) category.SelectedIndex = 0;
            for (var i = 0; i < category.Items.Count; i++) if (Equals(((Option)category.Items[i]!).Value, current)) category.SelectedIndex = i;
        }
        type.SelectedIndexChanged += (_, _) => Populate(); Populate();
    }
    public void Lock(string key) => inputs[key].Enabled = false;
    public void OnSave(Func<JsonObject, Task> save, string caption = "Salvar")
    {
        var confirm = DesktopUi.Button(caption, async () =>
        {
            foreach (var combo in inputs.Values.OfType<ComboBox>()) if (combo.SelectedItem == null) throw new InvalidOperationException($"Cadastre ou selecione: {combo.AccessibleName}.");
            fields.Enabled = false;
            try { await save(Values); DialogResult = DialogResult.OK; Close(); }
            finally { if (!IsDisposed) fields.Enabled = true; }
        }, true);
        var cancel = new Button { Text = "Cancelar", AutoSize = true, MinimumSize = new Size(100, 38), DialogResult = DialogResult.Cancel };
        actions.Controls.Add(confirm); actions.Controls.Add(cancel); AcceptButton = confirm; CancelButton = cancel;
    }
}
