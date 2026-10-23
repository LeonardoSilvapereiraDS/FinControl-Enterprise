using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FinControl.Web;
using FinControl.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace FinControl.WinForms.Desktop;

/// <summary>Hospeda a API no próprio processo, em uma porta local atribuída pelo Windows.</summary>
internal sealed class FinanceClient : IAsyncDisposable
{
    private WebApplication? host;
    private HttpClient? http;
    public int WorkspaceId { get; set; }
    public string SetupCode => host!.Services.GetRequiredService<InitialSetup>().Code;
    public async Task StartAsync()
    {
        host = FinControlHost.Create(["--urls", "http://127.0.0.1:0"], desktop: true);
        await host.StartAsync();
        var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        http = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), UseProxy = false })
        { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(90) };
        http.DefaultRequestHeaders.Add("X-FinControl", "1");
    }
    public string Path(string suffix) => $"/api/w/{WorkspaceId}/{suffix}";
    public Task<JsonNode> Get(string path) => Send(HttpMethod.Get, path);
    public Task<JsonNode> Post(string path, object? value = null) => Send(HttpMethod.Post, path, value);
    public Task<JsonNode> Put(string path, object value) => Send(HttpMethod.Put, path, value);
    public Task<JsonNode> Patch(string path, object value) => Send(HttpMethod.Patch, path, value);
    public Task<JsonNode> Delete(string path) => Send(HttpMethod.Delete, path);
    public async Task<JsonNode> Send(HttpMethod method, string path, object? value = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (value != null) request.Content = JsonContent.Create(value);
        using var response = await http!.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        JsonNode? node = null;
        if (!string.IsNullOrWhiteSpace(text)) node = JsonNode.Parse(text);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(node?["message"]?.GetValue<string>() ?? (response.StatusCode == HttpStatusCode.Unauthorized
                ? "Sua sessão expirou. Clique em Sair e entre novamente."
                : response.StatusCode == HttpStatusCode.TooManyRequests ? "Muitas tentativas. Aguarde um minuto e tente novamente."
                : $"Não foi possível concluir a operação ({(int)response.StatusCode})."));
        return node ?? new JsonObject();
    }
    public async ValueTask DisposeAsync()
    {
        http?.Dispose();
        if (host != null) { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8)); await host.StopAsync(timeout.Token); await host.DisposeAsync(); }
    }
}

internal static class JsonValues
{
    public static string Text(this JsonNode? node, string key, string fallback = "") => node?[key]?.ToString() ?? fallback;
    public static int Int(this JsonNode? node, string key) => node?[key]?.GetValue<int>() ?? 0;
    public static decimal Money(this JsonNode? node, string key) => node?[key]?.GetValue<decimal>() ?? 0;
    public static bool Flag(this JsonNode? node, string key) => node?[key]?.GetValue<bool>() ?? false;
    public static DateTime Date(this JsonNode? node, string key) => DateTime.TryParse(node.Text(key), out var date) ? date : DateTime.Today;
    public static IEnumerable<JsonNode> Items(this JsonNode? node, string key) => node?[key]?.AsArray().OfType<JsonNode>() ?? [];
}
