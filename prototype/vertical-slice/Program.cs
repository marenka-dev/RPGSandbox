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

        var model = Option(args, "--model") ?? "gemma3:12b";
        var ollamaUrl = Option(args, "--url") ?? "http://127.0.0.1:11434";
        var savePath = Option(args, "--save") ?? "rabenfeld-v01.db";
        var noAi = args.Contains("--no-ai", StringComparer.OrdinalIgnoreCase);

        await using var store = new GameStore(savePath);
        await store.InitializeAsync();
        var state = await store.LoadOrCreateAsync();

        using var renderer = new OllamaRenderer(ollamaUrl, model, noAi);
        var engine = new GameEngine(store, renderer);

        PrintHeader(model, savePath, noAi);
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
                Console.WriteLine("Svět vertical slice byl resetován.");
                await engine.RenderArrivalAsync(state);
                continue;
            }

            await engine.HandleAsync(state, input);
        }
    }

    private static void PrintHeader(string model, string savePath, bool noAi)
    {
        Console.WriteLine("RPGSandbox – Rabenfeld vertical slice v0.1");
        Console.WriteLine($"Renderer: {(noAi ? "fallback bez AI" : model)}");
        Console.WriteLine($"Save: {Path.GetFullPath(savePath)}");
        Console.WriteLine("Piš vlastní krátké příkazy česky. Pro nápovědu napiš 'pomoc'.");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Příklady:
              rozhlédnu se
              podívám se z okna
              sejdu dolů za Matějem
              poprosím Matěje o snídani a zeptám se, jestli je dnes rušno
              zeptám se Matěje na tajnou dohodu rady
              rozloučím se s Matějem
              půjdu na náměstí
              prohlédnu si trh
              zkusím Havla přesvědčit, aby mi prodal obilí za polovinu
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

    public GameEngine(GameStore store, INarrativeRenderer renderer)
    {
        _store = store;
        _renderer = renderer;
    }

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
        var intent = IntentParser.Parse(rawInput, state.Location);
        var resolution = Resolve(state, intent, rawInput);

        if (!resolution.Accepted)
        {
            Console.WriteLine(resolution.SystemMessage);
            return;
        }

        state.GameTime = state.GameTime.AddMinutes(resolution.Minutes);
        if (resolution.NewLocation is not null)
            state.Location = resolution.NewLocation.Value;
        if (resolution.SetMatejKnowsPlan)
            state.MatejKnowsPlan = true;

        await _store.AppendEventAsync(state.GameTime, intent.ToString(), rawInput, resolution.EventSummary);
        await _store.SaveAsync(state);

        if (resolution.Contract is not null)
            await RenderAsync(resolution.Contract);
    }

    private Resolution Resolve(WorldState state, PlayerIntent intent, string rawInput)
    {
        return intent switch
        {
            PlayerIntent.Look => ResolveLook(state),
            PlayerIntent.GoDownstairs when state.Location == LocationId.InnRoom =>
                Resolution.Ok(2, LocationId.InnCommonRoom, SceneContracts.GoDownstairs(state),
                    "Pavel sešel z pokoje do společenské místnosti hostince."),

            PlayerIntent.GoSquare when state.Location is LocationId.InnRoom or LocationId.InnCommonRoom =>
                Resolution.Ok(5, LocationId.MarketSquare, SceneContracts.GoSquare(state),
                    "Pavel odešel z hostince na rabenfeldské náměstí."),

            PlayerIntent.GoInn when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(5, LocationId.InnCommonRoom, SceneContracts.GoInn(state),
                    "Pavel se vrátil z náměstí do hostince U Tří korun."),

            PlayerIntent.BreakfastBusy when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(5, null, SceneContracts.BreakfastBusy(state),
                    "Matěj přinesl Pavlovi snídani a řekl, že je tržní den a náměstí bývá rušnější."),

            PlayerIntent.AskSecret when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(2, null, SceneContracts.AskSecret(state),
                    "Matěj nepotvrdil údajnou tajnou dohodu; nic spolehlivého o ní neví."),

            PlayerIntent.TellPlan when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(2, null, SceneContracts.TellPlan(state), 
                    "Pavel řekl Matějovi, že chce jít na trh a potom na radnici.", true),

            PlayerIntent.GoodbyeMatej when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.GoodbyeMatej(state),
                    "Pavel se rozloučil s Matějem."),

            PlayerIntent.BargainHalf when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(3, null, SceneContracts.BargainFailure(state),
                    "Pavel neuspěl při pokusu koupit obilí od Havla za polovinu ceny."),

            PlayerIntent.TalkMatej when state.Location == LocationId.InnCommonRoom =>
                Resolution.Ok(1, null, SceneContracts.TalkMatej(state),
                    "Pavel oslovil Matěje."),

            PlayerIntent.TalkHavel when state.Location == LocationId.MarketSquare =>
                Resolution.Ok(1, null, SceneContracts.TalkHavel(state),
                    "Pavel oslovil kupce Havla."),

            _ => Resolution.Reject(SuggestionFor(state.Location, rawInput))
        };
    }

    private static Resolution ResolveLook(WorldState state)
    {
        var contract = state.Location switch
        {
            LocationId.InnRoom => SceneContracts.LookFromWindow(state),
            LocationId.InnCommonRoom => SceneContracts.LookInn(state),
            LocationId.MarketSquare => SceneContracts.LookMarket(state),
            _ => SceneContracts.LookFromWindow(state)
        };
        return Resolution.Ok(3, null, contract, $"Pavel se rozhlédl v lokaci {state.Location}.");
    }

    private static string SuggestionFor(LocationId location, string rawInput)
    {
        var where = LocationNames.Name(location);
        return $"Engine zatím neumí bezpečně vyhodnotit akci „{rawInput}“ v lokaci {where}. " +
               "Zkus ji formulovat jednodušeji, nebo napiš 'pomoc'.";
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
    public static PlayerIntent Parse(string input, LocationId location)
    {
        var n = Normalize(input);

        if (ContainsAny(n, "rozhl", "podivam", "pozoru", "prohled", "prohlid", "koukam", "okna"))
            return PlayerIntent.Look;

        if (ContainsAny(n, "tajnou dohod", "tajne dohod", "dohod rady", "dohodu rady"))
            return PlayerIntent.AskSecret;

        if (ContainsAny(n, "snidan") && ContainsAny(n, "rus", "trzn", "mesto"))
            return PlayerIntent.BreakfastBusy;

        if (ContainsAny(n, "reknu matej", "povim matej", "planuju") &&
            ContainsAny(n, "trh") && ContainsAny(n, "radnic"))
            return PlayerIntent.TellPlan;

        if (ContainsAny(n, "rozlouc", "sbohem", "mej se") && location == LocationId.InnCommonRoom)
            return PlayerIntent.GoodbyeMatej;

        if (ContainsAny(n, "polovin", "pulku", "slev") && ContainsAny(n, "obil", "havel"))
            return PlayerIntent.BargainHalf;

        if (ContainsAny(n, "namesti", "trh", "ven") && ContainsAny(n, "jdu", "pujdu", "vyjdu", "odejdu"))
            return PlayerIntent.GoSquare;

        if (ContainsAny(n, "hostinc", "tri korun", "zpet") && ContainsAny(n, "jdu", "vratim", "pujdu"))
            return PlayerIntent.GoInn;

        if (ContainsAny(n, "dolu", "matej") && ContainsAny(n, "sejdu", "jdu", "pujdu") &&
            location == LocationId.InnRoom)
            return PlayerIntent.GoDownstairs;

        if (ContainsAny(n, "matej") && ContainsAny(n, "mluv", "oslov", "zept", "promluv"))
            return PlayerIntent.TalkMatej;

        if (ContainsAny(n, "havel") && ContainsAny(n, "mluv", "oslov", "zept", "promluv"))
            return PlayerIntent.TalkHavel;

        return PlayerIntent.Unknown;
    }

    private static bool ContainsAny(string text, params string[] fragments) =>
        fragments.Any(text.Contains);

    private static string Normalize(string text)
    {
        var formD = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = formD.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }
}

internal static class SceneContracts
{
    public static SceneContract InnRoomArrival(WorldState s) => new(
        "Pokoj v hostinci U Tří korun",
        "NARRATOR", "PLAYER", "second_person",
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
        "Společenská místnost U Tří korun",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel je ve společenské místnosti hostince U Tří korun.",
            "Hostinský Matěj je přítomen.",
            "Matěj je kolem padesáti let a Pavla zná z několika návštěv."
        ],
        ["Může být zmíněn běžný tlumený ruch hostince."],
        ["Další konkrétní hosté.", "Konkrétní jídla.", "Pověsti nebo zprávy."],
        "Stručně popiš lokaci.");

    public static SceneContract MarketArrival(WorldState s) => new(
        "Rabenfeldské náměstí",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel je na náměstí v Rabenfeldu.",
            "Probíhá běžný tržní den.",
            "Na náměstí jsou stánky, povozy a měšťané.",
            "Kupec Havel zde prodává obilí za 4 groše za míru."
        ],
        ["Může zaznít neurčitý tržní ruch."],
        ["Další pojmenované osoby.", "Konkrétní jiné zboží.", "Konflikty nebo zprávy."],
        "Stručně popiš příchod na náměstí.");

    public static SceneContract LookFromWindow(WorldState s) => new(
        "Pozorování Rabenfeldu z okna",
        "NARRATOR", "PLAYER", "second_person",
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
        "Rozhlédnutí v hostinci",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel je ve společenské místnosti U Tří korun.",
            "Matěj je přítomen."
        ],
        ["Může být zmíněn běžný tlumený ruch místnosti."],
        ["Další konkrétní lidé.", "Konkrétní jídlo nebo pití.", "Nové informace."],
        "Pavel se krátce rozhlédne.");

    public static SceneContract LookMarket(WorldState s) => new(
        "Rozhlédnutí na trhu",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel stojí na rabenfeldském náměstí.",
            "Probíhá běžný tržní den.",
            "Pavel vidí stánky, povozy a měšťany.",
            "Havel prodává obilí za 4 groše za míru."
        ],
        ["Může být zmíněn neurčitý hluk trhu."],
        ["Konkrétní jiné zboží.", "Další pojmenované osoby.", "Události nebo pověsti."],
        "Pavel se tři minuty rozhlíží.");

    public static SceneContract GoDownstairs(WorldState s) => new(
        "Cesta dolů za Matějem",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel opustí pokoj a sejde do společenské místnosti hostince U Tří korun.",
            "Hostinský Matěj je ve společenské místnosti.",
            "Matěj Pavla zná z několika návštěv."
        ],
        ["Přechod může být popsán jednou krátkou větou."],
        ["Další osoby.", "Nové události.", "Konkrétní jídlo nebo pití."],
        "Pavel dorazí do společenské místnosti.");

    public static SceneContract GoSquare(WorldState s) => new(
        "Cesta na náměstí",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel odejde z hostince U Tří korun na rabenfeldské náměstí.",
            "Na náměstí probíhá běžný tržní den.",
            "Havel zde prodává obilí za 4 groše za míru."
        ],
        ["Může být zmíněn neurčitý tržní ruch."],
        ["Další pojmenované osoby.", "Konkrétní jiné zboží.", "Události."],
        "Přesun trvá pět minut.");

    public static SceneContract GoInn(WorldState s) => new(
        "Návrat do hostince",
        "NARRATOR", "PLAYER", "second_person",
        [
            "Pavel odejde z náměstí a vrátí se do společenské místnosti hostince U Tří korun.",
            "Matěj je v hostinci."
        ],
        ["Přesun popiš stručně."],
        ["Další konkrétní osoby.", "Nové události."],
        "Přesun trvá pět minut.");

    public static SceneContract BreakfastBusy(WorldState s) => new(
        "Snídaně s Matějem",
        "Matěj", "Pavel", "dialogue",
        [
            "Matěj přinese Pavlovi snídani.",
            "Konkrétní podoba snídaně není definována.",
            "Pavel se zeptá, zda je dnes ve městě rušno.",
            "Matěj odpoví, že je tržní den a náměstí bývá rušnější.",
            "Matěj nemá žádnou další relevantní zprávu.",
            "Hráč Pavel se v próze oslovuje ve 2. osobě jako ty/tebe/tobě, ne ve 3. osobě."
        ],
        ["Matěj může položit snídani na stůl nebo udělat jedno krátké gesto hostinského."],
        ["Konkrétní jídlo nebo nápoj.", "Další hosté.", "Pověsti.", "Příchod Pavla do hostince."],
        "Rozhovor trvá pět minut.");

    public static SceneContract AskSecret(WorldState s) => new(
        "Otázka na údajnou tajnou dohodu",
        "Matěj", "Pavel", "dialogue",
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
        "Pavel sděluje svůj plán",
        "Pavel", "Matěj", "dialogue",
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
        "Oslovení Matěje",
        "Matěj", "Pavel", "dialogue",
        [
            "Pavel osloví hostinského Matěje.",
            "Matěj Pavla zná z několika návštěv.",
            "Matěj je věcný a přátelský."
        ],
        ["Matěj může Pavla krátce pozdravit a zeptat se, co potřebuje."],
        ["Nové zprávy.", "Pověsti.", "Konkrétní nabídka jídla.", "Tajemství."],
        "Matěj reaguje na oslovení.");

    public static SceneContract TalkHavel(WorldState s) => new(
        "Oslovení Havla",
        "Havel", "Pavel", "dialogue",
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
        "Neúspěšné vyjednávání s Havlem",
        "Havel", "Pavel", "dialogue",
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

            var parsed = JsonSerializer.Deserialize<OllamaResponse>(body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
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
            return "Havel odpoví věcně a drží se své ceny.";
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

internal sealed record Resolution(
    bool Accepted,
    int Minutes,
    LocationId? NewLocation,
    SceneContract? Contract,
    string EventSummary,
    string SystemMessage,
    bool SetMatejKnowsPlan)
{
    public static Resolution Ok(
        int minutes,
        LocationId? location,
        SceneContract contract,
        string summary,
        bool setMatejKnowsPlan = false) =>
        new(true, minutes, location, contract, summary, "", setMatejKnowsPlan);

    public static Resolution Reject(string message) =>
        new(false, 0, null, null, "", message, false);
}

internal enum PlayerIntent
{
    Unknown,
    Look,
    GoDownstairs,
    GoSquare,
    GoInn,
    TalkMatej,
    BreakfastBusy,
    AskSecret,
    TellPlan,
    GoodbyeMatej,
    TalkHavel,
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
