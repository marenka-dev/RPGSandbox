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
        Console.WriteLine("RPGSandbox – World Generator v0.3 Spatial Region");
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
            Console.WriteLine($"  Mapa: {region.Width} x {region.Height} buněk");
            Console.WriteLine($"  Klima: {region.Climate}");
            Console.WriteLine($"  Úrodnost průměr: {region.Fertility}/100");
            Console.WriteLine($"  Lesnatost průměr: {region.ForestCover}/100");
            Console.WriteLine($"  Voda průměr: {region.WaterAvailability}/100");
            Console.WriteLine($"  Obchodní průchodnost: {region.TradeAccess}/100");
            Console.WriteLine($"  Populace celkem: {region.Population}");
            Console.WriteLine($"    v hlavních sídlech: {region.PopulationBreakdown.Settled}");
            Console.WriteLine($"    rozptýlené venkovské domácnosti: {region.PopulationBreakdown.RuralHouseholds}");
            Console.WriteLine($"    malé osady: {region.PopulationBreakdown.SmallHamlets}");
            Console.WriteLine($"    samoty a izolovaná místa: {region.PopulationBreakdown.Isolated}");
            Console.WriteLine($"  Říční buňky: {region.Cells.Count(x => x.HasRiver)}");
        }

        Console.WriteLine();
        Console.WriteLine("SÍDLA:");
        foreach (var settlement in world.Settlements.OrderByDescending(x => x.Population))
        {
            Console.WriteLine(
                $"- {settlement.Name} ({settlement.Type}) [{settlement.X},{settlement.Y}], pop. {settlement.Population}");
            Console.WriteLine($"    místní zdroje: {string.Join(", ", settlement.LocalResources)}");
            Console.WriteLine($"    specializace: {string.Join(", ", settlement.EconomicSpecializations)}");
        }

        Console.WriteLine();
        Console.WriteLine("CESTY:");
        foreach (var road in world.Roads)
        {
            var from = world.Settlements.Single(x => x.Id == road.FromSettlementId).Name;
            var to = world.Settlements.Single(x => x.Id == road.ToSettlementId).Name;
            Console.WriteLine($"- {from} ↔ {to}, délka {road.LengthCells} buněk");
        }

        Console.WriteLine();
        Console.WriteLine("PODNIKY:");
        foreach (var business in world.Businesses)
        {
            var settlement = world.Settlements.Single(x => x.Id == business.SettlementId);
            Console.WriteLine($"- {business.Name} / {business.Type} / {settlement.Name}");
        }

        Console.WriteLine();
        Console.WriteLine($"Frakce: {world.Factions.Count}");
        Console.WriteLine($"Rodiny: {world.Families.Count}");
        Console.WriteLine($"Postavy: {world.Characters.Count}");
        Console.WriteLine($"Historické události: {world.History.Count}");

        Console.WriteLine();
        var start = world.Start;
        Console.WriteLine("STARTOVNÍ SCÉNA");
        Console.WriteLine($"Lokace: {start.LocationName} [{start.X},{start.Y}]");
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
        var geographyRng = RngStreams.For(config.Seed, "geography");
        var settlementRng = RngStreams.For(config.Seed, "settlements");
        var economyRng = RngStreams.For(config.Seed, "economy");
        var populationRng = RngStreams.For(config.Seed, "population");
        var politicsRng = RngStreams.For(config.Seed, "politics");
        var historyRng = RngStreams.For(config.Seed, "history");
        var sceneRng = RngStreams.For(config.Seed, "scene");

        var regions = GenerateRegions(geographyRng, config);
        var settlements = GenerateSettlements(settlementRng, regions);
        var roads = GenerateRoads(settlementRng, settlements);
        ApplyRoadsToSettlements(settlements, roads);

        var businesses = GenerateBusinesses(economyRng, regions, settlements);
        var families = GenerateFamilies(populationRng, settlements);
        var characters = GenerateCharacters(populationRng, families, settlements, businesses);
        businesses = AssignBusinessOwners(populationRng, businesses, characters);

        var factions = GenerateFactions(politicsRng, config, settlements);
        characters = AssignFactionMemberships(politicsRng, characters, factions);
        var relationships = GenerateRelationships(populationRng, characters, families);
        var history = GenerateHistory(historyRng, config, settlements, factions);
        var start = GenerateStartScene(sceneRng, regions, settlements, roads);

        return new GeneratedWorld(
            config,
            new DateTime(1427, 5, 14),
            regions,
            settlements,
            roads,
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
        const int width = 24;
        const int height = 18;

        var moistureBase = rng.Next(25, 91);
        var temperatureBase = rng.Next(25, 76);
        var elevationBase = rng.Next(10, 81);

        ApplyClimateBias(config.ClimateBias, ref moistureBase, ref temperatureBase, ref elevationBase);

        var riverX = rng.Next(3, width - 3);
        var riverDrift = rng.NextDouble() < 0.5 ? -1 : 1;
        var cells = new List<MapCell>(width * height);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var radial = Distance01(x, y, width / 2.0, height / 2.0, width, height);
            var ridge = (int)(18 * Math.Sin((x + rng.NextDouble()) / 4.3) + 12 * Math.Cos((y + 2) / 3.7));
            var elevation = Math.Clamp(elevationBase + ridge + (int)(radial * 16) + rng.Next(-7, 8), 0, 100);

            var riverCenter = riverX + (int)Math.Round(Math.Sin(y / 3.0) * 2.0) + (y / 7) * riverDrift;
            var riverDistance = Math.Abs(x - riverCenter);
            var hasRiver = riverDistance == 0;

            var moisture = Math.Clamp(
                moistureBase + (hasRiver ? 32 : Math.Max(0, 18 - riverDistance * 5)) + rng.Next(-9, 10),
                0, 100);

            var temperature = Math.Clamp(temperatureBase - elevation / 8 + rng.Next(-5, 6), 0, 100);
            var fertility = Math.Clamp(
                (moisture + (100 - Math.Abs(temperature - 55))) / 2 - elevation / 5 + (hasRiver ? 12 : 0),
                5, 100);

            var forest = Math.Clamp(
                moisture + elevation / 4 - fertility / 4 + rng.Next(-14, 15),
                0, 100);

            var resources = LocalResources(rng, fertility, forest, elevation, hasRiver, moisture);
            var terrain = CellTerrain(elevation, forest, fertility, hasRiver);

            cells.Add(new MapCell(
                x, y, elevation, moisture, temperature, fertility, forest, hasRiver, terrain, resources));
        }

        var avgFertility = (int)Math.Round(cells.Average(c => c.Fertility));
        var avgForest = (int)Math.Round(cells.Average(c => c.ForestCover));
        var avgWater = (int)Math.Round(cells.Average(c => c.HasRiver ? 100 : c.Moisture));
        var trade = Math.Clamp(80 - (int)cells.Average(c => c.Elevation) / 2 + avgWater / 5 + rng.Next(-8, 9), 10, 95);

        var area = width * height * 4;
        var carryingCapacity = Math.Max(2500, area * (avgFertility + avgWater / 2 + trade / 3) / 22);
        var population = Math.Max(1800, carryingCapacity + rng.Next(-carryingCapacity / 10, carryingCapacity / 10 + 1));

        var settledShare = Math.Clamp(0.14 + trade / 500.0, 0.18, 0.34);
        var settled = (int)Math.Round(population * settledShare);
        var remainder = population - settled;
        var rural = (int)Math.Round(remainder * 0.70);
        var hamlets = (int)Math.Round(remainder * 0.20);
        var isolated = remainder - rural - hamlets;

        var climate = ClimateFrom(temperatureBase, moistureBase);

        return
        [
            new Region(
                "region.1",
                GenerateRegionName(rng),
                climate,
                population,
                avgFertility,
                avgForest,
                avgWater,
                trade,
                area,
                width,
                height,
                new PopulationBreakdown(settled, rural, hamlets, isolated),
                cells)
        ];
    }

    private static List<Settlement> GenerateSettlements(Random rng, List<Region> regions)
    {
        var result = new List<Settlement>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var id = 1;

        foreach (var region in regions)
        {
            var count = Math.Clamp(region.Population / 3000, 3, 9);
            var candidates = region.Cells
                .Where(c => !c.HasRiver)
                .Select(c => new
                {
                    Cell = c,
                    Score = SettlementSuitability(c, region)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var chosen = new List<MapCell>();
            for (var i = 0; i < count; i++)
            {
                var pool = candidates
                    .Where(x => chosen.All(c => Distance(c.X, c.Y, x.Cell.X, x.Cell.Y) >= 4.0))
                    .Take(18)
                    .ToList();

                if (pool.Count == 0)
                    break;

                var selected = pool[rng.Next(Math.Min(pool.Count, 7))].Cell;
                chosen.Add(selected);

                var isPrimary = i == 0;
                var type = isPrimary
                    ? region.Population >= 12000 ? "town" : "market_town"
                    : PickSettlementType(rng, selected, region);

                var population = PopulationForSettlement(rng, type, selected, region);
                var localResources = ResourcesAround(region, selected.X, selected.Y, 2);
                var specializations = DeriveSettlementSpecializations(selected, localResources, type);
                var landmarks = DeriveLandmarks(type, specializations);

                result.Add(new Settlement(
                    $"settlement.{id++}",
                    GenerateUniqueSettlementName(rng, names),
                    type,
                    region.Id,
                    population,
                    selected.X,
                    selected.Y,
                    landmarks,
                    [],
                    localResources,
                    specializations));
            }

            RebalanceSettlementPopulation(region, result.Where(x => x.RegionId == region.Id).ToList());
        }

        return result;
    }

    private static List<Road> GenerateRoads(Random rng, List<Settlement> settlements)
    {
        var roads = new List<Road>();
        var id = 1;

        foreach (var group in settlements.GroupBy(s => s.RegionId))
        {
            var local = group.OrderByDescending(s => s.Population).ToList();
            if (local.Count < 2) continue;

            var connected = new HashSet<string> { local[0].Id };

            while (connected.Count < local.Count)
            {
                var options =
                    from a in local
                    where connected.Contains(a.Id)
                    from b in local
                    where !connected.Contains(b.Id)
                    let distance = Distance(a.X, a.Y, b.X, b.Y)
                    orderby distance
                    select new { A = a, B = b, Distance = distance };

                var best = options.First();
                var path = StraightPath(best.A.X, best.A.Y, best.B.X, best.B.Y);
                roads.Add(new Road(
                    $"road.{id++}",
                    best.A.Id,
                    best.B.Id,
                    path.Count,
                    path));
                connected.Add(best.B.Id);
            }

            if (local.Count >= 3 && rng.NextDouble() < 0.65)
            {
                var a = local[0];
                var b = local.OrderByDescending(s => Distance(a.X, a.Y, s.X, s.Y)).First();
                if (!roads.Any(r => Connects(r, a.Id, b.Id)))
                {
                    var path = StraightPath(a.X, a.Y, b.X, b.Y);
                    roads.Add(new Road($"road.{id++}", a.Id, b.Id, path.Count, path));
                }
            }
        }

        return roads;
    }

    private static void ApplyRoadsToSettlements(List<Settlement> settlements, List<Road> roads)
    {
        foreach (var settlement in settlements)
        {
            foreach (var road in roads.Where(r =>
                         r.FromSettlementId == settlement.Id || r.ToSettlementId == settlement.Id))
                settlement.Connections.Add(road.Id);
        }
    }

    private static List<Business> GenerateBusinesses(
        Random rng,
        List<Region> regions,
        List<Settlement> settlements)
    {
        var result = new List<Business>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var local = settlement.LocalResources;

            void Add(string type, string goods, int count)
            {
                for (var n = 0; n < count; n++)
                    result.Add(new Business(
                        $"business.{id++}",
                        GenerateBusinessName(rng, settlement.Name, type),
                        type,
                        settlement.Id,
                        "",
                        [],
                        goods.Split('|').ToList()));
            }

            if (local.Contains("obilí"))
            {
                Add("farm", "obilí", Math.Max(1, settlement.Population / 450));
                if (local.Contains("říční voda") && settlement.Population >= 220)
                    Add("mill", "mouka|mletí obilí", Math.Max(1, settlement.Population / 1900));
                if (settlement.Population >= 500)
                    Add("bakery", "chléb|pečivo", Math.Max(1, settlement.Population / 1800));
            }

            if (local.Contains("dřevo"))
            {
                Add("woodcutter", "dřevo", Math.Max(1, settlement.Population / 1000));
                if (settlement.Population >= 350)
                    Add("carpenter", "nábytek|tesařské práce", Math.Max(1, settlement.Population / 1600));
            }

            if (local.Contains("železná ruda") && settlement.Population >= 280)
                Add("smithy", "kovářské výrobky|nástroje", Math.Max(1, settlement.Population / 1800));

            if (local.Contains("ryby") && settlement.Population >= 120)
                Add("fishery", "ryby", Math.Max(1, settlement.Population / 1400));

            if (settlement.Population >= 250)
                Add("inn", "ubytování|jídlo|pití", Math.Max(1, settlement.Population / 2400));

            if (settlement.Type is "town" or "market_town")
            {
                Add("merchant", "dovoz|vývoz", Math.Max(1, settlement.Population / 2200));
                Add("market_stall", "místní zboží", Math.Max(1, settlement.Population / 1300));
            }
        }

        return result;
    }

    private static List<Family> GenerateFamilies(Random rng, List<Settlement> settlements)
    {
        var result = new List<Family>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var importantHouseholds = Math.Clamp(settlement.Population / 160, 3, 14);

            for (var i = 0; i < importantHouseholds; i++)
            {
                var socialClass =
                    i == 0 && settlement.Type is "town" or "market_town"
                        ? "elite"
                        : rng.NextDouble() < 0.20 ? "merchant" : "common";

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
                var character = new Character(
                    $"person.{id++}",
                    GenerateGivenName(rng),
                    age,
                    PickProfession(rng, settlement, businesses),
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

    private static List<Business> AssignBusinessOwners(
        Random rng,
        List<Business> businesses,
        List<Character> characters)
    {
        return businesses.Select(b =>
        {
            var candidates = characters
                .Where(x => x.HomeSettlementId == b.SettlementId && x.Age >= 18)
                .ToList();

            var owner = candidates.Count == 0
                ? characters.First()
                : candidates[rng.Next(candidates.Count)];

            var workerCount = Math.Min(Math.Max(0, candidates.Count - 1), WorkerCountFor(b.Type, rng));
            var workers = candidates
                .Where(x => x.Id != owner.Id)
                .OrderBy(_ => rng.Next())
                .Take(workerCount)
                .Select(x => x.Id)
                .ToList();

            return b with { OwnerCharacterId = owner.Id, WorkerCharacterIds = workers };
        }).ToList();
    }

    private static List<Faction> GenerateFactions(
        Random rng,
        WorldConfig config,
        List<Settlement> settlements)
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

    private static List<Character> AssignFactionMemberships(
        Random rng,
        List<Character> characters,
        List<Faction> factions)
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

    private static List<Relationship> GenerateRelationships(
        Random rng,
        List<Character> characters,
        List<Family> families)
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

        return result;
    }

    private static List<HistoricalEvent> GenerateHistory(
        Random rng,
        WorldConfig config,
        List<Settlement> settlements,
        List<Faction> factions)
    {
        var events = new List<HistoricalEvent>();
        var startYear = 1427 - config.HistoryYears;

        foreach (var settlement in settlements)
        {
            events.Add(new HistoricalEvent(
                rng.Next(startYear, 1400),
                $"{settlement.Name} se ustálilo jako {SettlementRole(settlement)}.",
                [settlement.Id],
                []));

            if (settlement.EconomicSpecializations.Contains("obilí") && rng.NextDouble() < 0.65)
                events.Add(new HistoricalEvent(
                    rng.Next(1400, 1426),
                    $"Dobrá úroda posílila obchod s obilím v okolí {settlement.Name}.",
                    [settlement.Id],
                    []));
        }

        foreach (var faction in factions)
            events.Add(new HistoricalEvent(
                rng.Next(1390, 1427),
                $"{faction.Name} získala výraznější vliv v místních záležitostech.",
                [faction.SeatSettlementId],
                [faction.Id]));

        return events.OrderBy(x => x.Year).ToList();
    }

    private static StartScene GenerateStartScene(
        Random rng,
        List<Region> regions,
        List<Settlement> settlements,
        List<Road> roads)
    {
        var region = regions[0];

        var occupied = settlements.Select(s => (s.X, s.Y)).ToHashSet();
        var candidates = region.Cells
            .Where(c => !occupied.Contains((c.X, c.Y)))
            .Where(c => settlements.Any(s => Distance(c.X, c.Y, s.X, s.Y) <= 7.0))
            .ToList();

        var cell = candidates.Count > 0
            ? candidates[rng.Next(candidates.Count)]
            : region.Cells[rng.Next(region.Cells.Count)];

        var nearest = settlements
            .OrderBy(s => Distance(cell.X, cell.Y, s.X, s.Y))
            .Take(Math.Min(2, settlements.Count))
            .ToList();

        var features = new List<string> { cell.Terrain };
        if (cell.HasRiver) features.Add("řeka");
        if (cell.ForestCover >= 55) features.Add("hustší porost");
        if (cell.Fertility >= 60 && cell.ForestCover < 55) features.Add("obhospodařovatelná půda");
        features.AddRange(cell.Resources.Select(ResourceFeature).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

        var roadHere = roads.Any(r => r.Path.Any(p => p.X == cell.X && p.Y == cell.Y));
        if (roadHere) features.Add("cesta procházející místem");

        var destinations = nearest
            .Select(s => $"{s.Name} – {Distance(cell.X, cell.Y, s.X, s.Y):0.0} buněk")
            .ToList();

        var actions = new List<string> { "Rozhlédnout se po okolí" };
        if (cell.HasRiver) actions.Add("Jít k řece");
        if (cell.Resources.Contains("dřevo")) actions.Add("Prohlédnout okolní porost");
        if (cell.Resources.Contains("železná ruda") || cell.Resources.Contains("kámen"))
            actions.Add("Prohlédnout skalnaté podloží");
        if (roadHere) actions.Add("Prohlédnout cestu");
        actions.AddRange(nearest.Select(s => $"Vydat se směrem na {s.Name}"));

        return new StartScene(
            "scene.start",
            $"{cell.Terrain} v regionu {region.Name}",
            cell.ForestCover >= 55 ? "forest_cell" : "rural_cell",
            region.Name,
            WeatherFor(region, rng),
            cell.X,
            cell.Y,
            features.Distinct().ToList(),
            destinations,
            [],
            actions);
    }

    private static int SettlementSuitability(MapCell cell, Region region)
    {
        var waterBonus = cell.HasRiver ? 28 : Math.Min(22, cell.Moisture / 4);
        var flatness = Math.Max(0, 30 - Math.Abs(cell.Elevation - 35) / 2);
        var fertility = cell.Fertility / 2;
        var resourceVariety = cell.Resources.Count * 4;
        var forestPenalty = Math.Max(0, cell.ForestCover - 75) / 3;
        return fertility + waterBonus + flatness + resourceVariety + region.TradeAccess / 5 - forestPenalty;
    }

    private static void RebalanceSettlementPopulation(Region region, List<Settlement> local)
    {
        if (local.Count == 0) return;

        var target = region.PopulationBreakdown.Settled;
        var current = local.Sum(s => s.Population);
        if (current <= 0) return;

        var factor = target / (double)current;
        foreach (var settlement in local)
            settlement.Population = Math.Max(40, (int)Math.Round(settlement.Population * factor));
    }

    private static List<string> ResourcesAround(Region region, int x, int y, int radius)
    {
        return region.Cells
            .Where(c => Distance(c.X, c.Y, x, y) <= radius)
            .SelectMany(c => c.Resources.Concat(c.HasRiver ? ["říční voda"] : []))
            .Distinct()
            .OrderBy(v => v)
            .ToList();
    }

    private static List<string> LocalResources(
        Random rng,
        int fertility,
        int forest,
        int elevation,
        bool hasRiver,
        int moisture)
    {
        var result = new List<string>();
        if (fertility >= 58) result.Add("obilí");
        if (forest >= 52) result.Add("dřevo");
        if (forest >= 68) result.Add("zvěř");
        if (elevation >= 60 && rng.NextDouble() < 0.38) result.Add("železná ruda");
        if (elevation >= 48) result.Add("kámen");
        if (hasRiver && moisture >= 55) result.Add("ryby");
        if (fertility < 45 && forest < 50) result.Add("pastviny");
        return result;
    }

    private static string CellTerrain(int elevation, int forest, int fertility, bool hasRiver)
    {
        if (hasRiver && forest >= 50) return "zalesněné říční údolí";
        if (hasRiver) return "říční nížina";
        if (elevation >= 72) return "skalnatá vrchovina";
        if (elevation >= 55 && forest >= 55) return "zalesněná vrchovina";
        if (forest >= 70) return "hustý les";
        if (forest >= 48) return "lesnatá krajina";
        if (fertility >= 62) return "úrodná otevřená krajina";
        return "zvlněná venkovská krajina";
    }

    private static List<string> DeriveSettlementSpecializations(
        MapCell cell,
        List<string> localResources,
        string type)
    {
        var result = new List<string>();
        if (localResources.Contains("obilí")) result.Add("obilí");
        if (localResources.Contains("dřevo")) result.Add("dřevo");
        if (localResources.Contains("železná ruda")) result.Add("kovářství");
        if (localResources.Contains("ryby")) result.Add("rybolov");
        if (localResources.Contains("pastviny")) result.Add("chov");
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
        if (specializations.Contains("rybolov")) result.Add("rybí trh");

        return result;
    }

    private static string PickSettlementType(Random rng, MapCell cell, Region region)
    {
        var roll = rng.NextDouble();
        if (region.TradeAccess >= 65 && roll < 0.18) return "market_town";
        if (cell.Fertility >= 62 && roll < 0.72) return "village";
        return roll < 0.70 ? "village" : "hamlet";
    }

    private static int PopulationForSettlement(Random rng, string type, MapCell cell, Region region)
    {
        var environmentFactor = Math.Clamp((cell.Fertility + cell.Moisture + region.TradeAccess) / 150.0, 0.6, 1.5);

        var basePop = type switch
        {
            "town" => rng.Next(2200, 4800),
            "market_town" => rng.Next(850, 2200),
            "village" => rng.Next(180, 720),
            _ => rng.Next(60, 220)
        };

        return Math.Max(40, (int)Math.Round(basePop * environmentFactor));
    }

    private static string PickProfession(Random rng, Settlement settlement, List<Business> businesses)
    {
        var localTypes = businesses
            .Where(x => x.SettlementId == settlement.Id)
            .Select(x => x.Type)
            .ToList();

        var professions = new List<string> { "zemědělec", "dělník", "správce domácnosti" };

        if (localTypes.Contains("mill")) professions.Add("mlynář");
        if (localTypes.Contains("bakery")) professions.Add("pekař");
        if (localTypes.Contains("inn")) professions.Add("hostinský");
        if (localTypes.Contains("merchant")) professions.Add("kupec");
        if (localTypes.Contains("smithy")) professions.Add("kovář");
        if (localTypes.Contains("woodcutter")) professions.Add("dřevorubec");
        if (localTypes.Contains("carpenter")) professions.Add("tesař");
        if (localTypes.Contains("fishery")) professions.Add("rybář");

        return professions[rng.Next(professions.Count)];
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

        moisture = Math.Clamp(moisture, 0, 100);
        temperature = Math.Clamp(temperature, 0, 100);
        elevation = Math.Clamp(elevation, 0, 100);
    }

    private static string ClimateFrom(int temperature, int moisture)
    {
        if (temperature < 35) return moisture > 55 ? "chladné vlhké" : "chladné suché";
        if (temperature > 68) return moisture > 55 ? "teplé vlhké" : "teplé suché";
        return moisture > 65 ? "mírné vlhké" : moisture < 35 ? "mírné suché" : "mírné";
    }

    private static List<MapPoint> StraightPath(int x1, int y1, int x2, int y2)
    {
        var result = new List<MapPoint>();
        var steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));

        if (steps == 0)
        {
            result.Add(new MapPoint(x1, y1));
            return result;
        }

        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var x = (int)Math.Round(x1 + (x2 - x1) * t);
            var y = (int)Math.Round(y1 + (y2 - y1) * t);
            if (result.Count == 0 || result[^1].X != x || result[^1].Y != y)
                result.Add(new MapPoint(x, y));
        }

        return result;
    }

    private static bool Connects(Road road, string a, string b) =>
        (road.FromSettlementId == a && road.ToSettlementId == b) ||
        (road.FromSettlementId == b && road.ToSettlementId == a);

    private static double Distance(int x1, int y1, int x2, int y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Distance01(
        int x, int y,
        double cx, double cy,
        int width, int height)
    {
        var dx = (x - cx) / width;
        var dy = (y - cy) / height;
        return Math.Clamp(Math.Sqrt(dx * dx + dy * dy), 0, 1);
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
        "fishery" => $"Rybářství {GenerateSurname(rng)}",
        "farm" => $"Hospodářství {GenerateSurname(rng)}",
        _ => $"{type} {GenerateSurname(rng)}"
    };

    private static string GenerateSurname(Random rng) =>
        Pick(rng, "Novák", "Beran", "Kovář", "Mlynář", "Růžička", "Vlk", "Holub", "Černý", "Jelen");

    private static string GenerateGivenName(Random rng) =>
        Pick(rng, "Matěj", "Jan", "Ondřej", "Václav", "Anna", "Kateřina", "Eliška", "Markéta", "Jakub", "Petr");

    private static string SettlementRole(Settlement settlement) =>
        settlement.Type is "town" or "market_town"
            ? "regionální obchodní centrum"
            : "lokální sídlo navázané na zdroje svého okolí";

    private static string WeatherFor(Region region, Random rng)
    {
        if (region.Climate.Contains("chlad"))
            return rng.NextDouble() < 0.5 ? "chladné jasno" : "chladno a zataženo";
        if (region.Climate.Contains("vlhké"))
            return rng.NextDouble() < 0.5 ? "vlhké ráno" : "přeháňky";
        if (region.Climate.Contains("teplé"))
            return "teplé ráno";
        return rng.NextDouble() < 0.5 ? "jasné ráno" : "proměnlivá oblačnost";
    }

    private static string ResourceFeature(string resource) => resource switch
    {
        "dřevo" => "využitelný lesní porost",
        "železná ruda" => "náznaky rudonosného podloží",
        "kámen" => "skalnaté podloží",
        "obilí" => "úrodná půda",
        "ryby" => "voda vhodná k rybolovu",
        "pastviny" => "travnaté plochy",
        _ => resource
    };

    private static string Pick(Random rng, params string[] values) =>
        values[rng.Next(values.Length)];
}

internal static class RngStreams
{
    public static Random For(int rootSeed, string stream)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in $"{rootSeed}:{stream}")
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return new Random((int)(hash & 0x7fffffff));
        }
    }
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
    List<Road> Roads,
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
    int Population,
    int Fertility,
    int ForestCover,
    int WaterAvailability,
    int TradeAccess,
    int AreaKm2,
    int Width,
    int Height,
    PopulationBreakdown PopulationBreakdown,
    List<MapCell> Cells);

internal sealed record PopulationBreakdown(
    int Settled,
    int RuralHouseholds,
    int SmallHamlets,
    int Isolated)
{
    [JsonIgnore]
    public int Total => Settled + RuralHouseholds + SmallHamlets + Isolated;
}

internal sealed record MapCell(
    int X,
    int Y,
    int Elevation,
    int Moisture,
    int Temperature,
    int Fertility,
    int ForestCover,
    bool HasRiver,
    string Terrain,
    List<string> Resources);

internal sealed record MapPoint(int X, int Y);

internal sealed record Road(
    string Id,
    string FromSettlementId,
    string ToSettlementId,
    int LengthCells,
    List<MapPoint> Path);

internal sealed record Settlement(
    string Id,
    string Name,
    string Type,
    string RegionId,
    [property: JsonIgnore] int InitialPopulation,
    int X,
    int Y,
    List<string> Landmarks,
    List<string> Connections,
    List<string> LocalResources,
    List<string> EconomicSpecializations)
{
    public int Population { get; set; } = InitialPopulation;
}

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
    List<string> ServicesOrGoods);

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
    int X,
    int Y,
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

        foreach (var region in world.Regions)
        {
            if (region.Cells.Count != region.Width * region.Height)
                errors.Add($"Region {region.Id} has invalid cell count.");

            if (region.PopulationBreakdown.Total != region.Population)
                errors.Add($"Region {region.Id} population breakdown does not match total.");

            if (region.Cells.Any(c => c.X < 0 || c.X >= region.Width || c.Y < 0 || c.Y >= region.Height))
                errors.Add($"Region {region.Id} contains out-of-bounds cells.");
        }

        foreach (var settlement in world.Settlements)
        {
            if (!regionIds.Contains(settlement.RegionId))
                errors.Add($"Settlement {settlement.Id} references missing region {settlement.RegionId}.");

            var region = world.Regions.Single(r => r.Id == settlement.RegionId);
            if (settlement.X < 0 || settlement.X >= region.Width ||
                settlement.Y < 0 || settlement.Y >= region.Height)
                errors.Add($"Settlement {settlement.Id} is outside its region.");

            if (settlement.Population <= 0)
                errors.Add($"Settlement {settlement.Id} has invalid population.");
        }

        foreach (var road in world.Roads)
        {
            if (!settlementIds.Contains(road.FromSettlementId) ||
                !settlementIds.Contains(road.ToSettlementId))
                errors.Add($"Road {road.Id} references missing settlement.");

            if (road.Path.Count == 0)
                errors.Add($"Road {road.Id} has no path.");
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
