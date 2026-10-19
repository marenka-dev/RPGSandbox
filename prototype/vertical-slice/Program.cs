using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RPGSandbox.VerticalSlice;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rendererModel = Option(args, "--model") ?? "gemma3:12b";
        var semanticModel = Option(args, "--intent-model") ?? "qwen3:8b";
        var ollamaUrl = Option(args, "--url") ?? "http://127.0.0.1:11434";
        var savePath = Option(args, "--save") ?? "rabenfeld-v01.db";
        var noAi = args.Contains("--no-ai", StringComparer.OrdinalIgnoreCase);
        var noSemanticAi = noAi || args.Contains("--no-intent-ai", StringComparer.OrdinalIgnoreCase);

        await using var store = new GameStore(savePath);
        await store.InitializeAsync();
        var state = await store.LoadOrCreateAsync();

        using var world = new WorldGraph(savePath);
        world.Initialize(state);
        world.SyncPlayerState(state);

        using var renderer = new OllamaRenderer(ollamaUrl, rendererModel, noAi);
        using var interpreter = new OllamaSemanticInterpreter(ollamaUrl, semanticModel, noSemanticAi);
        var engine = new GameEngine(store, renderer, interpreter, world);

        PrintHeader(rendererModel, semanticModel, savePath, noAi, noSemanticAi);
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

            if (input.Equals("pomoc", StringComparison.OrdinalIgnoreCase))
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
                world.ResetAndSeed(state);
                world.SyncPlayerState(state);
                engine.ResetContext();
                Console.WriteLine("Svět vertical slice byl resetován.");
                await engine.RenderArrivalAsync(state);
                continue;
            }

            await engine.HandleAsync(state, input);
        }
    }

    private static void PrintHeader(
        string rendererModel,
        string semanticModel,
        string savePath,
        bool noAi,
        bool noSemanticAi)
    {
        Console.WriteLine("RPGSandbox – Rabenfeld vertical slice v0.4 World Graph & Concretization");
        Console.WriteLine($"Vypravěč: {(noAi ? "fallback bez AI" : rendererModel)}");
        Console.WriteLine($"Interpret významu: {(noSemanticAi ? "jen lokální rychlá pravidla" : semanticModel)}");
        Console.WriteLine($"Save: {Path.GetFullPath(savePath)}");
        Console.WriteLine("Napiš, co chceš udělat nebo zjistit. AI překládá jazyk, engine pracuje nad persistentním grafem světa.");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Zkus psát přirozeně. Například:
              kde jsem?
              koho znám?
              kde je Matěj?
              jsou tu i další stánky?
              půjdu za Havlem
              prohlédnu si Havla
              co prodáváš?
              to je moc drahé
              tak za 3?
              půjdu za Matějem
              můžu se s někým seznámit?
              vezmu židli a dám ji ke dveřím

            Poslední příklad může engine odmítnout, pokud pro něj ještě nemá
            obecná fyzikální pravidla. Neměl by ale vyžadovat přesnou formulaci příkazu.

            Systém:
              stav
              reset
              konec
            """);
    }

    private static void PrintState(WorldState state)
    {
        Console.WriteLine($"Čas: {state.GameTime:dd.MM.yyyy HH:mm}");
        Console.WriteLine($"Lokace: {WorldData.LocationName(state.Location)}");
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
    private readonly ISemanticInterpreter _interpreter;
    private readonly WorldGraph _world;

    private string? _conversationTarget;
    private string? _focusSubject;

    public GameEngine(
        GameStore store,
        INarrativeRenderer renderer,
        ISemanticInterpreter interpreter,
        WorldGraph world)
    {
        _store = store;
        _renderer = renderer;
        _interpreter = interpreter;
        _world = world;
    }

    public void ResetContext()
    {
        _conversationTarget = null;
        _focusSubject = null;
    }

    public async Task RenderArrivalAsync(WorldState state)
    {
        var contract = SceneContracts.Arrival(state);
        await RenderAsync(contract);
    }

    public async Task HandleAsync(WorldState state, string rawInput)
    {
        var command = FastSemanticParser.TryParse(rawInput, state, _conversationTarget, _focusSubject)
            ?? await _interpreter.InterpretAsync(
                rawInput,
                state,
                _conversationTarget,
                _focusSubject,
                _world.VisibleNames(state.Location),
                _world.KnownNames());

        if (command is null || command.Kind == "unknown")
        {
            Console.WriteLine("Význam vstupu se nepodařilo bezpečně určit. Svět se nezměnil.");
            return;
        }

        var validation = SemanticValidator.Validate(command, state);
        if (!validation.IsValid)
        {
            Console.WriteLine(validation.Message);
            return;
        }

        var result = Resolve(state, command, rawInput);
        if (!result.Accepted)
        {
            Console.WriteLine(result.SystemMessage);
            return;
        }

        if (result.DirectAnswer is not null)
        {
            Console.WriteLine(result.DirectAnswer);
            return;
        }

        state.GameTime = state.GameTime.AddMinutes(result.Minutes);

        if (result.NewLocation is not null)
        {
            state.Location = result.NewLocation.Value;
            _conversationTarget = null;
            _focusSubject = null;
        }

        if (result.SetMatejKnowsPlan)
            state.MatejKnowsPlan = true;

        if (result.ConversationTarget is not null)
            _conversationTarget = result.ConversationTarget;

        if (result.FocusSubject is not null)
            _focusSubject = result.FocusSubject;

        await _store.AppendEventAsync(
            state.GameTime,
            $"{command.Kind}:{command.Op}",
            rawInput,
            result.EventSummary);

        await _store.SaveAsync(state);
        _world.SyncPlayerState(state);

        if (result.Contract is not null)
            await RenderAsync(result.Contract);
    }

    private EngineResult Resolve(WorldState state, SemanticCommand command, string rawInput)
    {
        return command.Kind switch
        {
            "query" => ResolveQuery(state, command),
            "action" => ResolveAction(state, command, rawInput),
            _ => EngineResult.Reject("Neznámý typ příkazu. Svět se nezměnil.")
        };
    }

    private EngineResult ResolveQuery(WorldState state, SemanticCommand command)
    {
        var subject = command.Subject ?? command.Target;

        return command.Op switch
        {
            "current_location" =>
                EngineResult.Answer($"Jsi v lokaci: {WorldData.LocationName(state.Location)}."),

            "time" =>
                EngineResult.Answer($"Je {state.GameTime:HH:mm}, {state.GameTime:dd.MM.yyyy}."),

            "money" =>
                EngineResult.Answer($"Máš {state.MoneyGroats} grošů."),

            "inventory" =>
                EngineResult.Answer(_world.InventoryAnswer(state)),

            "known_people" =>
                EngineResult.Answer($"Z aktuálně známých entit: {string.Join(", ", _world.KnownNames())}."),

            "visible_people" or "contents" =>
                EngineResult.Answer(_world.ContentsAnswer(state.Location)),

            "describe" =>
                EngineResult.Answer(_world.Describe(subject ?? "player", state)),

            "can_perceive" =>
                EngineResult.Answer(_world.PerceptionAnswer(subject, state)),

            "location_of" =>
                ResolveLocationQuery(subject),

            "exists_here" =>
                EngineResult.Answer(_world.ExistsHereAnswer(subject, state)),

            "property" =>
                EngineResult.Answer(_world.PropertyAnswer(subject, command.Predicate, state)),

            "available_social_contact" =>
                EngineResult.Answer(_world.AvailableSocialContactAnswer(state.Location)),

            _ =>
                EngineResult.Answer("Dotaz je srozumitelný, ale tento typ vztahu zatím World Graph neumí vyhodnotit.")
        };
    }

    private EngineResult ResolveLocationQuery(string? subject)
    {
        var knownLocation = _world.KnownLocationOf(subject);
        if (knownLocation is null)
            return EngineResult.Answer($"Nevíš spolehlivě, kde se „{subject}“ právě nachází.");

        return EngineResult.Answer(
            $"{_world.EntityNameFromRaw(subject)} je podle tvých aktuálních znalostí v lokaci: {WorldData.LocationName(knownLocation.Value)}.");
    }

    private EngineResult ResolveAction(WorldState state, SemanticCommand command, string rawInput)
    {
        var target = _world.CanonicalPersonName(command.Target);
        var subject = WorldData.CanonicalSubject(command.Subject ?? _focusSubject);

        return command.Op switch
        {
            "observe" => ResolveObserve(state, command),
            "inspect" => ResolveInspect(state, target),
            "move_to" => ResolveMoveTo(state, target ?? command.Target),
            "approach" => ResolveApproach(state, target),
            "speak" => ResolveSpeak(state, target, command),
            "ask" => ResolveAsk(state, target, command),
            "tell" => ResolveTell(state, target, command),
            "request" => ResolveRequest(state, target, subject, command),
            "offer" => ResolveOffer(state, target, subject, command),
            "wait" => ResolveWait(state, command),
            _ => EngineResult.Reject(
                $"Engine rozumí významu „{command.Op}“, ale zatím pro tuto obecnou operaci nemá pravidla. Svět se nezměnil.")
        };
    }

    private static EngineResult ResolveObserve(WorldState state, SemanticCommand command)
    {
        var subject = TextNorm.Normalize(command.Subject ?? "");
        var contract = subject.Contains("okn")
            ? SceneContracts.LookFromWindow(state)
            : subject.Contains("pokoj")
                ? SceneContracts.LookRoom(state)
                : SceneContracts.LookAround(state);

        return EngineResult.Ok(
            2, null, contract, "Pavel pozoroval své okolí.",
            focusSubject: command.Subject);
    }

    private static EngineResult ResolveInspect(WorldState state, string? target)
    {
        if (target == "Havel" && state.Location == LocationId.MarketSquare)
            return EngineResult.Ok(
                1, null, SceneContracts.InspectHavel(state),
                "Pavel si prohlédl Havla.", "Havel", "obilí");

        if (target == "Matěj" && state.Location == LocationId.InnCommonRoom)
            return EngineResult.Ok(
                1, null, SceneContracts.InspectMatej(state),
                "Pavel si prohlédl Matěje.", "Matěj");

        return EngineResult.Reject("Cíl si teď nemůžeš bezpečně prohlédnout z aktuální lokace.");
    }

    private EngineResult ResolveMoveTo(WorldState state, string? target)
    {
        var location = _world.ResolveDestination(target, state);
        if (location is null)
            return EngineResult.Reject("Nevíš, kam přesně by tato cesta měla vést.");

        if (location == state.Location)
        {
            var person = _world.CanonicalPersonName(target);
            if (person is not null)
                return ResolveApproach(state, person);
            return EngineResult.Answer("Už na tom místě jsi.");
        }

        var minutes = WorldData.TravelMinutes(state.Location, location.Value);
        return EngineResult.Ok(
            minutes,
            location,
            SceneContracts.Move(state, location.Value),
            $"Pavel se přesunul do lokace {WorldData.LocationName(location.Value)}.");
    }

    private static EngineResult ResolveApproach(WorldState state, string? target)
    {
        if (target is null || !WorldData.IsVisible(target, state.Location))
            return EngineResult.Reject("Tuto osobu teď v aktuální lokaci nevidíš.");

        return EngineResult.Ok(
            1,
            null,
            SceneContracts.Approach(state, target),
            $"Pavel přistoupil k osobě {target}.",
            target,
            target == "Havel" ? "obilí" : null);
    }

    private static EngineResult ResolveSpeak(
        WorldState state,
        string? target,
        SemanticCommand command)
    {
        target ??= WorldData.DefaultConversationTarget(state.Location);
        if (target is null || !WorldData.IsVisible(target, state.Location))
            return EngineResult.Reject("Nemáš tu nikoho odpovídajícího, ke komu bys mohl právě promluvit.");

        return EngineResult.Ok(
            1,
            null,
            SceneContracts.Speak(state, target, command.Text),
            $"Pavel promluvil k osobě {target}.",
            target,
            target == "Havel" ? "obilí" : null);
    }

    private static EngineResult ResolveAsk(
        WorldState state,
        string? target,
        SemanticCommand command)
    {
        target ??= WorldData.DefaultConversationTarget(state.Location);
        if (target is null || !WorldData.IsVisible(target, state.Location))
            return EngineResult.Reject("Adresát otázky není v aktuální lokaci dostupný.");

        var topic = command.Subject ?? command.Predicate ?? command.Text ?? "";
        var knowledge = NpcKnowledge.Answer(target, topic, state);

        return EngineResult.Ok(
            1,
            null,
            SceneContracts.NpcAnswer(state, target, topic, knowledge),
            $"Pavel položil osobě {target} otázku na téma {topic}.",
            target,
            knowledge.FocusSubject);
    }

    private static EngineResult ResolveTell(
        WorldState state,
        string? target,
        SemanticCommand command)
    {
        target ??= WorldData.DefaultConversationTarget(state.Location);
        if (target is null || !WorldData.IsVisible(target, state.Location))
            return EngineResult.Reject("Adresát sdělení není v aktuální lokaci dostupný.");

        var text = command.Text ?? command.Subject ?? "";
        var setPlan = target == "Matěj" &&
                      TextNorm.Normalize(text).Contains("trh") &&
                      TextNorm.Normalize(text).Contains("radnic");

        return EngineResult.Ok(
            1,
            null,
            SceneContracts.Tell(state, target, text, setPlan),
            $"Pavel něco sdělil osobě {target}.",
            target,
            null,
            setPlan);
    }

    private static EngineResult ResolveRequest(
        WorldState state,
        string? target,
        string? subject,
        SemanticCommand command)
    {
        target ??= WorldData.DefaultConversationTarget(state.Location);

        if (target == "Matěj" && state.Location == LocationId.InnCommonRoom &&
            TextNorm.Normalize(subject ?? command.Text ?? "").Contains("snidan"))
        {
            return EngineResult.Ok(
                4,
                null,
                SceneContracts.Breakfast(state),
                "Matěj přinesl Pavlovi snídani.",
                "Matěj",
                "snídaně");
        }

        return EngineResult.Reject("Požadavek je srozumitelný, ale engine pro něj zatím nemá obecné pravidlo v této situaci.");
    }

    private static EngineResult ResolveOffer(
        WorldState state,
        string? target,
        string? subject,
        SemanticCommand command)
    {
        target ??= WorldData.DefaultConversationTarget(state.Location);

        if (target != "Havel" || state.Location != LocationId.MarketSquare)
            return EngineResult.Reject("Tuto nabídku teď nemáš komu platně předložit.");

        if (!TextNorm.Normalize(subject ?? "").Contains("obil"))
            return EngineResult.Reject("Engine nerozpoznal, čeho se nabídka týká.");

        if (!command.Args.TryGetValue("price", out var rawPrice) ||
            !int.TryParse(rawPrice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offeredPrice))
        {
            return EngineResult.Ok(
                1,
                null,
                SceneContracts.AskForBetterPrice(state),
                "Pavel naznačil Havlovi, že chce jednat o ceně obilí.",
                "Havel",
                "obilí");
        }

        var accepted = offeredPrice >= state.GrainPriceGroats;

        return EngineResult.Ok(
            2,
            null,
            SceneContracts.PriceOffer(state, offeredPrice, accepted),
            accepted
                ? $"Havel přijal nabídnutou cenu {offeredPrice} grošů za míru."
                : $"Havel odmítl nabídnutou cenu {offeredPrice} grošů za míru.",
            "Havel",
            "obilí");
    }

    private static EngineResult ResolveWait(WorldState state, SemanticCommand command)
    {
        var minutes = 5;
        if (command.Args.TryGetValue("minutes", out var raw) &&
            int.TryParse(raw, out var parsed))
            minutes = Math.Clamp(parsed, 1, 120);

        return EngineResult.Ok(
            minutes,
            null,
            SceneContracts.Wait(state, minutes),
            $"Pavel čekal {minutes} minut.");
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

internal static class FastSemanticParser
{
    public static SemanticCommand? TryParse(
        string input,
        WorldState state,
        string? conversationTarget,
        string? focusSubject)
    {
        var n = TextNorm.Normalize(input);

        if (TextNorm.ContainsAny(n, "kde jsem", "kde se nachazim"))
            return SemanticCommand.Query("current_location");

        if (TextNorm.ContainsAny(n, "kolik je hodin", "jaky je cas"))
            return SemanticCommand.Query("time");

        if (TextNorm.ContainsAny(n, "kolik mam penez", "kolik mam gros"))
            return SemanticCommand.Query("money");

        if (TextNorm.ContainsAny(n, "koho znam"))
            return SemanticCommand.Query("known_people");

        if (n.StartsWith("kde je "))
            return SemanticCommand.Query("location_of", subject: input[7..].Trim(' ', '?', '.', '!'));

        if (TextNorm.ContainsAny(n, "jsou tu i dalsi stanky", "jsou tu dalsi stanky"))
            return SemanticCommand.Query("exists_here", subject: "stánky");

        if (TextNorm.ContainsAny(n, "muzu se s nekym seznamit", "je tu nekdo koho neznam"))
            return SemanticCommand.Query("available_social_contact");

        if (TextNorm.ContainsAny(n, "pujdu za ", "jdu za ", "dojdu za "))
        {
            var target = ExtractAfter(input, "za ");
            return SemanticCommand.Action("move_to", target: target);
        }

        if (TextNorm.ContainsAny(n, "prohlidnu si", "prohlednu si", "podivam se na"))
        {
            var target = GuessTarget(input);
            if (target is not null)
                return SemanticCommand.Action("inspect", target: target);
        }

        if (TextNorm.ContainsAny(n, "promluvim s", "oslovim", "pozdravim"))
        {
            var target = GuessTarget(input) ?? conversationTarget;
            return SemanticCommand.Action("speak", target: target, text: input);
        }

        if (TextNorm.ContainsAny(n, "poprosim", "pozadam") && n.Contains("snidan"))
            return SemanticCommand.Action("request", target: conversationTarget ?? "Matěj", subject: "snídaně");

        if (conversationTarget == "Havel" && focusSubject == "obilí")
        {
            var price = ExtractStandaloneNumber(n);
            if (price is not null && TextNorm.ContainsAny(n, "za ", "tak za", "dam", "nabid"))
                return SemanticCommand.Action(
                    "offer", target: "Havel", subject: "obilí",
                    args: new Dictionary<string, string> { ["price"] = price.Value.ToString(CultureInfo.InvariantCulture) });

            if (TextNorm.ContainsAny(n, "drahe", "moc stoji", "sleva", "slevit"))
                return SemanticCommand.Action("offer", target: "Havel", subject: "obilí");
        }

        return null;
    }

    private static string? GuessTarget(string input)
    {
        var n = TextNorm.Normalize(input);
        if (n.Contains("havel")) return "Havel";
        if (n.Contains("matej")) return "Matěj";
        return null;
    }

    private static string ExtractAfter(string input, string marker)
    {
        var index = TextNorm.Normalize(input).IndexOf(marker, StringComparison.Ordinal);
        if (index < 0) return input;
        return input[(index + marker.Length)..].Trim(' ', '.', '?', '!');
    }

    private static int? ExtractStandaloneNumber(string n)
    {
        foreach (var part in n.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(part.Trim('?', '!', '.', ','), out var value))
                return value;
        return null;
    }
}

internal sealed class OllamaSemanticInterpreter : ISemanticInterpreter, IDisposable
{
    private readonly HttpClient _client;
    private readonly string _model;
    private readonly bool _disabled;

    public OllamaSemanticInterpreter(string baseUrl, string model, bool disabled)
    {
        _model = model;
        _disabled = disabled;
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(45)
        };
    }

    public async Task<SemanticCommand?> InterpretAsync(
        string input,
        WorldState state,
        string? conversationTarget,
        string? focusSubject,
        List<string> visibleEntities,
        List<string> knownEntities)
    {
        if (_disabled)
            return null;

        var system = """
            Jsi sémantický překladač vstupu hráče pro sandbox RPG.
            NEROZHODUJEŠ, co se ve světě stane. Pouze přeložíš českou větu do obecného významu.

            Vrať pouze JSON:
            {
              "kind":"action|query|unknown",
              "op":"...",
              "target":null,
              "subject":null,
              "predicate":null,
              "text":null,
              "args":{"price":"3"}
            }

            Všechny hodnoty uvnitř args zapisuj jako STRING, i když jde o číslo.
            Pokud žádný parametr není, vrať "args":{}.

            Povolené ACTION op:
            observe, inspect, move_to, approach, speak, ask, tell, request, offer, wait

            Povolené QUERY op:
            current_location, time, money, known_people, visible_people,
            location_of, exists_here, property, available_social_contact

            Zásady:
            - Neodhaduj výsledek akce.
            - Nevymýšlej nové entity.
            - target je adresát/cíl, subject je předmět nebo téma.
            - U ceny použij args.price jen pokud hráč skutečně uvedl konkrétní číslo.
            - Věta "to je moc drahé" NENÍ nabídka konkrétní ceny. Může být offer bez price nebo speak.
            - Zájmena řeš z CURRENT CONVERSATION a FOCUS SUBJECT.
            - "Půjdu za X" je move_to s target X.
            - "Kde je X" je query location_of se subject X.
            - Otázka NPC je action ask, nikoli world query, pokud se hráč ptá konkrétní postavy.
            - Pokud význam nejde bezpečně vyjádřit povolenými operacemi, vrať unknown.
            """;

        var user = $"""
            CURRENT LOCATION: {WorldData.LocationName(state.Location)}
            CURRENT CONVERSATION: {conversationTarget ?? "null"}
            FOCUS SUBJECT: {focusSubject ?? "null"}
            VISIBLE ENTITIES: {(visibleEntities.Count == 0 ? "none" : string.Join(", ", visibleEntities))}
            KNOWN ENTITIES: {(knownEntities.Count == 0 ? "none" : string.Join(", ", knownEntities))}
            PLAYER INPUT: {input}
            """;

        var request = new
        {
            model = _model,
            stream = false,
            think = false,
            format = "json",
            options = new { temperature = 0.0, num_predict = 160 },
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

            var envelope = JsonSerializer.Deserialize<OllamaEnvelope>(
                body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var content = envelope?.Message?.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var command = JsonSerializer.Deserialize<SemanticCommand>(
                content, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return command?.Normalize();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();

    private sealed record OllamaMessage(string? Content);
    private sealed record OllamaEnvelope(OllamaMessage? Message);
}

internal static class SemanticValidator
{
    private static readonly HashSet<string> ActionOps =
    [
        "observe", "inspect", "move_to", "approach", "speak",
        "ask", "tell", "request", "offer", "wait"
    ];

    private static readonly HashSet<string> QueryOps =
    [
        "current_location", "time", "money", "known_people", "visible_people",
        "location_of", "exists_here", "property", "available_social_contact"
    ];

    public static ValidationResult Validate(SemanticCommand command, WorldState state)
    {
        if (command.Kind == "action" && !ActionOps.Contains(command.Op))
            return ValidationResult.Fail("AI navrhla nepovolenou akci. Svět se nezměnil.");

        if (command.Kind == "query" && !QueryOps.Contains(command.Op))
            return ValidationResult.Fail("AI navrhla nepovolený dotaz. Svět se nezměnil.");

        if (command.Kind is not ("action" or "query"))
            return ValidationResult.Fail("Neplatný typ sémantického příkazu.");

        var target = WorldData.CanonicalEntity(command.Target);
        if (command.Target is not null && target is null &&
            command.Op is not ("move_to" or "exists_here" or "property"))
        {
            return ValidationResult.Fail("Cíl příkazu neodpovídá žádné známé nebo dostupné entitě.");
        }

        return ValidationResult.Ok();
    }
}

internal static class NpcKnowledge
{
    public static KnowledgeAnswer Answer(string npc, string topic, WorldState state)
    {
        var t = TextNorm.Normalize(topic);

        if (npc == "Matěj")
        {
            if (TextNorm.ContainsAny(t, "jak se mas", "zdravi", "jak se dari"))
                return new("Matěj je v pořádku a právě pracuje v hostinci.", null);

            if (TextNorm.ContainsAny(t, "jidlo", "snidan", "jist"))
                return new("Matěj může nabídnout běžnou snídani; konkrétní složení není definováno.", "snídaně");

            if (TextNorm.ContainsAny(t, "rusno", "trh", "namesti"))
                return new("Dnes je tržní den a náměstí bývá během tržního dne rušnější.", "trh");

            if (TextNorm.ContainsAny(t, "tajna dohoda", "dohoda rady", "rada"))
                return new("Matěj nemá žádnou spolehlivou informaci o údajné tajné dohodě a neví, zda existuje.", "městská rada");

            return new("Matěj k tomuto tématu nemá v dostupných znalostech žádnou spolehlivou informaci.", topic);
        }

        if (npc == "Havel")
        {
            if (TextNorm.ContainsAny(t, "co prodavas", "zbozi", "obil"))
                return new($"Havel prodává obilí za {state.GrainPriceGroats} groše za míru.", "obilí");

            if (TextNorm.ContainsAny(t, "cena", "kolik", "drahe", "stoji"))
                return new($"Havlova aktuální cena obilí je {state.GrainPriceGroats} groše za míru.", "obilí");

            return new("Havel k tomuto tématu nemá v prototypu žádnou další definovanou znalost.", topic);
        }

        return new("NPC nemá k tématu definovanou znalost.", topic);
    }
}

internal static class WorldData
{
    public static string LocationName(LocationId location) => location switch
    {
        LocationId.InnRoom => "pokoj v hostinci U Tří korun",
        LocationId.InnCommonRoom => "společenská místnost hostince U Tří korun",
        LocationId.MarketSquare => "rabenfeldské náměstí",
        _ => location.ToString()
    };

    public static List<string> VisibleEntities(LocationId location) => location switch
    {
        LocationId.InnCommonRoom => ["Matěj"],
        LocationId.MarketSquare => ["Havel", "stánky", "povozy", "měšťané"],
        _ => ["okno", "postel", "stůl", "židle", "dveře"]
    };

    public static List<string> KnownEntities(WorldState state) =>
        ["Matěj", "hostinec U Tří korun", "rabenfeldské náměstí"];

    public static string? CanonicalEntity(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var n = TextNorm.Normalize(raw);
        if (n.Contains("matej")) return "Matěj";
        if (n.Contains("havel")) return "Havel";
        return null;
    }

    public static string? CanonicalSubject(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var n = TextNorm.Normalize(raw);
        if (n.Contains("obil")) return "obilí";
        if (n.Contains("snidan")) return "snídaně";
        return raw.Trim();
    }

    public static string DisplayName(string canonical) => canonical;

    public static LocationId? KnownLocationOf(string entity, WorldState state) => entity switch
    {
        "Matěj" => LocationId.InnCommonRoom,
        "Havel" when state.Location == LocationId.MarketSquare => LocationId.MarketSquare,
        _ => null
    };

    public static LocationId? ResolveDestination(string? raw, WorldState state)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var person = CanonicalEntity(raw);
        if (person is not null)
            return KnownLocationOf(person, state);

        var n = TextNorm.Normalize(raw);
        if (n.Contains("hostinc") || n.Contains("tri korun") || n.Contains("matej"))
            return LocationId.InnCommonRoom;
        if (n.Contains("namest") || n.Contains("trh") || n.Contains("havel"))
            return LocationId.MarketSquare;
        if (n.Contains("pokoj"))
            return LocationId.InnRoom;
        return null;
    }

    public static int TravelMinutes(LocationId from, LocationId to)
    {
        if (from == to) return 0;
        if ((from == LocationId.InnRoom && to == LocationId.InnCommonRoom) ||
            (from == LocationId.InnCommonRoom && to == LocationId.InnRoom))
            return 2;
        return 5;
    }

    public static bool IsVisible(string entity, LocationId location) =>
        VisibleEntities(location).Contains(entity, StringComparer.Ordinal);

    public static string? DefaultConversationTarget(LocationId location) => location switch
    {
        LocationId.InnCommonRoom => "Matěj",
        LocationId.MarketSquare => "Havel",
        _ => null
    };

    public static string VisiblePeopleAnswer(LocationId location) => location switch
    {
        LocationId.InnCommonRoom => "V této části prototypu tu vidíš Matěje.",
        LocationId.MarketSquare => "Na náměstí vidíš Havla a také další měšťany, kteří zatím nejsou konkretizováni jako persistentní postavy.",
        _ => "V pokoji teď žádnou další osobu nevidíš."
    };

    public static string AvailableSocialContactAnswer(LocationId location) => location switch
    {
        LocationId.MarketSquare => "Ano. Kromě Havla jsou na tržišti další měšťané. Zatím nejsou konkretizováni; budoucí engine z nich při skutečném kontaktu vytvoří persistentní NPC.",
        LocationId.InnCommonRoom => "V aktuálním vertical slice je konkrétní dostupnou postavou Matěj.",
        _ => "V pokoji teď není nikdo, s kým by ses mohl nově seznámit."
    };
}

internal static class SceneContracts
{
    public static SceneContract Arrival(WorldState state) => state.Location switch
    {
        LocationId.InnRoom => Contract(
            state, "Pokoj U Tří korun", "NARRATOR", "PLAYER", "second_person",
            ["Je časné ráno.", "Hráč je v pokoji hostince U Tří korun.", "Z okna je vidět rabenfeldské náměstí."],
            ["Může být zmíněno ranní světlo."],
            ["Nové osoby.", "Nové předměty.", "Jiná lokace."],
            "Uveď hráče stručně do aktuální lokace."),

        LocationId.InnCommonRoom => Contract(
            state, "Společenská místnost U Tří korun", "NARRATOR", "PLAYER", "second_person",
            ["Hráč je ve společenské místnosti hostince U Tří korun.", "Matěj je přítomen."],
            ["Může zaznít neurčitý ruch hostince."],
            ["Další konkrétní hosté.", "Konkrétní jídlo.", "Jiná lokace."],
            "Stručně popiš aktuální lokaci."),

        _ => Contract(
            state, "Rabenfeldské náměstí", "NARRATOR", "PLAYER", "second_person",
            ["Hráč je na rabenfeldském náměstí.", "Probíhá běžný tržní den.", "Havel zde prodává obilí za 4 groše za míru."],
            ["Může zaznít neurčitý tržní ruch."],
            ["Havlův krám nebo interiér.", "Další pojmenované osoby.", "Jiná lokace."],
            "Stručně popiš aktuální lokaci.")
    };

    public static SceneContract LookRoom(WorldState s) => Contract(
        s, "Prohlídka pokoje", "NARRATOR", "PLAYER", "second_person",
        ["Hráč je v pokoji U Tří korun.", "V pokoji je postel, stůl, židle, okno a dveře."],
        ["Může být zmíněno ranní světlo."],
        ["Dění na trhu.", "Další nábytek.", "Nové osoby.", "Jiná lokace."],
        "Hráč si krátce prohlédne pokoj.");

    public static SceneContract LookFromWindow(WorldState s) => Contract(
        s, "Pohled z okna", "NARRATOR", "PLAYER", "second_person",
        ["Hráč stojí u okna pokoje U Tří korun.", "Na náměstí začíná běžný trh.", "Vidí stánky, povozy a měšťany.", "Nic mimořádného se neděje."],
        ["Může být zmíněno ranní světlo a neurčitý ruch."],
        ["Konkrétní zboží.", "Nové osoby.", "Jiná lokace."],
        "Hráč pozoruje město.");

    public static SceneContract LookAround(WorldState s) => s.Location switch
    {
        LocationId.InnRoom => LookRoom(s),
        LocationId.InnCommonRoom => Contract(
            s, "Rozhlédnutí v hostinci", "NARRATOR", "PLAYER", "second_person",
            ["Hráč je ve společenské místnosti U Tří korun.", "Matěj je přítomen."],
            ["Může zaznít neurčitý ruch místnosti."],
            ["Další konkrétní lidé.", "Konkrétní jídlo.", "Jiná lokace."],
            "Hráč se krátce rozhlédne."),
        _ => Contract(
            s, "Rozhlédnutí na trhu", "NARRATOR", "PLAYER", "second_person",
            ["Hráč stojí na rabenfeldském náměstí.", "Probíhá běžný tržní den.", "Vidí více stánků, povozy a měšťany.", "Havel prodává obilí za 4 groše za míru."],
            ["Může zaznít neurčitý hluk trhu."],
            ["Havlův krám nebo interiér.", "Konkrétní jiné zboží.", "Jiná lokace."],
            "Hráč se krátce rozhlédne.")
    };

    public static SceneContract InspectMatej(WorldState s) => Contract(
        s, "Prohlídka Matěje", "NARRATOR", "PLAYER", "second_person",
        ["Matěj je hostinský kolem padesáti let.", "Hráč ho zná z několika návštěv.", "Matěj je v hostinci U Tří korun."],
        ["Může působit jako člověk zvyklý na práci v hostinci."],
        ["Skrytá minulost.", "Tajemství.", "Nové předměty.", "Jiná lokace."],
        "Hráč si Matěje krátce prohlédne.");

    public static SceneContract InspectHavel(WorldState s) => Contract(
        s, "Prohlídka Havla", "NARRATOR", "PLAYER", "second_person",
        ["Havel je kupec na rabenfeldském náměstí.", "Prodává obilí za 4 groše za míru.", "Hráče osobně nezná."],
        ["Havel může působit věcně a soustředěně na obchod."],
        ["Havlův krám nebo interiér.", "Třídění zrna.", "Další osoby.", "Skryté vlastnosti.", "Jiná lokace."],
        "Hráč si Havla krátce prohlédne.");

    public static SceneContract Move(WorldState before, LocationId destination)
    {
        var after = before.CloneWithLocation(destination);
        return destination switch
        {
            LocationId.InnRoom => Contract(
                after, "Cesta do pokoje", "NARRATOR", "PLAYER", "second_person",
                ["Hráč se přesune do svého pokoje v hostinci U Tří korun."],
                ["Přesun popiš stručně."], ["Nové události.", "Jiný cíl cesty."], "Přesun se podařil."),

            LocationId.InnCommonRoom => Contract(
                after, "Cesta do hostince", "NARRATOR", "PLAYER", "second_person",
                ["Hráč dorazí do společenské místnosti hostince U Tří korun.", "Matěj je přítomen."],
                ["Přesun popiš stručně."], ["Další konkrétní osoby.", "Jiná lokace."], "Přesun se podařil."),

            _ => Contract(
                after, "Cesta na náměstí", "NARRATOR", "PLAYER", "second_person",
                ["Hráč dorazí na rabenfeldské náměstí.", "Probíhá běžný tržní den.", "Havel zde prodává obilí."],
                ["Může zaznít neurčitý tržní ruch."], ["Havlův krám.", "Jiná lokace."], "Přesun se podařil.")
        };
    }

    public static SceneContract Approach(WorldState s, string target) => Contract(
        s, $"Přistoupení k {target}", target, "PLAYER", "dialogue",
        [$"Hráč přistoupí k osobě {target}.", $"{target} je v aktuální lokaci."],
        [$"{target} může krátce reagovat na příchod hráče."],
        ["Nová lokace.", "Nové osoby.", "Nové informace."],
        $"{target} zaznamená hráčovu přítomnost.");

    public static SceneContract Speak(WorldState s, string target, string? text) => Contract(
        s, $"Rozhovor s {target}", target, "PLAYER", "dialogue",
        [$"Hráč promluví k osobě {target}.", text is null ? "Obsah není dále specifikován." : $"Hráč řekl: {text}", $"{target} je v aktuální lokaci."],
        [$"{target} může krátce reagovat bez doplnění nových faktů."],
        ["Nové informace.", "Nová lokace.", "Další osoby."],
        "Reakce má odpovídat pouze známým skutečnostem.");

    public static SceneContract NpcAnswer(
        WorldState s, string npc, string topic, KnowledgeAnswer answer) => Contract(
        s, $"Odpověď {npc}", npc, "PLAYER", "dialogue",
        [$"Hráč se ptá osoby {npc} na téma: {topic}.", $"Kanonická znalost NPC pro odpověď: {answer.Fact}"],
        [$"{npc} může odpovědět přirozeně a stručně."],
        ["Další znalosti mimo uvedenou kanonickou znalost.", "Nová lokace.", "Další osoby."],
        "NPC odpoví pouze v mezích své znalosti.");

    public static SceneContract Tell(WorldState s, string target, string text, bool remembersPlan) => Contract(
        s, $"Sdělení osobě {target}", target, "PLAYER", "dialogue",
        [$"Hráč sdělí osobě {target}: {text}", remembersPlan ? "Matěj si zapamatuje plán: trh a potom radnice." : "Sdělení nemění jiná kanonická fakta."],
        [$"{target} může krátce potvrdit, že slyšel sdělení."],
        ["Doplňování dalších plánů.", "Nová lokace."],
        "Sdělení bylo vysloveno.");

    public static SceneContract Breakfast(WorldState s) => Contract(
        s, "Snídaně", "Matěj", "PLAYER", "dialogue",
        ["Hráč požádal o snídani.", "Matěj přinese běžnou snídani.", "Konkrétní složení ani cena nejsou definovány."],
        ["Matěj může snídani položit před hráče."],
        ["Konkrétní jídlo.", "Konkrétní cena.", "Další hosté.", "Jiná lokace."],
        "Hráč dostane snídani.");

    public static SceneContract AskForBetterPrice(WorldState s) => Contract(
        s, "Jednání o ceně", "Havel", "PLAYER", "dialogue",
        [$"Aktuální cena obilí je {s.GrainPriceGroats} groše za míru.", "Hráč vyjádřil nespokojenost s cenou, ale neuvedl konkrétní nabídku."],
        ["Havel může stručně potvrdit svou aktuální cenu."],
        ["Konkrétní nabídnutá cena.", "Polovina ceny.", "Sleva, kterou engine neurčil.", "Další zákazníci."],
        "Cena se nemění.");

    public static SceneContract PriceOffer(WorldState s, int price, bool accepted) => Contract(
        s, "Cenová nabídka Havlovi", "Havel", "PLAYER", "dialogue",
        [
            $"Hráč nabízí {price} grošů za míru obilí.",
            $"Havlova aktuální cena je {s.GrainPriceGroats} groše za míru.",
            accepted ? "Havel tuto cenu přijme." : "Havel tuto cenu odmítne.",
            "Vztah mezi hráčem a Havlem se touto jednou nabídkou nemění."
        ],
        ["Havel může odpovědět krátce a věcně."],
        ["Jiná nabídnutá cena.", "Příčina Havlovy ceny.", "Další zákazníci.", "Nová lokace."],
        accepted ? "Havel souhlasí s nabídnutou cenou." : "Havel nabídku odmítne.");

    public static SceneContract Wait(WorldState s, int minutes) => Contract(
        s, "Čekání", "NARRATOR", "PLAYER", "second_person",
        [$"Hráč čeká {minutes} minut.", "Během čekání nenastane žádná mimořádná událost."],
        ["Může být zmíněn běžný ruch aktuální lokace."],
        ["Nové události.", "Nové osoby.", "Jiná lokace."],
        "Čas uplyne bez významné události.");

    private static SceneContract Contract(
        WorldState state,
        string title,
        string speaker,
        string listener,
        string perspective,
        List<string> facts,
        List<string> flavor,
        List<string> forbidden,
        string outcome) =>
        new(
            title,
            WorldData.LocationName(state.Location),
            speaker,
            listener,
            perspective,
            facts,
            flavor,
            forbidden,
            outcome);
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
            return contract.Outcome;

        var system = """
            Jsi stylistický renderer českého narativního RPG RPGSandbox.
            Engine už rozhodl realitu světa.

            Absolutní pravidla:
            - CANONICAL LOCATION je jediná aktuální lokace hráče. Nikdy nevytvářej jiný interiér, krám, místnost ani místo.
            - Nepřidávej osoby, objekty, události, motivy, příčiny, pověsti ani vztahy mimo kontrakt.
            - Hráče oslovuj ve 2. osobě jednotného čísla, nikdy jménem Pavel ve 3. osobě.
            - Pokud detail není uveden, vynech ho.
            - Nevysvětluj engine ani pravidla.
            - Piš přirozenou současnou češtinou.
            - Kratší přesný text je lepší než kreativní doplnění.
            - Výstup pouze jako finální herní próza.
            """;

        var user = $"""
            CANONICAL LOCATION: {contract.CanonicalLocation}
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
            options = new { temperature = 0.15, num_predict = 140 },
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

            var envelope = JsonSerializer.Deserialize<OllamaEnvelope>(
                body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return envelope?.Message?.Content?.Trim() is { Length: > 0 } text
                ? text
                : contract.Outcome;
        }
        catch (Exception ex)
        {
            return $"{contract.Outcome}\n[Lokální AI není dostupná: {ex.Message}]";
        }
    }

    private static string Bullets(IEnumerable<string> lines) =>
        string.Join("\n", lines.Select(x => "- " + x));

    public void Dispose() => _client.Dispose();

    private sealed record OllamaMessage(string? Content);
    private sealed record OllamaEnvelope(OllamaMessage? Message);
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

internal interface ISemanticInterpreter
{
    Task<SemanticCommand?> InterpretAsync(
        string input,
        WorldState state,
        string? conversationTarget,
        string? focusSubject,
        List<string> visibleEntities,
        List<string> knownEntities);
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

    public WorldState CloneWithLocation(LocationId location) => new()
    {
        GameTime = GameTime,
        Location = location,
        MoneyGroats = MoneyGroats,
        MatejKnowsPlan = MatejKnowsPlan,
        GrainPriceGroats = GrainPriceGroats
    };
}

internal sealed record SemanticCommand(
    string Kind,
    string Op,
    string? Target,
    string? Subject,
    string? Predicate,
    string? Text,
    Dictionary<string, string> Args)
{
    public static SemanticCommand Query(
        string op,
        string? subject = null,
        string? predicate = null,
        Dictionary<string, string>? args = null) =>
        new("query", op, null, subject, predicate, null, args ?? new());

    public static SemanticCommand Action(
        string op,
        string? target = null,
        string? subject = null,
        string? predicate = null,
        string? text = null,
        Dictionary<string, string>? args = null) =>
        new("action", op, target, subject, predicate, text, args ?? new());

    public SemanticCommand Normalize() => this with
    {
        Kind = Kind?.Trim().ToLowerInvariant() ?? "unknown",
        Op = Op?.Trim().ToLowerInvariant() ?? "unknown",
        Args = Args ?? new Dictionary<string, string>()
    };
}

internal sealed record SceneContract(
    string Title,
    string CanonicalLocation,
    string Speaker,
    string Listener,
    string Perspective,
    List<string> Facts,
    List<string> AllowedFlavor,
    List<string> Forbidden,
    string Outcome);

internal sealed record KnowledgeAnswer(string Fact, string? FocusSubject);

internal sealed record ValidationResult(bool IsValid, string Message)
{
    public static ValidationResult Ok() => new(true, "");
    public static ValidationResult Fail(string message) => new(false, message);
}

internal sealed record EngineResult(
    bool Accepted,
    int Minutes,
    LocationId? NewLocation,
    SceneContract? Contract,
    string EventSummary,
    string SystemMessage,
    string? DirectAnswer,
    string? ConversationTarget,
    string? FocusSubject,
    bool SetMatejKnowsPlan)
{
    public static EngineResult Ok(
        int minutes,
        LocationId? location,
        SceneContract contract,
        string summary,
        string? conversationTarget = null,
        string? focusSubject = null,
        bool setMatejKnowsPlan = false) =>
        new(true, minutes, location, contract, summary, "", null,
            conversationTarget, focusSubject, setMatejKnowsPlan);

    public static EngineResult Answer(string text) =>
        new(true, 0, null, null, "", "", text, null, null, false);

    public static EngineResult Reject(string text) =>
        new(false, 0, null, null, "", text, null, null, null, false);
}

internal enum LocationId
{
    InnRoom = 1,
    InnCommonRoom = 2,
    MarketSquare = 3
}

internal static class TextNorm
{
    public static bool ContainsAny(string text, params string[] fragments) =>
        fragments.Any(text.Contains);

    public static string Normalize(string text)
    {
        var formD = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = formD.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
