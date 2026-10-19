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
        var model = Option(args, "--model") ?? "gemma3:12b";
        var endpoint = Option(args, "--url") ?? "http://127.0.0.1:11434";
        var reportPath = Option(args, "--report") ?? "local-ai-report-v06.json";
        var isMock = args.Contains("--mock", StringComparer.OrdinalIgnoreCase);
        var enableThinking = args.Contains("--think", StringComparer.OrdinalIgnoreCase);
        var scenarioPath = Option(args, "--scenarios")
            ?? Path.Combine(AppContext.BaseDirectory, "scenarios-renderer-v06.json");

        if (!File.Exists(scenarioPath))
        {
            Console.Error.WriteLine($"Nenalezen soubor scénářů: {scenarioPath}");
            return 2;
        }

        List<Scenario>? scenarios;
        try
        {
            scenarios = JsonSerializer.Deserialize<List<Scenario>>(
                await File.ReadAllTextAsync(scenarioPath), Json);
            if (scenarios is not { Count: > 0 })
                throw new InvalidDataException("Scénáře jsou prázdné.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Neplatné scénáře: {ex.Message}");
            return 2;
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri(endpoint.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(5)
        };

        var results = new List<ScenarioResult>();

        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"\n=== {scenario.Title} ===");

            if (isMock)
            {
                var mock = $"[MOCK] {scenario.ExpectedSpeaker} -> {scenario.ExpectedListener}: render bez AI planneru.";
                Console.WriteLine(mock);
                results.Add(new ScenarioResult(scenario.Id, scenario.Title, true, 0, 0, 0, mock, null));
                continue;
            }

            var render = await RenderScene(client, model, enableThinking, scenario);

            if (render.Error is not null)
            {
                Console.Error.WriteLine($"RENDER SELHAL: {render.Error}");
                results.Add(new ScenarioResult(
                    scenario.Id, scenario.Title, false, render.ElapsedSeconds,
                    render.InputTokens, render.OutputTokens, "", render.Error));
                continue;
            }

            Console.WriteLine(render.Text);
            Console.WriteLine(
                $"Doba: {render.ElapsedSeconds:F2} s | tokeny in/out: {render.InputTokens}/{render.OutputTokens} | " +
                $"počet slov: {CountWords(render.Text)}");

            results.Add(new ScenarioResult(
                scenario.Id, scenario.Title, true, render.ElapsedSeconds,
                render.InputTokens, render.OutputTokens, render.Text, null));
        }

        var report = new RunReport(
            DateTimeOffset.UtcNow,
            isMock ? "mock" : "ollama",
            isMock ? null : model,
            Environment.OSVersion.ToString(),
            Environment.ProcessorCount,
            results);

        await File.WriteAllTextAsync(
            reportPath, JsonSerializer.Serialize(report, Json), Encoding.UTF8);

        Console.WriteLine($"\nReport uložen: {Path.GetFullPath(reportPath)}");
        return results.Any(r => !r.Success) ? 1 : 0;
    }

    private static async Task<RenderResult> RenderScene(
        HttpClient client,
        string model,
        bool enableThinking,
        Scenario scenario)
    {
        var system = """
            Jsi stylistický renderer českého narativního RPG RPGSandbox.
            Herní engine už rozhodl scénu. Dostáváš hotový Engine Contract.
            Tvým jediným úkolem je převést ho do krátké, přirozené české prózy.

            Absolutní pravidla:
            - Nepřidávej žádnou osobu, místo, objekt, podnik, událost, motiv, příčinu, pověst ani vztah, které nejsou výslovně uvedené v kontraktu.
            - Nepřidávej žádný nový kanonický detail ani "logické" doplnění.
            - Povolená atmosféra je přesně vymezena v ALLOWED FLAVOR; nic dalšího nevymýšlej.
            - Dodrž přesně SPEAKER, LISTENER a PERSPECTIVE.
            - second_person vždy znamená české tykání ve 2. osobě jednotného čísla: "vidíš", "slyšíš", "stojíš", nikdy "vidíte".
            - Pokud je perspective dialogue, nesmíš prohodit, kdo mluví a kdo poslouchá.
            - Pokud něco není uvedeno, vynech to.
            - Nevysvětluj emoce ani skryté úmysly, pokud nejsou mezi fakty.
            - Nevysvětluj engine, hody, testy ani pravidla.
            - Piš přirozenou současnou češtinou jako kvalitní české RPG, ne jako doslovný překlad.
            - Vyhýbej se patosu, klišé, poetickým metaforám a komentářům vypravěče.
            - Výstup je pouze finální herní text, bez nadpisů a bez komentáře.
            - Cílová délka 20–60 slov. Kratší správný text je lepší než delší text s domyšlenými detaily.
            """;

        var prompt = $"""
            SCÉNA:
            {scenario.Title}

            SPEAKER:
            {scenario.ExpectedSpeaker}

            LISTENER:
            {scenario.ExpectedListener}

            PERSPECTIVE:
            {scenario.ExpectedPerspective}

            CANONICAL FACTS:
            {TextBullets(scenario.Facts)}

            ALLOWED FLAVOR:
            {TextBullets(scenario.Flavor)}

            FORBIDDEN:
            {Bullets(scenario.ForbiddenFacts)}

            PLAYER ACTION:
            {scenario.PlayerAction}

            ENGINE RESULT:
            {scenario.EngineResult}

            Napiš pouze výslednou scénu. Neuváděj nic mimo kontrakt.
            """;

        var request = new
        {
            model,
            stream = false,
            think = enableThinking,
            options = new { temperature = 0.18, num_predict = 140 },
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = prompt }
            }
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

            return new RenderResult(
                parsed?.Message?.Content?.Trim() ?? "",
                null,
                clock.Elapsed.TotalSeconds,
                parsed?.PromptEvalCount ?? 0,
                parsed?.EvalCount ?? 0);
        }
        catch (Exception ex)
        {
            clock.Stop();
            return new RenderResult("", ex.Message, clock.Elapsed.TotalSeconds, 0, 0);
        }
    }

    private static int CountWords(string answer) =>
        answer.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string Bullets(IEnumerable<string>? items)
    {
        var list = items?.ToList() ?? new List<string>();
        return list.Count > 0 ? string.Join("\n", list.Select(x => "- " + x)) : "- žádné";
    }

    private static string TextBullets(IEnumerable<ContractItem> items)
    {
        var list = items.ToList();
        return list.Count > 0 ? string.Join("\n", list.Select(x => "- " + x.Text)) : "- žádné";
    }

    private static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        return null;
    }

    private sealed record ContractItem(string Id, string Text);

    private sealed record Scenario(
        string Id,
        string Title,
        string Context,
        string PlayerAction,
        string EngineResult,
        string ExpectedSpeaker,
        string ExpectedListener,
        string ExpectedPerspective,
        List<ContractItem> Facts,
        List<ContractItem> Flavor,
        List<string> ForbiddenFacts);

    private sealed record OllamaMessage(string? Content);

    private sealed record OllamaResponse(
        OllamaMessage? Message,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int? EvalCount);

    private sealed record RenderResult(
        string Text,
        string? Error,
        double ElapsedSeconds,
        int InputTokens,
        int OutputTokens);

    private sealed record ScenarioResult(
        string Id,
        string Title,
        bool Success,
        double ElapsedSeconds,
        int InputTokens,
        int OutputTokens,
        string Output,
        string? Error);

    private sealed record RunReport(
        DateTimeOffset CreatedUtc,
        string Backend,
        string? Model,
        string Os,
        int LogicalProcessorCount,
        List<ScenarioResult> Results);
}
