using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGSandbox.LocalAi;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static async Task<int> Main(string[] args)
    {
        var model = Option(args, "--model") ?? "qwen2.5:7b";
        var endpoint = Option(args, "--url") ?? "http://127.0.0.1:11434";
        var reportPath = Option(args, "--report") ?? "local-ai-report.json";
        var isMock = args.Contains("--mock", StringComparer.OrdinalIgnoreCase);
        var enableThinking = args.Contains("--think", StringComparer.OrdinalIgnoreCase);
        var scenarioPath = Option(args, "--scenarios") ?? Path.Combine(AppContext.BaseDirectory, "scenarios.json");

        if (!File.Exists(scenarioPath))
        {
            Console.Error.WriteLine($"Nenalezen soubor scénářů: {scenarioPath}");
            return 2;
        }

        List<Scenario>? scenarios;
        try
        {
            scenarios = JsonSerializer.Deserialize<List<Scenario>>(await File.ReadAllTextAsync(scenarioPath), Json);
            if (scenarios is not { Count: > 0 }) throw new InvalidDataException("Scénáře jsou prázdné.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Neplatné scénáře: {ex.Message}");
            return 2;
        }

        using var client = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(5) };
        var results = new List<ScenarioResult>();

        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"\n=== {scenario.Title} ===");
            var system = """
                Jsi vypravěč narativní hry RPGSandbox. Odpovídej přirozenou, kvalitní češtinou.
                Dostaneš pouze fakta, která smí daná postava v této scéně znát.
                Nepřidávej nová potvrzená fakta ani neměň historii, inventář, stav světa či vztahy.
                Nevysvětluj herní mechaniky, neprozrazuj čísla ani hody kostkou.
                Výsledek akce určil herní engine; respektuj ho přesně.
                Napiš krátký atmosférický dialog nebo popis (80–150 českých slov).
                """;

            var prompt = $"""
                AKTUÁLNÍ SCÉNA: {scenario.Title}
                POVOLENÝ KONTEXT:
                {scenario.Context}

                ZÁMĚR HRÁČE: {scenario.PlayerAction}
                OVĚŘENÝ VÝSLEDEK ENGINU:
                {scenario.EngineResult}
                """;

            if (isMock)
            {
                var sample = $"[MOCK] Scénář: {scenario.Title}. Skutečná odpověď AI se netestovala.";
                Console.WriteLine(sample);
                results.Add(new ScenarioResult(scenario.Id, scenario.Title, 0, null, null, sample, null));
                continue;
            }

            var request = new
            {
                model,
                stream = false,
                think = enableThinking,
                options = new { temperature = 0.6, num_predict = 280 },
                messages = new[] { new { role = "system", content = system }, new { role = "user", content = prompt } }
            };
            var clock = Stopwatch.StartNew();
            try
            {
                using var response = await client.PostAsJsonAsync("api/chat", request, Json);
                var body = await response.Content.ReadAsStringAsync();
                clock.Stop();
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");

                var parsed = JsonSerializer.Deserialize<OllamaResponse>(body, Json);
                var answer = parsed?.Message?.Content?.Trim() ?? "";
                Console.WriteLine(answer);
                Console.WriteLine($"Doba: {clock.Elapsed.TotalSeconds:F2} s | vstupní tokeny: {parsed?.PromptEvalCount} | výstupní tokeny: {parsed?.EvalCount} | počet slov: {CountWords(answer)}");
                results.Add(new ScenarioResult(scenario.Id, scenario.Title, clock.Elapsed.TotalSeconds,
                    parsed?.PromptEvalCount, parsed?.EvalCount, answer, null));
            }
            catch (Exception ex)
            {
                clock.Stop();
                Console.Error.WriteLine($"Scénář selhal: {ex.Message}");
                results.Add(new ScenarioResult(scenario.Id, scenario.Title, clock.Elapsed.TotalSeconds,
                    null, null, "", ex.Message));
            }
        }

        var report = new RunReport(DateTimeOffset.UtcNow, isMock ? "mock" : "ollama", isMock ? null : model,
            Environment.OSVersion.ToString(), Environment.ProcessorCount, results);
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, Json), Encoding.UTF8);
        Console.WriteLine($"\nReport uložen: {Path.GetFullPath(reportPath)}");
        if (results.Any(r => r.Error is not null)) return 1;
        return 0;
    }

    private static int CountWords(string answer) => answer.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        return null;
    }

    private sealed record Scenario(string Id, string Title, string Context, string PlayerAction, string EngineResult);
    private sealed record OllamaMessage(string? Content);
    private sealed record OllamaResponse(OllamaMessage? Message,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int? EvalCount);
    private sealed record ScenarioResult(string Id, string Title, double ElapsedSeconds, int? InputTokens,
        int? OutputTokens, string Output, string? Error);
    private sealed record RunReport(DateTimeOffset CreatedUtc, string Backend, string? Model, string Os,
        int LogicalProcessorCount, List<ScenarioResult> Results);
}
