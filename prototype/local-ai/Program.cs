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
        var reportPath = Option(args, "--report") ?? "local-ai-report-v05.json";
        var isMock = args.Contains("--mock", StringComparer.OrdinalIgnoreCase);
        var enableThinking = args.Contains("--think", StringComparer.OrdinalIgnoreCase);
        var scenarioPath = Option(args, "--scenarios")
            ?? Path.Combine(AppContext.BaseDirectory, "scenarios-scene-plan-v05.json");

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
                var mockPlan = new ScenePlan(
                    scenario.ExpectedSpeaker,
                    scenario.ExpectedListener,
                    scenario.ExpectedPerspective,
                    scenario.RequiredFactIds,
                    new List<string>(),
                    new List<string>());

                Console.WriteLine("[MOCK PLAN]");
                Console.WriteLine(JsonSerializer.Serialize(mockPlan, Json));
                Console.WriteLine("[MOCK TEXT]");
                Console.WriteLine($"Scénář {scenario.Title} by byl vykreslen po validaci plánu.");

                results.Add(new ScenarioResult(
                    scenario.Id, scenario.Title, true, 0, 0, 0, 0,
                    mockPlan, "MOCK", null));
                continue;
            }

            var totalClock = Stopwatch.StartNew();

            var planResult = await CreateAndValidatePlan(
                client, model, enableThinking, scenario);

            if (!planResult.IsValid)
            {
                totalClock.Stop();
                Console.Error.WriteLine("PLÁN ZAMÍTNUT:");
                Console.Error.WriteLine(planResult.Error);

                results.Add(new ScenarioResult(
                    scenario.Id, scenario.Title, false,
                    planResult.ElapsedSeconds, 0,
                    planResult.InputTokens, planResult.OutputTokens,
                    planResult.Plan, "", planResult.Error));
                continue;
            }

            Console.WriteLine("[PLÁN OK]");
            Console.WriteLine(JsonSerializer.Serialize(planResult.Plan, Json));

            var renderResult = await RenderScene(
                client, model, enableThinking, scenario, planResult.Plan!);

            totalClock.Stop();

            if (renderResult.Error is not null)
            {
                Console.Error.WriteLine($"RENDER SELHAL: {renderResult.Error}");
                results.Add(new ScenarioResult(
                    scenario.Id, scenario.Title, false,
                    planResult.ElapsedSeconds, renderResult.ElapsedSeconds,
                    planResult.InputTokens + renderResult.InputTokens,
                    planResult.OutputTokens + renderResult.OutputTokens,
                    planResult.Plan, "", renderResult.Error));
                continue;
            }

            Console.WriteLine("[VÝSLEDNÝ TEXT]");
            Console.WriteLine(renderResult.Text);
            Console.WriteLine(
                $"Plán: {planResult.ElapsedSeconds:F2} s | render: {renderResult.ElapsedSeconds:F2} s | " +
                $"celkem: {totalClock.Elapsed.TotalSeconds:F2} s | " +
                $"tokeny in/out: {planResult.InputTokens + renderResult.InputTokens}/" +
                $"{planResult.OutputTokens + renderResult.OutputTokens} | " +
                $"slova: {CountWords(renderResult.Text)}");

            results.Add(new ScenarioResult(
                scenario.Id, scenario.Title, true,
                planResult.ElapsedSeconds, renderResult.ElapsedSeconds,
                planResult.InputTokens + renderResult.InputTokens,
                planResult.OutputTokens + renderResult.OutputTokens,
                planResult.Plan, renderResult.Text, null));
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

    private static async Task<PlanResult> CreateAndValidatePlan(
        HttpClient client,
        string model,
        bool enableThinking,
        Scenario scenario)
    {
        var system = """
            Jsi plánovací vrstva RPGSandbox.
            Nejsi vypravěč. Neprodukuj prózu.
            Z poskytnutého kontraktu vyber pouze povolená fakta a zachovej přesně role.
            Vrať pouze validní JSON bez markdownu a bez dalšího textu.

            Povinný tvar:
            {
              "speaker": "jméno nebo NARRATOR",
              "listener": "jméno nebo PLAYER",
              "perspective": "second_person nebo dialogue",
              "factIds": ["F1"],
              "flavorIds": ["A1"],
              "newFacts": []
            }

            Pravidla:
            - speaker, listener a perspective musí přesně odpovídat EXPECTED hodnotám.
            - factIds smějí obsahovat pouze ID z ALLOWED FACT IDS.
            - všechny REQUIRED FACT IDS musí být ve factIds.
            - flavorIds smějí obsahovat pouze ID z ALLOWED FLAVOR IDS.
            - newFacts musí být vždy prázdné pole.
            - Nevymýšlej žádné nové skutečnosti.
            """;

        var prompt = $"""
            SCÉNA: {scenario.Title}
            EXPECTED SPEAKER: {scenario.ExpectedSpeaker}
            EXPECTED LISTENER: {scenario.ExpectedListener}
            EXPECTED PERSPECTIVE: {scenario.ExpectedPerspective}

            ALLOWED FACT IDS:
            {IdBullets(scenario.Facts)}

            REQUIRED FACT IDS:
            {Bullets(scenario.RequiredFactIds)}

            ALLOWED FLAVOR IDS:
            {IdBullets(scenario.Flavor)}

            ZAKÁZANÉ SKUTEČNOSTI:
            {Bullets(scenario.ForbiddenFacts)}

            ZÁMĚR HRÁČE:
            {scenario.PlayerAction}

            OVĚŘENÝ VÝSLEDEK ENGINU:
            {scenario.EngineResult}
            """;

        var request = new
        {
            model,
            stream = false,
            think = enableThinking,
            format = "json",
            options = new { temperature = 0.0, num_predict = 180 },
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
            var content = parsed?.Message?.Content?.Trim() ?? "";
            var plan = JsonSerializer.Deserialize<ScenePlan>(content, Json);

            if (plan is null)
                return PlanResult.Invalid("Model nevrátil čitelný plán.", clock.Elapsed.TotalSeconds,
                    parsed?.PromptEvalCount ?? 0, parsed?.EvalCount ?? 0);

            var validationError = ValidatePlan(scenario, plan);
            if (validationError is not null)
                return new PlanResult(false, plan, validationError, clock.Elapsed.TotalSeconds,
                    parsed?.PromptEvalCount ?? 0, parsed?.EvalCount ?? 0);

            return new PlanResult(true, plan, null, clock.Elapsed.TotalSeconds,
                parsed?.PromptEvalCount ?? 0, parsed?.EvalCount ?? 0);
        }
        catch (Exception ex)
        {
            clock.Stop();
            return PlanResult.Invalid(ex.Message, clock.Elapsed.TotalSeconds, 0, 0);
        }
    }

    private static string? ValidatePlan(Scenario scenario, ScenePlan plan)
    {
        if (!string.Equals(plan.Speaker, scenario.ExpectedSpeaker, StringComparison.Ordinal))
            return $"speaker '{plan.Speaker}' != '{scenario.ExpectedSpeaker}'";

        if (!string.Equals(plan.Listener, scenario.ExpectedListener, StringComparison.Ordinal))
            return $"listener '{plan.Listener}' != '{scenario.ExpectedListener}'";

        if (!string.Equals(plan.Perspective, scenario.ExpectedPerspective, StringComparison.Ordinal))
            return $"perspective '{plan.Perspective}' != '{scenario.ExpectedPerspective}'";

        var allowedFacts = scenario.Facts.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var factIds = (plan.FactIds ?? new()).ToHashSet(StringComparer.Ordinal);

        if (factIds.Any(id => !allowedFacts.Contains(id)))
            return "Plán použil nepovolené factId.";

        if (scenario.RequiredFactIds.Any(id => !factIds.Contains(id)))
            return "Plánu chybí povinné factId.";

        var allowedFlavor = scenario.Flavor.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if ((plan.FlavorIds ?? new()).Any(id => !allowedFlavor.Contains(id)))
            return "Plán použil nepovolené flavorId.";

        if (plan.NewFacts is { Count: > 0 })
            return "Plán obsahuje newFacts, což je zakázané.";

        return null;
    }

    private static async Task<RenderResult> RenderScene(
        HttpClient client,
        string model,
        bool enableThinking,
        Scenario scenario,
        ScenePlan plan)
    {
        var selectedFacts = scenario.Facts
            .Where(x => plan.FactIds.Contains(x.Id))
            .ToList();

        var selectedFlavor = scenario.Flavor
            .Where(x => plan.FlavorIds.Contains(x.Id))
            .ToList();

        var system = """
            Jsi stylistický renderer českého narativního RPG RPGSandbox.
            ENGINE už rozhodl scénu a plán prošel validací.
            Nesmíš přidat žádný nový kanonický detail.

            Piš přirozenou současnou češtinou jako kvalitní české RPG.
            Vyhýbej se překladovým obratům, patosu, klišé a vysvětlování emocí, které nejsou zadány.
            Dodrž přesně speaker, listener a perspective.
            Pokud detail není v APPROVED FACTS nebo APPROVED FLAVOR, raději ho vynech.
            Nevysvětluj engine ani herní mechaniky.
            Výstup je pouze finální herní próza, bez nadpisů a bez komentáře.
            Cílová délka je 30–70 slov.
            """;

        var prompt = $"""
            SPEAKER: {plan.Speaker}
            LISTENER: {plan.Listener}
            PERSPECTIVE: {plan.Perspective}

            APPROVED FACTS:
            {TextBullets(selectedFacts)}

            APPROVED FLAVOR:
            {TextBullets(selectedFlavor)}

            FORBIDDEN:
            {Bullets(scenario.ForbiddenFacts)}

            PLAYER ACTION:
            {scenario.PlayerAction}

            ENGINE RESULT:
            {scenario.EngineResult}

            Napiš pouze výslednou scénu.
            """;

        var request = new
        {
            model,
            stream = false,
            think = enableThinking,
            options = new { temperature = 0.35, num_predict = 180 },
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

    private static string IdBullets(IEnumerable<ContractItem> items) =>
        string.Join("\n", items.Select(x => $"- {x.Id}: {x.Text}"));

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
        List<string> RequiredFactIds,
        List<ContractItem> Flavor,
        List<string> ForbiddenFacts);

    private sealed record ScenePlan(
        string Speaker,
        string Listener,
        string Perspective,
        List<string> FactIds,
        List<string> FlavorIds,
        List<string> NewFacts);

    private sealed record OllamaMessage(string? Content);

    private sealed record OllamaResponse(
        OllamaMessage? Message,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int? EvalCount);

    private sealed record PlanResult(
        bool IsValid,
        ScenePlan? Plan,
        string? Error,
        double ElapsedSeconds,
        int InputTokens,
        int OutputTokens)
    {
        public static PlanResult Invalid(string error, double elapsed, int input, int output) =>
            new(false, null, error, elapsed, input, output);
    }

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
        double PlanSeconds,
        double RenderSeconds,
        int InputTokens,
        int OutputTokens,
        ScenePlan? Plan,
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
