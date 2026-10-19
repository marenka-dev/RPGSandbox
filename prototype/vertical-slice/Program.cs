using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGSandbox.VerticalSlice;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rendererModel = Option(args, "--model") ?? "gemma3:12b";
        var intentModel = Option(args, "--intent-model") ?? "qwen3:8b";
        var ollamaUrl = Option(args, "--url") ?? "http://127.0.0.1:11434";
        var savePath = Option(args, "--save") ?? "rabenfeld-v01.db";
        var noAi = args.Contains("--no-ai", StringComparer.OrdinalIgnoreCase);
        var noIntentAi = noAi || args.Contains("--no-intent-ai", StringComparer.OrdinalIgnoreCase);

        await using var store = new GameStore(savePath);
        await store.InitializeAsync();
        var state = await store.LoadOrCreateAsync();

        using var renderer = new OllamaRenderer(ollamaUrl, rendererModel, noAi);
        using var interpreter = new OllamaIntentInterpreter(ollamaUrl, intentModel, noIntentAi);
        var engine = new GameEngine(store, renderer, interpreter);

        PrintHeader(rendererModel, intentModel, savePath, noAi, noIntentAi);
        await engine.RenderArrivalAsync(state);

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("konec", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                await store.SaveAsync(state);
                Console.WriteLine("Hra uložena. Na shledanou.");
                return;
            }

            if (input.Equals("pomoc", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            if (input.Equals("stav", StringComparison.OrdinalIgnoreCase))
            {
                PrintState(state);
                continue;
            }

            if (input.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                state = WorldState.NewGame();
                await store.ResetAsync(state);
                engine.ResetConversation();
                Console.WriteLine("Svět vertical slice byl resetován.");
                await engine.RenderArrivalAsync(state);
                continue;
            }

            await engine.HandleAsync(state, input);
        }
    }

    private static void PrintHeader(
        string rendererModel,
        string intentModel,
        string savePath,
        bool noAi,
        bool noIntentAi)
    {
        Console.WriteLine("RPGSandbox – Rabenfeld vertical slice v0.2 Natural Input");
        Console.WriteLine($"Vypravěč: {(noAi ? "fallback bez AI" : rendererModel)}");
        Console.WriteLine($"Interpret vstupu: {(noIntentAi ? "jen deterministický parser" : intentModel)}");
        Console.WriteLine($"Save: {Path.GetFullPath(savePath)}");
        Console.WriteLine("Piš přirozeně česky. Engine nejdřív zkusí bezpečný parser a teprve potom AI interpretaci.");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Můžeš psát přirozeně, například:
              kde jsem?
              koho znám?
              kolik mám peněz?
              kolik je hodin?
              podívám se po pokoji
              podívám se z okna
              sejdu dolů
              jak se máš?
              máš něco k jídlu?
              poprosím o snídani
              je dnes ve městě rušno?
              půjdu na náměstí
              pozdravím Havla
              prohlédnu si Havla
              zkusím ho přesvědčit, aby mi prodal obilí za polovinu
              vrátím se do hostince

            Systém:
              stav
              reset
              konec
            """);
    }

    private static void PrintState(WorldState state)
    {
        Console.WriteLine($"Čas: {state.GameTime:dd.MM.yyyy HH:mm}");
        Console.WriteLine($"Lokace: {LocationNames.Name(state.Location)}");
        Console.WriteLine($"Peníze: {state.MoneyGroats} grošů");
        Console.WriteLine($"Matěj zná plán trh → radnice: {(state.MatejKnowsPlan ? "ano" : "ne")}");
        Console.WriteLine($"Cena obilí u Havla: {state.GrainPriceGroats} groše/míra");
    }

    private static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}

internal sealed class GameEngine
{
    private readonly GameStore _store;
    private readonly INarrativeRenderer _renderer;
    private readonly IIntentInterpreter _interpreter;
    private string? _conversationTarget;

    public GameEngine(GameStore store, INarrativeRenderer renderer, IIntentInterpreter interpreter)
    {
        _store = store;
        _renderer = renderer;
        _interpreter = interpreter;
    }

    public void ResetConversation() => _conversationTarget = null;

    public async Task RenderArrivalAsync(WorldState state)
    {
        var contract = state.Location switch
        {
            LocationId.InnRoom => SceneContracts.InnRoomArrival(state),
            LocationId.InnCommonRoom => SceneContracts.InnCommonArrival(state),
            LocationId.MarketSquare => SceneContracts.MarketArrival(state),
            _ => SceneContracts.InnRoomArrival(state)
        };

        await RenderAsync(contract);
    }

    public async Task HandleAsync(WorldState state, string rawInput)
    {
        if (TryHandleDirectQuestion(state, rawInput, out var directAnswer))
        {
            Console.WriteLine(directAnswer);
            return;
        }

        var parsed = IntentParser.Parse(rawInput, state.Location, _conversationTarget);

        if (parsed.Intent == PlayerIntent.Unknown)
        {
            var aiParsed = await _interpreter.InterpretAsync(
                rawInput,
                state,
                _conversationTarget,
                AvailableTargets(state.Location));

            if (aiParsed is not null)
                parsed = aiParsed;
        }

        parsed = NormalizeImplicitTarget(parsed, state.Location, _conversationTarget);

        var resolution = Resolve(state, parsed, rawInput);

        if (!resolution.Accepted)
        {
            Console.WriteLine(resolution.SystemMessage);
            return;
        }

        state.GameTime = state.GameTime.AddMinutes(resolution.Minutes);

        if (resolution.NewLocation is not null)
        {
            state.Location = resolution.NewLocation.Value;
            _conversationTarget = null;
        }

        if (parsed.Intent == PlayerIntent.GoodbyeMatej)
            _conversationTarget = null;

        if (resolution.SetMatejKnowsPlan)
            state.MatejKnowsPlan = true;

        if (resolution.ConversationTarget is not null)
            _conversationTarget = resolution.ConversationTarget;

        await _store.AppendEventAsync(
            state.GameTime,
            parsed.Intent.ToString(),
            rawInput,
            resolution.EventSummary);

        await _store.SaveAsync(state);

        if (resolution.Contract is not null)
            await RenderAsync(resolution.Contract);
    }

    private static bool TryHandleDirectQuestion(WorldState state, string rawInput, out string answer)
    {
        var n = TextNorm.Normalize(rawInput);

        if (TextNorm.ContainsAny(n, "kde jsem", "kde se nachazim", "kde ted jsem"))
        {
            answer = $"Jsi v lokaci: {LocationNames.Name(state.Location)}.";
            return true;
        }

        if (TextNorm.ContainsAny(n, "kolik mam penez", "kolik mam gros", "penize mam", "stav penez"))
        {
            answer = $"Máš {state.MoneyGroats} grošů.";
            return true;
        }

        if (TextNorm.ContainsAny(n, "kolik je hodin", "kolik je", "jaky je cas", "cas je"))
        {
            answer = $"Je {state.GameTime:HH:mm}, {state.GameTime:dd.MM.yyyy}.";
            return true;
        }

        if (TextNorm.ContainsAny(n, "koho znam", "jake lidi znam", "kdo je znamy"))
        {
            answer = state.Location == LocationId.MarketSquare
                ? "Z relevantních lidí znáš Matěje, hostinského U Tří korun. Na trhu právě vidíš kupce Havla."
                : "Z relevantních lidí znáš Matěje, hostinského U Tří korun.";
            return true;
        }

        answer = "";
        return false;
    }

    private Resolution Resolve(WorldState state, ParsedIntent parsed, string rawInput)
    {
        var intent = parsed.Intent;
        var target = parsed.Target;

        return intent switch
        {
            PlayerIntent.LookRoom when state.Location == LocationId.InnRoom =>
                Resolution.Ok(2, null, SceneContracts.LookRoom(state),
                    "Pavel si prohlédl pokoj."),

            PlayerIntent.LookWindow when state.Location == LocationId.InnRoom =>
                Resolution.Ok(3, null, SceneContracts.LookFromWindow(state),
                    "Pavel se díval z okna na Rabenfeld."),

            PlayerIntent.LookAround => ResolveLook(state),

            PlayerIntent.InspectMatej when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.InspectMatej(state),
                    "Pavel si prohlédl Matěje.", conversationTarget: "Matěj"),

            PlayerIntent.InspectHavel when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(1, null, SceneContracts.InspectHavel(state),
                    "Pavel si prohlédl Havla.", conversationTarget: "Havel"),

            PlayerIntent.GoDownstairs when state.Location == LocationId.InnRoom =>
                Resolution.Ok(2, LocationId.InnCommonRoom, SceneContracts.GoDownstairs(state),
                    "Pavel sešel z pokoje do společenské místnosti hostince."),

            PlayerIntent.GoSquare when state.Location is LocationId.InnRoom or LocationId.InnCommonRoom =>
                Resolution.Ok(5, LocationId.MarketSquare, SceneContracts.GoSquare(state),
                    "Pavel odešel z hostince na rabenfeldské náměstí."),

            PlayerIntent.GoInn when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(5, LocationId.InnCommonRoom, SceneContracts.GoInn(state),
                    "Pavel se vrátil z náměstí do hostince U Tří korun."),

            PlayerIntent.OrderBreakfast when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(4, null, SceneContracts.OrderBreakfast(state),
                    "Matěj přinesl Pavlovi snídani.", conversationTarget: "Matěj"),

            PlayerIntent.AskBusy when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.AskBusy(state),
                    "Matěj řekl Pavlovi, že je tržní den a náměstí bývá rušnější.",
                    conversationTarget: "Matěj"),

            PlayerIntent.BreakfastBusy when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(5, null, SceneContracts.BreakfastBusy(state),
                    "Matěj přinesl Pavlovi snídani a řekl, že je tržní den a náměstí bývá rušnější.",
                    conversationTarget: "Matěj"),

            PlayerIntent.AskHowAreYou when state.Location == LocationId.InnCommonRoom && target == "Matěj" =>
                Resolution.Ok(1, null, SceneContracts.AskHowMatej(state),
                    "Pavel se Matěje zeptal, jak se má.", conversationTarget: "Matěj"),

            PlayerIntent.AskFood when state.Location == LocationId.InnCommonRoom && target == "Matěj" =>
                Resolution.Ok(1, null, SceneContracts.AskFood(state),
                    "Pavel se Matěje zeptal, zda má něco k jídlu.", conversationTarget: "Matěj"),

            PlayerIntent.AskSecret when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(2, null, SceneContracts.AskSecret(state),
                    "Matěj nepotvrdil údajnou tajnou dohodu; nic spolehlivého o ní neví.",
                    conversationTarget: "Matěj"),

            PlayerIntent.TellPlan when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(2, null, SceneContracts.TellPlan(state),
                    "Pavel řekl Matějovi, že chce jít na trh a potom na radnici.",
                    true, "Matěj"),

            PlayerIntent.GoodbyeMatej when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.GoodbyeMatej(state),
                    "Pavel se rozloučil s Matějem.", conversationTarget: null),

            PlayerIntent.GreetMatej when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.GreetMatej(state),
                    "Pavel pozdravil Matěje.", conversationTarget: "Matěj"),

            PlayerIntent.GreetHavel when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(1, null, SceneContracts.GreetHavel(state),
                    "Pavel pozdravil Havla.", conversationTarget: "Havel"),

            PlayerIntent.BargainHalf when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(3, null, SceneContracts.BargainFailure(state),
                    "Pavel neuspěl při pokusu koupit obilí od Havla za polovinu ceny.",
                    conversationTarget: "Havel"),

            PlayerIntent.TalkMatej when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.TalkMatej(state),
                    "Pavel oslovil Matěje.", conversationTarget: "Matěj"),

            PlayerIntent.TalkHavel when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(1, null, SceneContracts.TalkHavel(state),
                    "Pavel oslovil kupce Havla.", conversationTarget: "Havel"),

            _ => Resolution.Reject(SuggestionFor(state.Location, rawInput))
        };
    }

    private static ParsedIntent NormalizeImplicitTarget(
        ParsedIntent parsed,
        LocationId location,
        string? conversationTarget)
    {
        if (!string.IsNullOrWhiteSpace(parsed.Target))
            return parsed;

        var target = conversationTarget;

        if (target is null)
        {
            if (location == LocationId.InnCommonRoom)
                target = "Matěj";
            else if (location == LocationId.MarketSquare)
                target = "Havel";
        }

        return parsed with { Target = target };
    }

    private static List<string> AvailableTargets(LocationId location) => location switch
    {
        LocationId.InnCommonRoom => ["Matěj"],
        LocationId.MarketSquare => ["Havel"],
        _ => []
    };

    private static Resolution ResolveLook(WorldState state)
    {
        var contract = state.Location switch
        {
            LocationId.InnRoom => SceneContracts.LookRoom(state),
            LocationId.InnCommonRoom => SceneContracts.LookInn(state),
            LocationId.MarketSquare => SceneContracts.LookMarket(state),
            _ => SceneContracts.LookRoom(state)
        };

        return Resolution.Ok(2, null, contract, $"Pavel se rozhlédl v lokaci {state.Location}.");
    }

    private static string SuggestionFor(LocationId location, string rawInput)
    {
        var where = LocationNames.Name(location);
        return $"Akci „{rawInput}“ se zatím nepodařilo bezpečně převést na povolenou herní akci v lokaci {where}. " +
               "Svět se nezměnil.";
    }

    private async Task RenderAsync(SceneContract contract)
    {
        var sw = Stopwatch.StartNew();
        var text = await _renderer.RenderAsync(contract);
        sw.Stop();
        Console.WriteLine();
        Console.WriteLine(text);
        Console.WriteLine($"[{sw.Elapsed.TotalSeconds:F1} s]");
    }
}

internal static class IntentParser
{
    public static ParsedIntent Parse(string input, LocationId location, string? conversationTarget)
    {
        var n = TextNorm.Normalize(input);

        if (location == LocationId.InnRoom &&
            TextNorm.ContainsAny(n, "po pokoji", "pokoj", "mistnost") &&
            TextNorm.ContainsAny(n, "podiv", "prohl", "rozhl"))
            return new(PlayerIntent.LookRoom, null, "deterministic");

        if (location == LocationId.InnRoom &&
            TextNorm.ContainsAny(n, "z okna", "oknem", "ven z okna"))
            return new(PlayerIntent.LookWindow, null, "deterministic");

        if (location == LocationId.MarketSquare &&
            TextNorm.ContainsAny(n, "prohl", "podiv") &&
            TextNorm.ContainsAny(n, "havel", "havla", "ho"))
            return new(PlayerIntent.InspectHavel, "Havel", "deterministic");

        if (location == LocationId.InnCommonRoom &&
            TextNorm.ContainsAny(n, "prohl", "podiv") &&
            TextNorm.ContainsAny(n, "matej", "mateje", "ho"))
            return new(PlayerIntent.InspectMatej, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "tajnou dohod", "tajne dohod", "dohod rady", "dohodu rady"))
            return new(PlayerIntent.AskSecret, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "snidan") && TextNorm.ContainsAny(n, "rus", "trzn", "mesto"))
            return new(PlayerIntent.BreakfastBusy, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "snidan") &&
            TextNorm.ContainsAny(n, "popros", "dam si", "chci", "objedn", "prines"))
            return new(PlayerIntent.OrderBreakfast, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "mas neco k jidlu", "mate neco k jidlu", "co mas k jidlu", "jidlo"))
            return new(PlayerIntent.AskFood, conversationTarget, "deterministic");

        if (TextNorm.ContainsAny(n, "jak se mas", "jak se vam dari", "jak se dari"))
            return new(PlayerIntent.AskHowAreYou, conversationTarget, "deterministic");

        if (TextNorm.ContainsAny(n, "je dnes", "dnes je") &&
            TextNorm.ContainsAny(n, "rus", "trzn", "namesti"))
            return new(PlayerIntent.AskBusy, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "reknu matej", "povim matej", "planuju") &&
            TextNorm.ContainsAny(n, "trh") && TextNorm.ContainsAny(n, "radnic"))
            return new(PlayerIntent.TellPlan, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "rozlouc", "sbohem", "mej se") && location == LocationId.InnCommonRoom)
            return new(PlayerIntent.GoodbyeMatej, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "pozdrav", "ahoj", "dobry den") &&
            TextNorm.ContainsAny(n, "havel", "havla") && location == LocationId.MarketSquare)
            return new(PlayerIntent.GreetHavel, "Havel", "deterministic");

        if (TextNorm.ContainsAny(n, "pozdrav", "ahoj", "dobry den") &&
            TextNorm.ContainsAny(n, "matej", "mateje") && location == LocationId.InnCommonRoom)
            return new(PlayerIntent.GreetMatej, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "polovin", "pulku", "slev") &&
            TextNorm.ContainsAny(n, "obil", "havel", "ho") &&
            location == LocationId.MarketSquare)
            return new(PlayerIntent.BargainHalf, "Havel", "deterministic");

        if (TextNorm.ContainsAny(n, "namesti", "trh", "ven") &&
            TextNorm.ContainsAny(n, "jdu", "pujdu", "vyjdu", "odejdu"))
            return new(PlayerIntent.GoSquare, null, "deterministic");

        if (TextNorm.ContainsAny(n, "hostinc", "tri korun", "zpet") &&
            TextNorm.ContainsAny(n, "jdu", "vratim", "pujdu"))
            return new(PlayerIntent.GoInn, null, "deterministic");

        if (TextNorm.ContainsAny(n, "dolu", "matej") &&
            TextNorm.ContainsAny(n, "sejdu", "jdu", "pujdu") &&
            location == LocationId.InnRoom)
            return new(PlayerIntent.GoDownstairs, null, "deterministic");

        if (TextNorm.ContainsAny(n, "matej") &&
            TextNorm.ContainsAny(n, "mluv", "oslov", "zept", "promluv"))
            return new(PlayerIntent.TalkMatej, "Matěj", "deterministic");

        if (TextNorm.ContainsAny(n, "havel") &&
            TextNorm.ContainsAny(n, "mluv", "oslov", "zept", "promluv"))
            return new(PlayerIntent.TalkHavel, "Havel", "deterministic");

        if (TextNorm.ContainsAny(n, "rozhl", "pozoru", "prohlid", "koukam"))
            return new(PlayerIntent.LookAround, null, "deterministic");

        return new(PlayerIntent.Unknown, null, "none");
    }
}

internal sealed class OllamaIntentInterpreter : IIntentInterpreter, IDisposable
{
    private readonly HttpClient _client;
    private readonly string _model;
    private readonly bool _disabled;

    public OllamaIntentInterpreter(string baseUrl, string model, bool disabled)
    {
        _model = model;
        _disabled = disabled;
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(45)
        };
    }

    public async Task<ParsedIntent?> InterpretAsync(
        string input,
        WorldState state,
        string? conversationTarget,
        List<string> availableTargets)
    {
        if (_disabled)
            return null;

        var allowed = new[]
        {
            "LookAround", "LookRoom", "LookWindow", "InspectMatej", "InspectHavel",
            "GoDownstairs", "GoSquare", "GoInn", "TalkMatej", "TalkHavel",
            "GreetMatej", "GreetHavel", "AskHowAreYou", "AskFood", "OrderBreakfast",
            "AskBusy", "BreakfastBusy", "AskSecret", "TellPlan", "GoodbyeMatej",
            "BargainHalf", "Unknown"
        };

        var system = """
            Jsi pouze interpret hráčova českého vstupu pro RPGSandbox.
            Nerozhoduješ výsledek akce a nesmíš měnit svět.
            Vrať POUZE validní JSON bez markdownu:
            {"intent":"NazevIntentu","target":"Matěj|Havel|null"}

            Použij pouze intent ze seznamu ALLOWED INTENTS.
            Target může být pouze osoba z AVAILABLE TARGETS nebo null.
            Zájmena jako "ho", "mu", "tebe" řeš podle CURRENT CONVERSATION, pokud existuje.
            Pokud si nejsi jistý nebo akce neodpovídá povolenému intentu, vrať Unknown.
            """;

        var user = $"""
            LOCATION: {LocationNames.Name(state.Location)}
            CURRENT CONVERSATION: {conversationTarget ?? "null"}
            AVAILABLE TARGETS: {(availableTargets.Count == 0 ? "none" : string.Join(", ", availableTargets))}
            ALLOWED INTENTS: {string.Join(", ", allowed)}
            PLAYER INPUT: {input}
            """;

        var request = new
        {
            model = _model,
            stream = false,
            think = false,
            format = "json",
            options = new { temperature = 0.0, num_predict = 80 },
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        try
        {
            using var response = await _client.PostAsJsonAsync("api/chat", request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return null;

            var parsed = JsonSerializer.Deserialize<OllamaResponse>(
                body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var content = parsed?.Message?.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var proposal = JsonSerializer.Deserialize<IntentProposal>(
                content, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (proposal is null ||
                !Enum.TryParse<PlayerIntent>(proposal.Intent, true, out var intent))
                return null;

            if (proposal.Target is not null &&
                !availableTargets.Contains(proposal.Target, StringComparer.Ordinal))
                return null;

            return new ParsedIntent(intent, proposal.Target, "qwen");
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();

    private sealed record OllamaMessage(string? Content);
    private sealed record OllamaResponse(OllamaMessage? Message);
    private sealed record IntentProposal(string Intent, string? Target);
}

internal static class SceneContracts
{
    public static SceneContract InnRoomArrival(WorldState s) => new(
        "Pokoj v hostinci U Tří korun", "NARRATOR", "PLAYER", "second_person",
        [
            "Je časné ráno.",
            "Pavel je v pokoji hostince U Tří korun v Rabenfeldu.",
            "Z okna je vidět náměstí.",
            "Na náměstí začíná běžný trh."
        ],
        ["Může být zmíněno ranní světlo a neurčitý ruch z náměstí."],
        ["Nové osoby.", "Konkrétní zboží.", "Mimořádné události."],
        "Uveď hráče do scény.");

    public static SceneContract InnCommonArrival(WorldState s) => new(
        "Společenská místnost U Tří korun", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel je ve společenské místnosti hostince U Tří korun.",
            "Hostinský Matěj je přítomen.",
            "Matěj je kolem padesáti let a Pavla zná z několika návštěv."
        ],
        ["Může být zmíněn běžný tlumený ruch hostince."],
        ["Další konkrétní hosté.", "Konkrétní jídla.", "Pověsti nebo zprávy."],
        "Stručně popiš lokaci.");

    public static SceneContract MarketArrival(WorldState s) => new(
        "Rabenfeldské náměstí", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel je na náměstí v Rabenfeldu.",
            "Probíhá běžný tržní den.",
            "Na náměstí jsou stánky, povozy a měšťané.",
            "Kupec Havel zde prodává obilí za 4 groše za míru."
        ],
        ["Může zaznít neurčitý tržní ruch."],
        ["Další pojmenované osoby.", "Konkrétní jiné zboží.", "Konflikty nebo zprávy."],
        "Stručně popiš příchod na náměstí.");

    public static SceneContract LookRoom(WorldState s) => new(
        "Prohlídka pokoje", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel je v jednoduchém pokoji hostince U Tří korun.",
            "V pokoji je postel, stůl, židle a okno.",
            "Okno vede na rabenfeldské náměstí."
        ],
        ["Může být zmíněno ranní světlo v pokoji."],
        ["Pohled na dění na trhu.", "Další nábytek.", "Předměty s herní hodnotou.", "Nové osoby."],
        "Pavel si krátce prohlédne pokoj.");

    public static SceneContract LookFromWindow(WorldState s) => new(
        "Pozorování Rabenfeldu z okna", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel stojí u okna pokoje U Tří korun.",
            "Je časné ráno.",
            "Na náměstí začíná běžný trh.",
            "Pavel vidí stánky, několik povozů a několik měšťanů.",
            "Neprobíhá nic mimořádného."
        ],
        ["Může být zmíněno ranní světlo a neurčitý ruch trhu."],
        ["Konkrétní zboží.", "Nové osoby.", "Počasí kromě světelných podmínek.", "Události."],
        "Pavel tři minuty pozoruje město.");

    public static SceneContract LookInn(WorldState s) => new(
        "Rozhlédnutí v hostinci", "NARRATOR", "PLAYER", "second_person",
        ["Pavel je ve společenské místnosti U Tří korun.", "Matěj je přítomen."],
        ["Může být zmíněn běžný tlumený ruch místnosti."],
        ["Další konkrétní lidé.", "Konkrétní jídlo nebo pití.", "Nové informace."],
        "Pavel se krátce rozhlédne.");

    public static SceneContract LookMarket(WorldState s) => new(
        "Rozhlédnutí na trhu", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel stojí na rabenfeldském náměstí.",
            "Probíhá běžný tržní den.",
            "Pavel vidí stánky, povozy a měšťany.",
            "Havel prodává obilí za 4 groše za míru."
        ],
        ["Může být zmíněn neurčitý hluk trhu."],
        ["Konkrétní jiné zboží.", "Další pojmenované osoby.", "Události nebo pověsti."],
        "Pavel se dvě minuty rozhlíží.");

    public static SceneContract InspectMatej(WorldState s) => new(
        "Prohlídka Matěje", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel si prohlíží Matěje.",
            "Matěj je hostinský kolem padesáti let.",
            "Pavel ho zná z několika návštěv."
        ],
        ["Matěj může působit jako člověk zvyklý na práci v hostinci."],
        ["Konkrétní minulost.", "Skryté vlastnosti.", "Tajemství.", "Nové předměty."],
        "Pavel nezíská žádnou novou skrytou informaci.");

    public static SceneContract InspectHavel(WorldState s) => new(
        "Prohlídka Havla", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel si prohlíží Havla.",
            "Havel je kupec.",
            "Havel prodává obilí za 4 groše za míru.",
            "Havel Pavla nezná."
        ],
        ["Havel může působit věcně a soustředěně na obchod."],
        ["Konkrétní věk.", "Rodina.", "Majetek.", "Skryté vlastnosti.", "Další osoby."],
        "Pavel nezíská žádnou novou skrytou informaci.");

    public static SceneContract GoDownstairs(WorldState s) => new(
        "Cesta dolů za Matějem", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel opustí pokoj a sejde do společenské místnosti hostince U Tří korun.",
            "Hostinský Matěj je ve společenské místnosti.",
            "Matěj Pavla zná z několika návštěv."
        ],
        ["Přechod může být popsán jednou krátkou větou."],
        ["Další osoby.", "Nové události.", "Konkrétní jídlo nebo pití."],
        "Pavel dorazí do společenské místnosti.");

    public static SceneContract GoSquare(WorldState s) => new(
        "Cesta na náměstí", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel odejde z hostince U Tří korun na rabenfeldské náměstí.",
            "Na náměstí probíhá běžný tržní den.",
            "Havel zde prodává obilí za 4 groše za míru."
        ],
        ["Může být zmíněn neurčitý tržní ruch."],
        ["Další pojmenované osoby.", "Konkrétní jiné zboží.", "Události."],
        "Přesun trvá pět minut.");

    public static SceneContract GoInn(WorldState s) => new(
        "Návrat do hostince", "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel odejde z náměstí a vrátí se do společenské místnosti hostince U Tří korun.",
            "Matěj je v hostinci."
        ],
        ["Přesun popiš stručně."],
        ["Další konkrétní osoby.", "Nové události."],
        "Přesun trvá pět minut.");

    public static SceneContract GreetMatej(WorldState s) => new(
        "Pozdrav Matěje", "Matěj", "Pavel", "dialogue",
        ["Pavel pozdraví Matěje.", "Matěj Pavla zná z několika návštěv.", "Matěj je přátelský a věcný."],
        ["Matěj může Pavla krátce pozdravit zpět."],
        ["Nové zprávy.", "Konkrétní jídlo.", "Pověsti."],
        "Matěj pozdrav opětuje.");

    public static SceneContract GreetHavel(WorldState s) => new(
        "Pozdrav Havla", "Havel", "Pavel", "dialogue",
        ["Pavel pozdraví Havla.", "Havel Pavla nezná.", "Havel je věcný kupec."],
        ["Havel může krátce pozdravit zpět."],
        ["Další osoby.", "Nová nabídka.", "Pověsti."],
        "Havel pozdrav opětuje bez změny vztahu.");

    public static SceneContract AskHowMatej(WorldState s) => new(
        "Jak se má Matěj", "Matěj", "Pavel", "dialogue",
        [
            "Pavel se Matěje ptá, jak se má.",
            "Matěj je v pořádku.",
            "Matěj je právě v práci v hostinci.",
            "Matěj nemá důvod sdělovat žádnou mimořádnou událost."
        ],
        ["Matěj může odpovědět krátce a civilně."],
        ["Zdravotní problém.", "Rodinný problém.", "Nová událost.", "Pověst."],
        "Matěj odpoví běžnou společenskou odpovědí.");

    public static SceneContract AskFood(WorldState s) => new(
        "Dotaz na jídlo", "Matěj", "Pavel", "dialogue",
        [
            "Pavel se Matěje ptá, zda má něco k jídlu.",
            "Matěj jako hostinský může nabídnout běžnou snídani.",
            "Konkrétní složení snídaně není definováno."
        ],
        ["Matěj může stručně potvrdit, že snídani připraví nebo přinese."],
        ["Konkrétní pokrm.", "Konkrétní cena.", "Další hosté."],
        "Matěj potvrdí, že snídani může nabídnout.");

    public static SceneContract OrderBreakfast(WorldState s) => new(
        "Objednání snídaně", "Matěj", "Pavel", "dialogue",
        [
            "Pavel požádá Matěje o snídani.",
            "Matěj Pavlovi přinese snídani.",
            "Konkrétní podoba snídaně není definována."
        ],
        ["Matěj může snídani položit před hráče a krátce reagovat."],
        ["Konkrétní jídlo.", "Konkrétní cena.", "Další hosté."],
        "Pavel dostane snídani.");

    public static SceneContract AskBusy(WorldState s) => new(
        "Dotaz na dnešní ruch", "Matěj", "Pavel", "dialogue",
        [
            "Pavel se Matěje ptá, zda je dnes ve městě rušno.",
            "Dnes je tržní den.",
            "Náměstí bývá během tržního dne rušnější.",
            "Matěj nemá žádnou další relevantní zprávu."
        ],
        ["Matěj může odpovědět krátce a věcně."],
        ["Náměstí je plné.", "Původ obchodníků.", "Další zprávy.", "Pověsti."],
        "Matěj odpoví pouze k tržnímu dni.");

    public static SceneContract BreakfastBusy(WorldState s) => new(
        "Snídaně s Matějem", "Matěj", "Pavel", "dialogue",
        [
            "Matěj přinese Pavlovi snídani.",
            "Konkrétní podoba snídaně není definována.",
            "Pavel se zeptá, zda je dnes ve městě rušno.",
            "Matěj odpoví, že je tržní den a náměstí bývá rušnější.",
            "Matěj nemá žádnou další relevantní zprávu."
        ],
        ["Matěj může položit snídani na stůl nebo udělat jedno krátké gesto hostinského."],
        ["Konkrétní jídlo nebo nápoj.", "Další hosté.", "Pověsti.", "Příchod Pavla do hostince."],
        "Rozhovor trvá pět minut.");

    public static SceneContract AskSecret(WorldState s) => new(
        "Otázka na údajnou tajnou dohodu", "Matěj", "Pavel", "dialogue",
        [
            "Jediným zdrojem informace o údajné tajné dohodě je Pavlova otázka.",
            "Matěj o žádné takové dohodě nic spolehlivého neví.",
            "Matěj neví, zda taková dohoda existuje.",
            "Matěj její existenci nepotvrdí."
        ],
        ["Matěj může odpovědět stručně a opatrně."],
        ["Matěj o dohodě také slyšel.", "Po městě kolují drby.", "Matěj něco tají.", "Rada je podezřelá."],
        "Matěj odpoví bez nové informace.");

    public static SceneContract TellPlan(WorldState s) => new(
        "Pavel sděluje svůj plán", "Pavel", "Matěj", "dialogue",
        [
            "Pavel řekne Matějovi, že dnes chce jít nejprve na trh a potom na radnici.",
            "Matěj si tuto informaci zapamatuje."
        ],
        ["Matěj může krátce potvrdit, že rozumí."],
        ["Důvod návštěvy trhu.", "Důvod návštěvy radnice.", "Další plán."],
        "Informace se stává součástí Matějovy paměti.");

    public static SceneContract GoodbyeMatej(WorldState s)
    {
        var facts = new List<string>
        {
            "Pavel se při odchodu loučí s Matějem.",
            "Matěj odpovídá Pavlovi."
        };

        if (s.MatejKnowsPlan)
        {
            facts.Add("Matěj si pamatuje, že Pavel dnes plánuje nejprve trh a potom radnici.");
            facts.Add("Matěj nezná důvod ani jedné návštěvy.");
        }

        return new SceneContract(
            "Rozloučení s Matějem", "Matěj", "Pavel", "dialogue", facts,
            ["Matěj může Pavlovi popřát dobrý den nebo štěstí."],
            ["Matěj jde na trh nebo radnici.", "Důvod Pavlovy návštěvy.", "Další úkol nebo schůzka."],
            "Matěj se krátce rozloučí.");
    }

    public static SceneContract TalkMatej(WorldState s) => new(
        "Oslovení Matěje", "Matěj", "Pavel", "dialogue",
        [
            "Pavel osloví hostinského Matěje.",
            "Matěj Pavla zná z několika návštěv.",
            "Matěj je věcný a přátelský."
        ],
        ["Matěj může Pavla krátce pozdravit a zeptat se, co potřebuje."],
        ["Nové zprávy.", "Pověsti.", "Konkrétní nabídka jídla.", "Tajemství."],
        "Matěj reaguje na oslovení.");

    public static SceneContract TalkHavel(WorldState s) => new(
        "Oslovení Havla", "Havel", "Pavel", "dialogue",
        [
            "Pavel osloví kupce Havla.",
            "Havel Pavla nezná.",
            "Havel je věcný obchodník.",
            "Havel prodává obilí za 4 groše za míru."
        ],
        ["Havel může Pavla stručně vyzvat, aby řekl, co potřebuje."],
        ["Další osoby.", "Příčina ceny obilí.", "Slevy.", "Pověsti."],
        "Havel reaguje věcně.");

    public static SceneContract BargainFailure(WorldState s) => new(
        "Neúspěšné vyjednávání s Havlem", "Havel", "Pavel", "dialogue",
        [
            "Pavel chce koupit obilí za polovinu ceny.",
            "Havel nabídku slušně odmítne.",
            "Cena zůstává 4 groše za míru.",
            "Havel není nepřátelský.",
            "Vztah mezi Pavlem a Havlem se nemění."
        ],
        ["Havel může nabídku odmítnout krátce a věcně."],
        ["Nedostatek obilí.", "Špatná úroda.", "Válka.", "Další zákazníci.", "Slevy.", "Herní mechanika."],
        "Skrytý test vyjednávání neuspěl; mechaniku v textu nezmiňuj.");
}

internal sealed class OllamaRenderer : INarrativeRenderer, IDisposable
{
    private readonly HttpClient _client;
    private readonly string _model;
    private readonly bool _noAi;

    public OllamaRenderer(string baseUrl, string model, bool noAi)
    {
        _model = model;
        _noAi = noAi;
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(3)
        };
    }

    public async Task<string> RenderAsync(SceneContract contract)
    {
        if (_noAi)
            return Fallback(contract);

        var system = """
            Jsi stylistický renderer českého narativního RPG RPGSandbox.
            Herní engine je jediná autorita nad realitou světa. Dostáváš hotový Engine Contract.

            Pravidla:
            - Nepřidávej žádnou osobu, místo, objekt, podnik, událost, motiv, příčinu, pověst ani vztah mimo kontrakt.
            - Povolená atmosféra je pouze v ALLOWED FLAVOR.
            - Dodrž přesně SPEAKER, LISTENER a PERSPECTIVE.
            - Hráč se v herní próze vždy oslovuje ve 2. osobě jednotného čísla: ty, tebe, tobě; nikdy jménem Pavel ve 3. osobě.
            - second_person znamená "vidíš", "jdeš", "stojíš", nikdy "vidíte".
            - Pokud detail není uveden, vynech ho.
            - Nevysvětluj skryté úmysly ani emoce, pokud nejsou faktem.
            - Nevysvětluj engine, pravidla ani hody.
            - Piš přirozenou současnou češtinou jako kvalitní české RPG.
            - Kratší správný text je lepší než kreativní doplnění.
            - Výstup pouze jako finální herní próza, bez nadpisu a komentáře.
            - Obvykle 15–60 slov.
            """;

        var user = $"""
            SCENE: {contract.Title}
            SPEAKER: {contract.Speaker}
            LISTENER: {contract.Listener}
            PERSPECTIVE: {contract.Perspective}

            CANONICAL FACTS:
            {Bullets(contract.Facts)}

            ALLOWED FLAVOR:
            {Bullets(contract.AllowedFlavor)}

            FORBIDDEN:
            {Bullets(contract.Forbidden)}

            ENGINE OUTCOME:
            {contract.Outcome}
            """;

        var request = new
        {
            model = _model,
            stream = false,
            think = false,
            options = new { temperature = 0.18, num_predict = 150 },
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        try
        {
            using var response = await _client.PostAsJsonAsync("api/chat", request);
            var body = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var parsed = JsonSerializer.Deserialize<OllamaResponse>(
                body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var text = parsed?.Message?.Content?.Trim();

            return string.IsNullOrWhiteSpace(text) ? Fallback(contract) : text;
        }
        catch (Exception ex)
        {
            return $"{Fallback(contract)}\n[Lokální AI není dostupná: {ex.Message}]";
        }
    }

    private static string Fallback(SceneContract contract)
    {
        if (contract.Speaker == "Matěj")
            return "Matěj stručně reaguje podle aktuální situace.";
        if (contract.Speaker == "Havel")
            return "Havel odpoví věcně podle aktuální situace.";
        return contract.Outcome;
    }

    private static string Bullets(IEnumerable<string> lines) =>
        string.Join("\n", lines.Select(x => "- " + x));

    public void Dispose() => _client.Dispose();

    private sealed record OllamaMessage(string? Content);
    private sealed record OllamaResponse(OllamaMessage? Message);
}

internal sealed class GameStore : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public GameStore(string path)
    {
        _connection = new SqliteConnection($"Data Source={path}");
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS game_state (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                game_time TEXT NOT NULL,
                location INTEGER NOT NULL,
                money_groats INTEGER NOT NULL,
                matej_knows_plan INTEGER NOT NULL,
                grain_price_groats INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS world_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                game_time TEXT NOT NULL,
                intent TEXT NOT NULL,
                player_input TEXT NOT NULL,
                summary TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<WorldState> LoadOrCreateAsync()
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT game_time, location, money_groats, matej_knows_plan, grain_price_groats
            FROM game_state WHERE id = 1;
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new WorldState
            {
                GameTime = DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Location = (LocationId)reader.GetInt32(1),
                MoneyGroats = reader.GetInt32(2),
                MatejKnowsPlan = reader.GetInt32(3) != 0,
                GrainPriceGroats = reader.GetInt32(4)
            };
        }

        var state = WorldState.NewGame();
        await SaveAsync(state);
        return state;
    }

    public async Task SaveAsync(WorldState state)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO game_state
                (id, game_time, location, money_groats, matej_knows_plan, grain_price_groats)
            VALUES
                (1, $time, $location, $money, $memory, $price)
            ON CONFLICT(id) DO UPDATE SET
                game_time = excluded.game_time,
                location = excluded.location,
                money_groats = excluded.money_groats,
                matej_knows_plan = excluded.matej_knows_plan,
                grain_price_groats = excluded.grain_price_groats;
            """;
        cmd.Parameters.AddWithValue("$time", state.GameTime.ToString("O"));
        cmd.Parameters.AddWithValue("$location", (int)state.Location);
        cmd.Parameters.AddWithValue("$money", state.MoneyGroats);
        cmd.Parameters.AddWithValue("$memory", state.MatejKnowsPlan ? 1 : 0);
        cmd.Parameters.AddWithValue("$price", state.GrainPriceGroats);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AppendEventAsync(DateTime time, string intent, string input, string summary)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO world_events(game_time, intent, player_input, summary)
            VALUES($time, $intent, $input, $summary);
            """;
        cmd.Parameters.AddWithValue("$time", time.ToString("O"));
        cmd.Parameters.AddWithValue("$intent", intent);
        cmd.Parameters.AddWithValue("$input", input);
        cmd.Parameters.AddWithValue("$summary", summary);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetAsync(WorldState state)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM world_events; DELETE FROM game_state;";
        await cmd.ExecuteNonQueryAsync();
        await SaveAsync(state);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

internal interface INarrativeRenderer
{
    Task<string> RenderAsync(SceneContract contract);
}

internal interface IIntentInterpreter
{
    Task<ParsedIntent?> InterpretAsync(
        string input,
        WorldState state,
        string? conversationTarget,
        List<string> availableTargets);
}

internal sealed class WorldState
{
    public DateTime GameTime { get; set; }
    public LocationId Location { get; set; }
    public int MoneyGroats { get; set; }
    public bool MatejKnowsPlan { get; set; }
    public int GrainPriceGroats { get; set; }

    public static WorldState NewGame() => new()
    {
        GameTime = new DateTime(1427, 5, 14, 7, 10, 0),
        Location = LocationId.InnRoom,
        MoneyGroats = 18,
        MatejKnowsPlan = false,
        GrainPriceGroats = 4
    };
}

internal sealed record SceneContract(
    string Title,
    string Speaker,
    string Listener,
    string Perspective,
    List<string> Facts,
    List<string> AllowedFlavor,
    List<string> Forbidden,
    string Outcome);

internal sealed record ParsedIntent(
    PlayerIntent Intent,
    string? Target,
    string Source);

internal sealed record Resolution(
    bool Accepted,
    int Minutes,
    LocationId? NewLocation,
    SceneContract? Contract,
    string EventSummary,
    string SystemMessage,
    bool SetMatejKnowsPlan,
    string? ConversationTarget)
{
    public static Resolution Ok(
        int minutes,
        LocationId? location,
        SceneContract contract,
        string summary,
        bool setMatejKnowsPlan = false,
        string? conversationTarget = null) =>
        new(true, minutes, location, contract, summary, "", setMatejKnowsPlan, conversationTarget);

    public static Resolution Reject(string message) =>
        new(false, 0, null, null, "", message, false, null);
}

internal enum PlayerIntent
{
    Unknown,
    LookAround,
    LookRoom,
    LookWindow,
    InspectMatej,
    InspectHavel,
    GoDownstairs,
    GoSquare,
    GoInn,
    TalkMatej,
    TalkHavel,
    GreetMatej,
    GreetHavel,
    AskHowAreYou,
    AskFood,
    OrderBreakfast,
    AskBusy,
    BreakfastBusy,
    AskSecret,
    TellPlan,
    GoodbyeMatej,
    BargainHalf
}

internal enum LocationId
{
    InnRoom = 1,
    InnCommonRoom = 2,
    MarketSquare = 3
}

internal static class LocationNames
{
    public static string Name(LocationId location) => location switch
    {
        LocationId.InnRoom => "pokoj v hostinci U Tří korun",
        LocationId.InnCommonRoom => "společenská místnost hostince U Tří korun",
        LocationId.MarketSquare => "rabenfeldské náměstí",
        _ => location.ToString()
    };
}

internal static class TextNorm
{
    public static bool ContainsAny(string text, params string[] fragments) =>
        fragments.Any(text.Contains);

    public static string Normalize(string text)
    {
        var formD = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = formD.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
