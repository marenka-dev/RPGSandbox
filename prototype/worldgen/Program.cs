using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGSandbox.WorldGen;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var seed = ReadInt(args, "--seed") ?? 14270514;
        var output = ReadString(args, "--out") ?? $"world-{seed}.json";

        var config = WorldConfig.Default(seed);
        var world = WorldGenerator.Generate(config);

        var json = JsonSerializer.Serialize(world, JsonOptions.Pretty);
        File.WriteAllText(output, json);

        PrintSummary(world, output);
    }

    private static void PrintSummary(GeneratedWorld world, string output)
    {
        Console.WriteLine("RPGSandbox – World Generator v0.1");
        Console.WriteLine($"Seed: {world.Config.Seed}");
        Console.WriteLine($"Datum světa: {world.CurrentDate:dd.MM.yyyy}");
        Console.WriteLine();
        Console.WriteLine($"Regiony: {world.Regions.Count}");
        Console.WriteLine($"Sídla: {world.Settlements.Count}");
        Console.WriteLine($"Frakce: {world.Factions.Count}");
        Console.WriteLine($"Rodiny: {world.Families.Count}");
        Console.WriteLine($"Postavy: {world.Characters.Count}");
        Console.WriteLine($"Podniky: {world.Businesses.Count}");
        Console.WriteLine($"Historické události: {world.History.Count}");
        Console.WriteLine();

        var start = world.Start;
        Console.WriteLine("STARTOVNÍ SCÉNA");
        Console.WriteLine($"Lokace: {start.LocationName}");
        Console.WriteLine($"Typ: {start.LocationType}");
        Console.WriteLine($"Region: {start.RegionName}");
        Console.WriteLine($"Počasí: {start.Weather}");
        Console.WriteLine($"Viditelné prvky: {string.Join(", ", start.VisibleFeatures)}");
        Console.WriteLine($"Blízké směry: {string.Join(", ", start.NearbyDestinations)}");
        Console.WriteLine($"Přítomné osoby: {string.Join(", ", start.PresentCharacters)}");
        Console.WriteLine();
        Console.WriteLine("Nabídnutelné akce:");
        foreach (var action in start.ContextActions)
            Console.WriteLine($"- {action}");

        Console.WriteLine();
        Console.WriteLine($"World snapshot uložen: {Path.GetFullPath(output)}");
    }

    private static int? ReadInt(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[i + 1], out var value))
                return value;
        return null;
    }

    private static string? ReadString(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}

internal static class WorldGenerator
{
    public static GeneratedWorld Generate(WorldConfig config)
    {
        var rng = new Random(config.Seed);

        var regions = GenerateRegions(rng);
        var settlements = GenerateSettlements(rng, regions);
        var factions = GenerateFactions(rng, settlements);
        var families = GenerateFamilies(rng, settlements);
        var characters = GenerateCharacters(rng, settlements, families, factions);
        var businesses = GenerateBusinesses(rng, settlements, characters);
        var relationships = GenerateRelationships(rng, characters);
        var history = GenerateHistory(rng, settlements, factions, families);
        var start = GenerateStartScene(rng, regions, settlements, characters);

        return new GeneratedWorld(
            config,
            new DateTime(1427, 5, 14),
            regions,
            settlements,
            factions,
            families,
            characters,
            businesses,
            relationships,
            history,
            start);
    }

    private static List<Region> GenerateRegions(Random rng)
    {
        return
        [
            new(
                "region.rabenfeld",
                "Rabenfeldsko",
                "mírné vnitrozemí",
                ["smíšené lesy", "pole", "nízké kopce"],
                ["dřevo", "obilí", "jíl"],
                18_400),
            new(
                "region.northwoods",
                "Severní hvozdy",
                "chladnější lesnatá oblast",
                ["husté lesy", "potoky", "skalnaté hřbety"],
                ["dřevo", "zvěř", "železná ruda"],
                4_600)
        ];
    }

    private static List<Settlement> GenerateSettlements(Random rng, List<Region> regions)
    {
        return
        [
            new(
                "settlement.rabenfeld",
                "Rabenfeld",
                "town",
                "region.rabenfeld",
                2_840,
                ["tržní náměstí", "radnice", "městská brána"],
                ["road.rabenfeld-linden", "road.rabenfeld-northwoods"]),
            new(
                "settlement.linden",
                "Linden",
                "village",
                "region.rabenfeld",
                430,
                ["náves", "kaple", "mlýn"],
                ["road.rabenfeld-linden"]),
            new(
                "settlement.hvozdec",
                "Hvozdec",
                "hamlet",
                "region.northwoods",
                120,
                ["mýtina", "dřevorubecký dvůr"],
                ["road.rabenfeld-northwoods"])
        ];
    }

    private static List<Faction> GenerateFactions(Random rng, List<Settlement> settlements)
    {
        return
        [
            new(
                "faction.rabenfeld.council",
                "Rabenfeldská městská rada",
                "civic",
                "settlement.rabenfeld",
                68,
                ["správa města", "výběr poplatků", "bezpečnost"]),
            new(
                "faction.merchants.guild",
                "Obchodnický cech Rabenfeldu",
                "guild",
                "settlement.rabenfeld",
                57,
                ["obchod", "tržní privilegia", "vliv na ceny"]),
            new(
                "faction.house.raben",
                "Rod Rabenů",
                "noble_house",
                "settlement.rabenfeld",
                74,
                ["pozemky", "soudní práva", "politický vliv"])
        ];
    }

    private static List<Family> GenerateFamilies(Random rng, List<Settlement> settlements)
    {
        return
        [
            new("family.matej", "Rodina Matěje", "settlement.rabenfeld", "common", ["person.matej", "person.anna"]),
            new("family.havel", "Rodina Havla", "settlement.rabenfeld", "merchant", ["person.havel", "person.katerina"]),
            new("family.raben", "Rod Rabenů", "settlement.rabenfeld", "noble", ["person.heinrich", "person.eliska"]),
            new("family.ondrej", "Rodina Ondřeje", "settlement.rabenfeld", "common", ["person.ondrej"]),
            new("family.miller", "Rodina mlynáře", "settlement.linden", "common", ["person.miller"])
        ];
    }

    private static List<Character> GenerateCharacters(
        Random rng,
        List<Settlement> settlements,
        List<Family> families,
        List<Faction> factions)
    {
        return
        [
            new("person.matej", "Matěj", 52, "hostinský", "family.matej", "settlement.rabenfeld",
                ["faction.merchants.guild"], 41, ["pragmatický", "společenský"]),
            new("person.anna", "Anna", 49, "správkyně domácnosti", "family.matej", "settlement.rabenfeld",
                [], 35, ["pečlivá", "opatrná"]),
            new("person.havel", "Havel", 39, "kupec s obilím", "family.havel", "settlement.rabenfeld",
                ["faction.merchants.guild"], 63, ["věcný", "soutěživý"]),
            new("person.katerina", "Kateřina", 36, "obchodnice", "family.havel", "settlement.rabenfeld",
                ["faction.merchants.guild"], 48, ["pozorná", "vyjednávací"]),
            new("person.heinrich", "Heinrich von Raben", 46, "pán panství", "family.raben", "settlement.rabenfeld",
                ["faction.house.raben"], 88, ["ambiciózní", "rezervovaný"]),
            new("person.eliska", "Eliška z Rabenfeldu", 21, "šlechtična", "family.raben", "settlement.rabenfeld",
                ["faction.house.raben"], 66, ["zvídavá", "sebevědomá"]),
            new("person.ondrej", "Ondřej", 34, "koželuh", "family.ondrej", "settlement.rabenfeld",
                [], 29, ["pracovitý", "nedůvěřivý"]),
            new("person.miller", "Václav", 45, "mlynář", "family.miller", "settlement.linden",
                [], 51, ["praktický", "usedlý"])
        ];
    }

    private static List<Business> GenerateBusinesses(
        Random rng,
        List<Settlement> settlements,
        List<Character> characters)
    {
        return
        [
            new(
                "business.threecrowns",
                "U Tří korun",
                "inn",
                "settlement.rabenfeld",
                "person.matej",
                ["person.anna"],
                ["ubytování", "jídlo", "pití"],
                ["room.common", "room.guest.1", "room.guest.2", "stable"]),
            new(
                "business.havel.grain",
                "Havlův obchod s obilím",
                "merchant_stall",
                "settlement.rabenfeld",
                "person.havel",
                ["person.katerina"],
                ["obilí"],
                ["market.stall.havel"]),
            new(
                "business.linden.mill",
                "Lindenský mlýn",
                "mill",
                "settlement.linden",
                "person.miller",
                [],
                ["mouka", "mletí obilí"],
                ["mill.main", "mill.store"])
        ];
    }

    private static List<Relationship> GenerateRelationships(Random rng, List<Character> characters)
    {
        return
        [
            new("person.matej", "person.anna", 86, 77, 81, 8, 6, 84, 4),
            new("person.havel", "person.matej", 58, 61, 43, 12, 19, 38, 14),
            new("person.heinrich", "person.havel", 47, 64, 28, 31, 13, 44, 22),
            new("person.havel", "person.katerina", 79, 68, 74, 10, 11, 71, 7)
        ];
    }

    private static List<HistoricalEvent> GenerateHistory(
        Random rng,
        List<Settlement> settlements,
        List<Faction> factions,
        List<Family> families)
    {
        return
        [
            new(1382, "Rabenfeld získal rozšířené tržní právo.", ["settlement.rabenfeld"], ["faction.merchants.guild"]),
            new(1397, "Po požáru byla přestavěna část tržiště.", ["settlement.rabenfeld"], []),
            new(1409, "Rod Rabenů financoval opravu severní cesty.", ["settlement.rabenfeld"], ["faction.house.raben"]),
            new(1418, "Obchodnický cech získal dvě místa v městské radě.", ["settlement.rabenfeld"], ["faction.merchants.guild", "faction.rabenfeld.council"]),
            new(1424, "Slabší úroda zvýšila význam dovozu obilí.", ["settlement.rabenfeld", "settlement.linden"], ["faction.merchants.guild"]),
            new(1426, "Městská rada zvýšila dohled nad tržními poplatky.", ["settlement.rabenfeld"], ["faction.rabenfeld.council"])
        ];
    }

    private static StartScene GenerateStartScene(
        Random rng,
        List<Region> regions,
        List<Settlement> settlements,
        List<Character> characters)
    {
        // v0.1 chooses a generated region first and derives the local scene from it.
        var region = regions.First(x => x.Id == "region.northwoods");
        var linkedSettlements = settlements
            .Where(x => x.RegionId == region.Id || x.Connections.Contains("road.rabenfeld-northwoods"))
            .Select(x => x.Name)
            .Distinct()
            .ToList();

        var visible = new List<string>
        {
            region.Terrain.FirstOrDefault() ?? "les",
            "úzká lesní cesta",
            "padlý kmen",
            "mělké stopy v blátě"
        };

        if (region.Resources.Contains("dřevo"))
            visible.Add("stopy po těžbě dřeva v širším okolí");

        var destinations = linkedSettlements
            .Select(name => $"{name} – po lesní cestě")
            .ToList();
        destinations.Add("potok – přibližně 200 metrů na jih");

        var actions = new List<string>
        {
            "Rozhlédnout se po mýtině",
            "Prohlédnout stopy v blátě",
            "Prohlédnout padlý kmen",
            "Jít k potoku"
        };
        actions.AddRange(linkedSettlements.Select(name => $"Vydat se po cestě směrem na {name}"));

        return new(
            "scene.start.forest",
            "lesní mýtina v Severních hvozdech",
            "forest_cell",
            region.Name,
            region.Climate.Contains("chlad") ? "chladné jasné ráno" : "jasné ráno",
            visible,
            destinations,
            [],
            actions);
    }
}

internal sealed record WorldConfig(
    int Seed,
    string Era,
    string Realism,
    string Fantasy,
    string SimulationScope,
    string TechnologyLevel)
{
    public static WorldConfig Default(int seed) =>
        new(seed, "late_medieval", "high", "low", "regional", "medieval");
}

internal sealed record GeneratedWorld(
    WorldConfig Config,
    DateTime CurrentDate,
    List<Region> Regions,
    List<Settlement> Settlements,
    List<Faction> Factions,
    List<Family> Families,
    List<Character> Characters,
    List<Business> Businesses,
    List<Relationship> Relationships,
    List<HistoricalEvent> History,
    StartScene Start);

internal sealed record Region(
    string Id,
    string Name,
    string Climate,
    List<string> Terrain,
    List<string> Resources,
    int Population);

internal sealed record Settlement(
    string Id,
    string Name,
    string Type,
    string RegionId,
    int Population,
    List<string> Landmarks,
    List<string> Connections);

internal sealed record Faction(
    string Id,
    string Name,
    string Type,
    string SeatSettlementId,
    int Influence,
    List<string> Interests);

internal sealed record Family(
    string Id,
    string Name,
    string HomeSettlementId,
    string SocialClass,
    List<string> MemberIds);

internal sealed record Character(
    string Id,
    string Name,
    int Age,
    string Profession,
    string FamilyId,
    string HomeSettlementId,
    List<string> FactionIds,
    int WealthGroats,
    List<string> Traits);

internal sealed record Business(
    string Id,
    string Name,
    string Type,
    string SettlementId,
    string OwnerCharacterId,
    List<string> WorkerCharacterIds,
    List<string> ServicesOrGoods,
    List<string> Spaces);

internal sealed record Relationship(
    string FromCharacterId,
    string ToCharacterId,
    int Trust,
    int Respect,
    int Affection,
    int Fear,
    int Envy,
    int Loyalty,
    int Aversion);

internal sealed record HistoricalEvent(
    int Year,
    string Summary,
    List<string> EntityIds,
    List<string> FactionIds);

internal sealed record StartScene(
    string Id,
    string LocationName,
    string LocationType,
    string RegionName,
    string Weather,
    List<string> VisibleFeatures,
    List<string> NearbyDestinations,
    List<string> PresentCharacters,
    List<string> ContextActions);

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
