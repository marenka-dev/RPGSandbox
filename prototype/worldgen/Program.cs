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

        var config = WorldConfig.Default(seed) with
        {
            ClimateBias = ReadString(args, "--climate") ?? "mixed",
            PoliticalStability = ReadString(args, "--politics") ?? "medium",
            EconomicDepth = ReadString(args, "--economy") ?? "deep"
        };

        var world = WorldGenerator.Generate(config);
        var validation = WorldValidator.Validate(world);

        if (validation.Count > 0)
        {
            Console.WriteLine("WORLDGEN VALIDATION FAILED:");
            foreach (var error in validation)
                Console.WriteLine("- " + error);
            Environment.ExitCode = 2;
            return;
        }

        File.WriteAllText(output, JsonSerializer.Serialize(world, JsonOptions.Pretty));
        PrintSummary(world, output);
    }

    private static void PrintSummary(GeneratedWorld world, string output)
    {
        Console.WriteLine("RPGSandbox – World Generator v0.2 Procedural Region");
        Console.WriteLine($"Seed: {world.Config.Seed}");
        Console.WriteLine($"Éra: {world.Config.Era}");
        Console.WriteLine($"Climate bias: {world.Config.ClimateBias}");
        Console.WriteLine($"Politická stabilita: {world.Config.PoliticalStability}");
        Console.WriteLine($"Ekonomika: {world.Config.EconomicDepth}");
        Console.WriteLine($"Datum světa: {world.CurrentDate:dd.MM.yyyy}");
        Console.WriteLine();

        foreach (var region in world.Regions)
        {
            Console.WriteLine($"REGION: {region.Name}");
            Console.WriteLine($"  Klima: {region.Climate}");
            Console.WriteLine($"  Terén: {string.Join(", ", region.Terrain)}");
            Console.WriteLine($"  Zdroje: {string.Join(", ", region.Resources)}");
            Console.WriteLine($"  Úrodnost: {region.Fertility}/100");
            Console.WriteLine($"  Lesnatost: {region.ForestCover}/100");
            Console.WriteLine($"  Voda: {region.WaterAvailability}/100");
            Console.WriteLine($"  Obchodní průchodnost: {region.TradeAccess}/100");
            Console.WriteLine($"  Populace: {region.Population}");
        }

        Console.WriteLine();
        Console.WriteLine("SÍDLA:");
        foreach (var settlement in world.Settlements.OrderByDescending(x => x.Population))
        {
            Console.WriteLine(
                $"- {settlement.Name} ({settlement.Type}), pop. {settlement.Population}, " +
                $"specializace: {string.Join(", ", settlement.EconomicSpecializations)}");
        }

        Console.WriteLine();
        Console.WriteLine("PODNIKY:");
        foreach (var business in world.Businesses)
            Console.WriteLine($"- {business.Name} / {business.Type} / {business.SettlementId}");

        Console.WriteLine();
        Console.WriteLine($"Frakce: {world.Factions.Count}");
        Console.WriteLine($"Rodiny: {world.Families.Count}");
        Console.WriteLine($"Postavy: {world.Characters.Count}");
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
    private static readonly string[] RegionPrefixes =
        ["Raben", "Linden", "Eichen", "Falken", "Silber", "Morgen", "Stein", "Wald", "Hohen", "Grün"];

    private static readonly string[] RegionSuffixes =
        ["mark", "land", "wald", "feld", "gau", "hain", "tal", "bruch"];

    private static readonly string[] SettlementRoots =
        ["Raben", "Linden", "Hohen", "Stein", "Eichen", "Falken", "Grün", "Silber", "Mühlen", "Dorn", "Berg", "Wald"];

    private static readonly string[] SettlementSuffixes =
        ["feld", "dorf", "hain", "bruck", "berg", "tal", "rode", "heim", "furt", "stadt"];

    public static GeneratedWorld Generate(WorldConfig config)
    {
        var rng = new Random(config.Seed);

        var regions = GenerateRegions(rng, config);
        var settlements = GenerateSettlements(rng, config, regions);
        var businesses = GenerateBusinesses(rng, config, regions, settlements);
        var families = GenerateFamilies(rng, settlements, businesses);
        var characters = GenerateCharacters(rng, families, settlements, businesses);
        businesses = AssignBusinessOwners(rng, businesses, characters);
        var factions = GenerateFactions(rng, config, settlements, characters);
        characters = AssignFactionMemberships(rng, characters, factions);
        var relationships = GenerateRelationships(rng, characters, families);
        var history = GenerateHistory(rng, config, regions, settlements, factions, businesses);
        var start = GenerateStartScene(rng, regions, settlements);

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

    private static List<Region> GenerateRegions(Random rng, WorldConfig config)
    {
        var count = config.SimulationScope == "regional" ? 1 : rng.Next(1, 3);
        var regions = new List<Region>();

        for (var i = 0; i < count; i++)
        {
            var moisture = rng.Next(25, 91);
            var temperature = rng.Next(25, 76);
            var elevation = rng.Next(10, 81);

            ApplyClimateBias(config.ClimateBias, ref moisture, ref temperature, ref elevation);
            moisture = Math.Clamp(moisture, 0, 100);
            temperature = Math.Clamp(temperature, 0, 100);
            elevation = Math.Clamp(elevation, 0, 100);

            var climate = ClimateFrom(temperature, moisture);
            var fertility = Math.Clamp((moisture + (100 - Math.Abs(temperature - 55))) / 2 - elevation / 5, 10, 95);
            var forest = Math.Clamp(moisture + elevation / 3 - fertility / 4 + rng.Next(-10, 11), 5, 95);
            var water = Math.Clamp(moisture + rng.Next(-15, 16), 5, 100);
            var trade = Math.Clamp(70 - elevation / 2 + water / 4 + rng.Next(-15, 16), 10, 95);
            var area = rng.Next(650, 1801);
            var carryingCapacity = Math.Max(2500, area * (fertility + water / 2 + trade / 3) / 18);
            var population = Math.Max(1800, carryingCapacity + rng.Next(-carryingCapacity / 8, carryingCapacity / 8 + 1));

            var terrain = GenerateTerrain(elevation, forest, water);
            var resources = GenerateResources(rng, fertility, forest, elevation, water);

            regions.Add(new Region(
                $"region.{i + 1}",
                GenerateRegionName(rng),
                climate,
                terrain,
                resources,
                population,
                fertility,
                forest,
                water,
                trade,
                area));
        }

        return regions;
    }

    private static List<Settlement> GenerateSettlements(Random rng, WorldConfig config, List<Region> regions)
    {
        var settlements = new List<Settlement>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var id = 1;

        foreach (var region in regions)
        {
            var targetUrbanShare = 0.16 + region.TradeAccess / 500.0;
            var settlementCount = Math.Clamp(region.Population / 2800, 3, 12);

            for (var i = 0; i < settlementCount; i++)
            {
                var isPrimary = i == 0;
                var type = isPrimary
                    ? region.Population > 16000 ? "town" : "market_town"
                    : PickSettlementType(rng, region);

                var population = PopulationForSettlement(rng, type, region, targetUrbanShare);
                var name = GenerateUniqueSettlementName(rng, usedNames);
                var specializations = DeriveSettlementSpecializations(region, type);
                var landmarks = DeriveLandmarks(type, specializations);
                var connections = new List<string>();

                settlements.Add(new Settlement(
                    $"settlement.{id++}",
                    name,
                    type,
                    region.Id,
                    population,
                    landmarks,
                    connections,
                    specializations));
            }

            var local = settlements.Where(x => x.RegionId == region.Id).ToList();
            for (var i = 0; i < local.Count - 1; i++)
            {
                var roadId = $"road.{local[i].Id}.{local[i + 1].Id}";
                local[i].Connections.Add(roadId);
                local[i + 1].Connections.Add(roadId);
            }
        }

        return settlements;
    }

    private static List<Business> GenerateBusinesses(
        Random rng,
        WorldConfig config,
        List<Region> regions,
        List<Settlement> settlements)
    {
        var result = new List<Business>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var region = regions.Single(x => x.Id == settlement.RegionId);

            void Add(string type, string goods, int count)
            {
                for (var n = 0; n < count; n++)
                {
                    result.Add(new Business(
                        $"business.{id++}",
                        GenerateBusinessName(rng, settlement.Name, type),
                        type,
                        settlement.Id,
                        "",
                        [],
                        goods.Split('|').ToList(),
                        []));
                }
            }

            if (region.Resources.Contains("obilí"))
            {
                Add("farm", "obilí", Math.Max(1, settlement.Population / 350));
                if (settlement.Population >= 250 && region.WaterAvailability >= 35)
                    Add("mill", "mouka|mletí obilí", Math.Max(1, settlement.Population / 1800));
                if (settlement.Population >= 500)
                    Add("bakery", "chléb|pečivo", Math.Max(1, settlement.Population / 1600));
            }

            if (region.Resources.Contains("dřevo"))
            {
                Add("woodcutter", "dřevo", Math.Max(1, settlement.Population / 900));
                if (settlement.Population >= 350)
                    Add("carpenter", "nábytek|tesařské práce", Math.Max(1, settlement.Population / 1400));
            }

            if (region.Resources.Contains("železná ruda") && settlement.Population >= 300)
                Add("smithy", "kovářské výrobky|nástroje", Math.Max(1, settlement.Population / 1700));

            if (settlement.Population >= 250)
                Add("inn", "ubytování|jídlo|pití", Math.Max(1, settlement.Population / 2200));

            if (settlement.Type is "town" or "market_town")
            {
                Add("merchant", "dovoz|vývoz", Math.Max(1, settlement.Population / 2000));
                Add("market_stall", "místní zboží", Math.Max(1, settlement.Population / 1200));
            }
        }

        return result;
    }

    private static List<Family> GenerateFamilies(Random rng, List<Settlement> settlements, List<Business> businesses)
    {
        var result = new List<Family>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var householdCount = Math.Clamp(settlement.Population / 5, 8, 90);
            var importantHouseholds = Math.Clamp(householdCount / 8, 3, 12);

            for (var i = 0; i < importantHouseholds; i++)
            {
                var socialClass = i == 0 && settlement.Type is "town" or "market_town"
                    ? "elite"
                    : rng.NextDouble() < 0.22 ? "merchant" : "common";

                result.Add(new Family(
                    $"family.{id++}",
                    $"Rodina {GenerateSurname(rng)}",
                    settlement.Id,
                    socialClass,
                    []));
            }
        }

        return result;
    }

    private static List<Character> GenerateCharacters(
        Random rng,
        List<Family> families,
        List<Settlement> settlements,
        List<Business> businesses)
    {
        var result = new List<Character>();
        var id = 1;

        foreach (var family in families)
        {
            var settlement = settlements.Single(x => x.Id == family.HomeSettlementId);
            var count = rng.Next(2, 6);

            for (var i = 0; i < count; i++)
            {
                var age = i == 0 ? rng.Next(30, 61) : rng.Next(16, 58);
                var profession = PickProfession(rng, settlement, businesses);
                var name = GenerateGivenName(rng);

                var character = new Character(
                    $"person.{id++}",
                    name,
                    age,
                    profession,
                    family.Id,
                    settlement.Id,
                    [],
                    WealthFor(family.SocialClass, rng),
                    GenerateTraits(rng));

                result.Add(character);
                family.MemberIds.Add(character.Id);
            }
        }

        return result;
    }

    private static List<Business> AssignBusinessOwners(Random rng, List<Business> businesses, List<Character> characters)
    {
        return businesses.Select(b =>
        {
            var candidates = characters.Where(x => x.HomeSettlementId == b.SettlementId && x.Age >= 18).ToList();
            var owner = candidates.Count == 0 ? characters.First() : candidates[rng.Next(candidates.Count)];
            var workers = candidates
                .Where(x => x.Id != owner.Id)
                .OrderBy(_ => rng.Next())
                .Take(Math.Min(candidates.Count - 1, WorkerCountFor(b.Type, rng)))
                .Select(x => x.Id)
                .ToList();

            return b with { OwnerCharacterId = owner.Id, WorkerCharacterIds = workers };
        }).ToList();
    }

    private static List<Faction> GenerateFactions(
        Random rng,
        WorldConfig config,
        List<Settlement> settlements,
        List<Character> characters)
    {
        var result = new List<Faction>();
        var id = 1;

        foreach (var settlement in settlements.Where(x => x.Type is "town" or "market_town"))
        {
            result.Add(new Faction(
                $"faction.{id++}",
                $"{settlement.Name}ská rada",
                "civic",
                settlement.Id,
                rng.Next(45, 86),
                ["správa", "bezpečnost", "poplatky"]));

            if (settlement.Population >= 1200)
                result.Add(new Faction(
                    $"faction.{id++}",
                    $"Obchodnický cech {settlement.Name}",
                    "guild",
                    settlement.Id,
                    rng.Next(35, 76),
                    ["obchod", "tržní privilegia"]));

            if (config.PoliticalStability == "low" && rng.NextDouble() < 0.6)
                result.Add(new Faction(
                    $"faction.{id++}",
                    $"Městská opozice {settlement.Name}",
                    "political_group",
                    settlement.Id,
                    rng.Next(20, 55),
                    ["omezení moci rady", "nižší poplatky"]));
        }

        return result;
    }

    private static List<Character> AssignFactionMemberships(Random rng, List<Character> characters, List<Faction> factions)
    {
        return characters.Select(c =>
        {
            var local = factions.Where(x => x.SeatSettlementId == c.HomeSettlementId).ToList();
            var memberships = local
                .Where(f => f.Type == "civic"
                    ? c.Profession.Contains("správ") || c.WealthGroats > 70
                    : f.Type == "guild"
                        ? c.Profession.Contains("kupec") || c.Profession.Contains("hostinsk") || c.WealthGroats > 50
                        : rng.NextDouble() < 0.12)
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            return c with { FactionIds = memberships };
        }).ToList();
    }

    private static List<Relationship> GenerateRelationships(Random rng, List<Character> characters, List<Family> families)
    {
        var result = new List<Relationship>();

        foreach (var family in families)
        {
            var members = characters.Where(x => x.FamilyId == family.Id).ToList();
            for (var i = 0; i < members.Count; i++)
            for (var j = 0; j < members.Count; j++)
            {
                if (i == j) continue;
                result.Add(new Relationship(
                    members[i].Id,
                    members[j].Id,
                    rng.Next(55, 91),
                    rng.Next(45, 86),
                    rng.Next(50, 91),
                    rng.Next(0, 21),
                    rng.Next(0, 21),
                    rng.Next(45, 91),
                    rng.Next(0, 16)));
            }
        }

        foreach (var settlementGroup in characters.GroupBy(x => x.HomeSettlementId))
        {
            var people = settlementGroup.ToList();
            var pairs = Math.Min(people.Count * 2, 16);
            for (var i = 0; i < pairs && people.Count >= 2; i++)
            {
                var a = people[rng.Next(people.Count)];
                var b = people[rng.Next(people.Count)];
                if (a.Id == b.Id) continue;

                result.Add(new Relationship(
                    a.Id, b.Id,
                    rng.Next(20, 81),
                    rng.Next(20, 81),
                    rng.Next(10, 71),
                    rng.Next(0, 41),
                    rng.Next(0, 41),
                    rng.Next(10, 71),
                    rng.Next(0, 51)));
            }
        }

        return result;
    }

    private static List<HistoricalEvent> GenerateHistory(
        Random rng,
        WorldConfig config,
        List<Region> regions,
        List<Settlement> settlements,
        List<Faction> factions,
        List<Business> businesses)
    {
        var events = new List<HistoricalEvent>();
        var startYear = 1427 - config.HistoryYears;

        foreach (var settlement in settlements)
        {
            events.Add(new HistoricalEvent(
                rng.Next(startYear, 1400),
                $"{settlement.Name} se v regionu ustálilo jako {SettlementRole(settlement)}.",
                [settlement.Id],
                []));

            if (settlement.EconomicSpecializations.Contains("obilí") && rng.NextDouble() < 0.7)
                events.Add(new HistoricalEvent(
                    rng.Next(1400, 1426),
                    $"Silná úroda posílila obchod s obilím v okolí {settlement.Name}.",
                    [settlement.Id],
                    []));
        }

        foreach (var faction in factions)
            events.Add(new HistoricalEvent(
                rng.Next(1390, 1427),
                $"{faction.Name} získala výraznější vliv v místních záležitostech.",
                [faction.SeatSettlementId],
                [faction.Id]));

        if (config.PoliticalStability == "low" && factions.Count > 1)
        {
            var a = factions[0];
            var b = factions[1];
            events.Add(new HistoricalEvent(
                rng.Next(1415, 1427),
                $"Napětí mezi {a.Name} a {b.Name} přerostlo v otevřený politický spor.",
                [a.SeatSettlementId],
                [a.Id, b.Id]));
        }

        return events.OrderBy(x => x.Year).ToList();
    }

    private static StartScene GenerateStartScene(Random rng, List<Region> regions, List<Settlement> settlements)
    {
        var region = regions[rng.Next(regions.Count)];
        var localSettlements = settlements.Where(x => x.RegionId == region.Id).ToList();
        var nearest = localSettlements.OrderBy(_ => rng.Next()).Take(Math.Min(2, localSettlements.Count)).ToList();

        var dominantTerrain = region.Terrain[rng.Next(region.Terrain.Count)];
        var features = new List<string> { dominantTerrain };

        if (region.ForestCover >= 45)
        {
            features.Add("lesní cesta");
            features.Add("padlý kmen");
        }
        else
        {
            features.Add("polní cesta");
            features.Add("mez mezi poli");
        }

        if (region.WaterAvailability >= 55)
            features.Add("voda slyšitelná nebo viditelná v okolí");

        if (region.Resources.Contains("dřevo") && region.ForestCover >= 55)
            features.Add("stopy po těžbě dřeva");

        if (region.Resources.Contains("kámen") && rng.NextDouble() < 0.5)
            features.Add("skalní výchoz");

        var destinations = nearest.Select(x => $"{x.Name} – po místní cestě").ToList();
        var actions = new List<string> { "Rozhlédnout se po okolí" };

        foreach (var feature in features.Skip(1))
            actions.Add($"Prohlédnout: {feature}");

        actions.AddRange(nearest.Select(x => $"Vydat se směrem na {x.Name}"));

        var sceneType = region.ForestCover >= 45 ? "forest_cell" : "rural_cell";
        var locationName = region.ForestCover >= 45
            ? $"lesní místo v regionu {region.Name}"
            : $"otevřená krajina v regionu {region.Name}";

        return new StartScene(
            "scene.start",
            locationName,
            sceneType,
            region.Name,
            WeatherFor(region, rng),
            features,
            destinations,
            [],
            actions);
    }

    private static void ApplyClimateBias(string bias, ref int moisture, ref int temperature, ref int elevation)
    {
        switch (bias.ToLowerInvariant())
        {
            case "dry":
                moisture -= 25;
                temperature += 10;
                break;
            case "wet":
                moisture += 20;
                break;
            case "cold":
                temperature -= 25;
                elevation += 10;
                break;
            case "warm":
                temperature += 20;
                break;
            case "mountain":
                elevation += 30;
                temperature -= 10;
                break;
        }
    }

    private static string ClimateFrom(int temperature, int moisture)
    {
        if (temperature < 35) return moisture > 55 ? "chladné vlhké" : "chladné suché";
        if (temperature > 68) return moisture > 55 ? "teplé vlhké" : "teplé suché";
        return moisture > 65 ? "mírné vlhké" : moisture < 35 ? "mírné suché" : "mírné";
    }

    private static List<string> GenerateTerrain(int elevation, int forest, int water)
    {
        var result = new List<string>();
        if (elevation >= 65) result.Add("vrchovina");
        else if (elevation >= 40) result.Add("zvlněná krajina");
        else result.Add("nížina");

        if (forest >= 55) result.Add("rozsáhlé lesy");
        else if (forest >= 30) result.Add("mozaika polí a lesů");
        else result.Add("otevřená zemědělská krajina");

        if (water >= 65) result.Add("řeky a potoky");
        else if (water >= 40) result.Add("menší vodní toky");

        return result;
    }

    private static List<string> GenerateResources(Random rng, int fertility, int forest, int elevation, int water)
    {
        var result = new List<string>();

        if (fertility >= 48) result.Add("obilí");
        if (forest >= 42) result.Add("dřevo");
        if (forest >= 58) result.Add("zvěř");
        if (elevation >= 55 && rng.NextDouble() < 0.65) result.Add("železná ruda");
        if (elevation >= 40) result.Add("kámen");
        if (water >= 55) result.Add("ryby");
        if (result.Count == 0) result.Add("pastviny");

        return result;
    }

    private static string PickSettlementType(Random rng, Region region)
    {
        var roll = rng.NextDouble();
        if (region.TradeAccess >= 65 && roll < 0.20) return "market_town";
        if (roll < 0.60) return "village";
        return "hamlet";
    }

    private static int PopulationForSettlement(Random rng, string type, Region region, double urbanShare)
    {
        return type switch
        {
            "town" => rng.Next(1800, Math.Max(1801, Math.Min(5200, region.Population / 3))),
            "market_town" => rng.Next(850, Math.Max(851, Math.Min(2600, region.Population / 4))),
            "village" => rng.Next(180, 750),
            _ => rng.Next(60, 240)
        };
    }

    private static List<string> DeriveSettlementSpecializations(Region region, string type)
    {
        var result = new List<string>();

        if (region.Resources.Contains("obilí")) result.Add("obilí");
        if (region.Resources.Contains("dřevo")) result.Add("dřevo");
        if (region.Resources.Contains("železná ruda")) result.Add("kovářství");
        if (region.Resources.Contains("ryby")) result.Add("rybolov");
        if (type is "town" or "market_town") result.Add("obchod");

        return result;
    }

    private static List<string> DeriveLandmarks(string type, List<string> specializations)
    {
        var result = new List<string>();

        if (type is "town" or "market_town")
        {
            result.Add("tržiště");
            result.Add("správní budova");
        }
        else
        {
            result.Add("náves");
        }

        if (specializations.Contains("obilí")) result.Add("sýpky");
        if (specializations.Contains("dřevo")) result.Add("dřevní sklad");
        if (specializations.Contains("kovářství")) result.Add("kovárna");

        return result;
    }

    private static string PickProfession(Random rng, Settlement settlement, List<Business> businesses)
    {
        var localTypes = businesses.Where(x => x.SettlementId == settlement.Id).Select(x => x.Type).ToList();
        var professions = new List<string> { "zemědělec", "dělník", "správce domácnosti" };

        if (localTypes.Contains("mill")) professions.Add("mlynář");
        if (localTypes.Contains("bakery")) professions.Add("pekař");
        if (localTypes.Contains("inn")) professions.Add("hostinský");
        if (localTypes.Contains("merchant")) professions.Add("kupec");
        if (localTypes.Contains("smithy")) professions.Add("kovář");
        if (localTypes.Contains("woodcutter")) professions.Add("dřevorubec");
        if (localTypes.Contains("carpenter")) professions.Add("tesař");

        return professions[rng.Next(professions.Count)];
    }

    private static int WealthFor(string socialClass, Random rng) => socialClass switch
    {
        "elite" => rng.Next(75, 151),
        "merchant" => rng.Next(45, 101),
        _ => rng.Next(10, 61)
    };

    private static List<string> GenerateTraits(Random rng)
    {
        string[] pool =
            ["praktický", "společenský", "opatrný", "ambiciózní", "pracovitý", "nedůvěřivý", "štědrý", "tvrdohlavý"];
        return pool.OrderBy(_ => rng.Next()).Take(2).ToList();
    }

    private static int WorkerCountFor(string type, Random rng) => type switch
    {
        "farm" => rng.Next(1, 4),
        "inn" => rng.Next(1, 4),
        "mill" => rng.Next(0, 3),
        "merchant" => rng.Next(0, 3),
        _ => rng.Next(0, 2)
    };

    private static string GenerateRegionName(Random rng) =>
        RegionPrefixes[rng.Next(RegionPrefixes.Length)] + RegionSuffixes[rng.Next(RegionSuffixes.Length)];

    private static string GenerateUniqueSettlementName(Random rng, HashSet<string> used)
    {
        string name;
        do
            name = SettlementRoots[rng.Next(SettlementRoots.Length)] + SettlementSuffixes[rng.Next(SettlementSuffixes.Length)];
        while (!used.Add(name));
        return name;
    }

    private static string GenerateBusinessName(Random rng, string settlementName, string type) => type switch
    {
        "inn" => $"Hostinec U {Pick(rng, "Jelena", "Tří lip", "Černého koně", "Staré brány")}",
        "mill" => $"{settlementName}ský mlýn",
        "bakery" => $"Pekárna {GenerateSurname(rng)}",
        "smithy" => $"Kovárna {GenerateSurname(rng)}",
        "carpenter" => $"Tesařství {GenerateSurname(rng)}",
        "woodcutter" => $"Dřevorubecký dvůr {GenerateSurname(rng)}",
        "merchant" => $"Obchod {GenerateSurname(rng)}",
        "market_stall" => $"Tržní stánek {GenerateSurname(rng)}",
        "farm" => $"Hospodářství {GenerateSurname(rng)}",
        _ => $"{type} {GenerateSurname(rng)}"
    };

    private static string GenerateSurname(Random rng) =>
        Pick(rng, "Novák", "Beran", "Kovář", "Mlynář", "Růžička", "Vlk", "Holub", "Černý", "Jelen");

    private static string GenerateGivenName(Random rng) =>
        Pick(rng, "Matěj", "Jan", "Ondřej", "Václav", "Anna", "Kateřina", "Eliška", "Markéta", "Jakub", "Petr");

    private static string SettlementRole(Settlement s) =>
        s.Type is "town" or "market_town" ? "regionální obchodní centrum" : "stabilní zemědělské sídlo";

    private static string WeatherFor(Region region, Random rng)
    {
        if (region.Climate.Contains("chlad")) return rng.NextDouble() < 0.5 ? "chladné jasno" : "chladno a zataženo";
        if (region.Climate.Contains("vlhké")) return rng.NextDouble() < 0.5 ? "vlhké ráno" : "přeháňky";
        if (region.Climate.Contains("teplé")) return "teplé ráno";
        return rng.NextDouble() < 0.5 ? "jasné ráno" : "proměnlivá oblačnost";
    }

    private static string Pick(Random rng, params string[] values) => values[rng.Next(values.Length)];
}

internal sealed record WorldConfig(
    int Seed,
    string Era,
    string Realism,
    string Fantasy,
    string SimulationScope,
    string TechnologyLevel,
    string ClimateBias,
    string PoliticalStability,
    string EconomicDepth,
    int HistoryYears)
{
    public static WorldConfig Default(int seed) =>
        new(
            seed,
            "late_medieval",
            "high",
            "low",
            "regional",
            "medieval",
            "mixed",
            "medium",
            "deep",
            80);
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
    int Population,
    int Fertility,
    int ForestCover,
    int WaterAvailability,
    int TradeAccess,
    int AreaKm2);

internal sealed record Settlement(
    string Id,
    string Name,
    string Type,
    string RegionId,
    int Population,
    List<string> Landmarks,
    List<string> Connections,
    List<string> EconomicSpecializations);

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

internal static class WorldValidator
{
    public static List<string> Validate(GeneratedWorld world)
    {
        var errors = new List<string>();

        var settlementIds = world.Settlements.Select(x => x.Id).ToHashSet();
        var regionIds = world.Regions.Select(x => x.Id).ToHashSet();
        var factionIds = world.Factions.Select(x => x.Id).ToHashSet();
        var familyIds = world.Families.Select(x => x.Id).ToHashSet();
        var characterIds = world.Characters.Select(x => x.Id).ToHashSet();

        if (world.Regions.Count == 0)
            errors.Add("World has no regions.");

        if (world.Settlements.Count == 0)
            errors.Add("World has no settlements.");

        foreach (var settlement in world.Settlements)
        {
            if (!regionIds.Contains(settlement.RegionId))
                errors.Add($"Settlement {settlement.Id} references missing region {settlement.RegionId}.");
            if (settlement.Population <= 0)
                errors.Add($"Settlement {settlement.Id} has invalid population.");
        }

        foreach (var family in world.Families)
        {
            if (!settlementIds.Contains(family.HomeSettlementId))
                errors.Add($"Family {family.Id} references missing settlement {family.HomeSettlementId}.");

            foreach (var member in family.MemberIds)
                if (!characterIds.Contains(member))
                    errors.Add($"Family {family.Id} references missing character {member}.");
        }

        foreach (var character in world.Characters)
        {
            if (!familyIds.Contains(character.FamilyId))
                errors.Add($"Character {character.Id} references missing family {character.FamilyId}.");
            if (!settlementIds.Contains(character.HomeSettlementId))
                errors.Add($"Character {character.Id} references missing settlement {character.HomeSettlementId}.");
            foreach (var faction in character.FactionIds)
                if (!factionIds.Contains(faction))
                    errors.Add($"Character {character.Id} references missing faction {faction}.");
        }

        foreach (var business in world.Businesses)
        {
            if (!settlementIds.Contains(business.SettlementId))
                errors.Add($"Business {business.Id} references missing settlement {business.SettlementId}.");
            if (!characterIds.Contains(business.OwnerCharacterId))
                errors.Add($"Business {business.Id} references missing owner {business.OwnerCharacterId}.");
            foreach (var worker in business.WorkerCharacterIds)
                if (!characterIds.Contains(worker))
                    errors.Add($"Business {business.Id} references missing worker {worker}.");
        }

        foreach (var relation in world.Relationships)
        {
            if (!characterIds.Contains(relation.FromCharacterId))
                errors.Add($"Relationship references missing source {relation.FromCharacterId}.");
            if (!characterIds.Contains(relation.ToCharacterId))
                errors.Add($"Relationship references missing target {relation.ToCharacterId}.");
        }

        return errors;
    }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
