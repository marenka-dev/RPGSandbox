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
        Console.WriteLine("RPGSandbox – World Generator v0.4 Population & Economy");
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
            Console.WriteLine($"  Populace celkem: {region.Population}");
            Console.WriteLine($"    v hlavních sídlech: {region.PopulationBreakdown.Settled}");
            Console.WriteLine($"    rozptýlené venkovské domácnosti: {region.PopulationBreakdown.RuralHouseholds}");
            Console.WriteLine($"    malé osady: {region.PopulationBreakdown.SmallHamlets}");
            Console.WriteLine($"    samoty a izolovaná místa: {region.PopulationBreakdown.Isolated}");
            Console.WriteLine($"  Říční buňky: {region.Cells.Count(x => x.HasRiver)}");
        }

        Console.WriteLine();
        Console.WriteLine("SÍDLA A EKONOMIKA:");
        foreach (var settlement in world.Settlements.OrderByDescending(x => x.Population))
        {
            var economy = world.SettlementEconomies.Single(x => x.SettlementId == settlement.Id);
            var households = world.Households.Count(x => x.SettlementId == settlement.Id);
            var householdPeople = world.Households.Where(x => x.SettlementId == settlement.Id).Sum(x => x.MemberCount);

            Console.WriteLine($"- {settlement.Name} ({settlement.Type}) [{settlement.X},{settlement.Y}], pop. {settlement.Population}");
            Console.WriteLine($"    domácnosti: {households}, obyvatel v domácnostech: {householdPeople}");
            Console.WriteLine($"    místní zdroje: {string.Join(", ", settlement.LocalResources)}");
            Console.WriteLine($"    potřeby: {string.Join(", ", economy.Demands.Select(x => $"{x.Resource}:{x.Amount}"))}");
            Console.WriteLine($"    produkce: {string.Join(", ", economy.Production.Select(x => $"{x.Resource}:{x.Amount}"))}");
            Console.WriteLine($"    bilance: {string.Join(", ", economy.Balances.Select(x => $"{x.Resource}:{x.Amount:+0;-0;0}"))}");
            Console.WriteLine($"    pracovní poptávka: {string.Join(", ", economy.JobDemand.Select(x => $"{x.Profession}:{x.WorkersNeeded}"))}");
        }

        Console.WriteLine();
        Console.WriteLine("PODNIKY:");
        foreach (var business in world.Businesses)
        {
            var settlement = world.Settlements.Single(x => x.Id == business.SettlementId);
            Console.WriteLine(
                $"- {business.Name} / {business.Type} / {settlement.Name} / " +
                $"pracovníci {business.RequiredWorkers} / kapacita {business.Capacity}");
        }

        Console.WriteLine();
        Console.WriteLine($"Domácnosti: {world.Households.Count}");
        Console.WriteLine($"Lehké persistentní osoby: {world.People.Count}");
        Console.WriteLine($"Významné postavy: {world.Characters.Count}");
        Console.WriteLine($"Frakce: {world.Factions.Count}");
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
        var populationRng = RngStreams.For(config.Seed, "population");
        var economyRng = RngStreams.For(config.Seed, "economy");
        var politicsRng = RngStreams.For(config.Seed, "politics");
        var historyRng = RngStreams.For(config.Seed, "history");
        var sceneRng = RngStreams.For(config.Seed, "scene");

        var regions = GenerateRegions(geographyRng, config);
        var settlements = GenerateSettlements(settlementRng, regions);
        var roads = GenerateRoads(settlementRng, settlements);
        ApplyRoadsToSettlements(settlements, roads);

        var households = GenerateHouseholds(populationRng, settlements);
        var people = GeneratePeople(populationRng, households, settlements);
        var economies = GenerateSettlementEconomies(economyRng, settlements);
        var businesses = GenerateBusinesses(economyRng, settlements, economies);
        AssignHouseholdOccupations(populationRng, households, economies);
        var characters = GenerateImportantCharacters(populationRng, households, people, settlements);
        businesses = AssignBusinessOwners(populationRng, businesses, characters);

        var factions = GenerateFactions(politicsRng, config, settlements);
        characters = AssignFactionMemberships(politicsRng, characters, factions);
        var relationships = GenerateRelationships(populationRng, characters);
        var history = GenerateHistory(historyRng, config, settlements, factions);
        var start = GenerateStartScene(sceneRng, regions, settlements, roads);

        return new GeneratedWorld(
            config,
            new DateTime(1427, 5, 14),
            regions,
            settlements,
            roads,
            households,
            people,
            economies,
            factions,
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

        return
        [
            new Region(
                "region.1",
                GenerateRegionName(rng),
                ClimateFrom(temperatureBase, moistureBase),
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
                .Select(c => new { Cell = c, Score = SettlementSuitability(c, region) })
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

                result.Add(new Settlement(
                    $"settlement.{id++}",
                    GenerateUniqueSettlementName(rng, names),
                    type,
                    region.Id,
                    population,
                    selected.X,
                    selected.Y,
                    DeriveLandmarks(type, localResources),
                    [],
                    localResources));
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
                var best =
                    (from a in local
                     where connected.Contains(a.Id)
                     from b in local
                     where !connected.Contains(b.Id)
                     let distance = Distance(a.X, a.Y, b.X, b.Y)
                     orderby distance
                     select new { A = a, B = b }).First();

                var path = StraightPath(best.A.X, best.A.Y, best.B.X, best.B.Y);
                roads.Add(new Road($"road.{id++}", best.A.Id, best.B.Id, path.Count, path));
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
            foreach (var road in roads.Where(r =>
                         r.FromSettlementId == settlement.Id || r.ToSettlementId == settlement.Id))
                settlement.Connections.Add(road.Id);
    }

    private static List<Household> GenerateHouseholds(Random rng, List<Settlement> settlements)
    {
        var result = new List<Household>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var remaining = settlement.Population;
            while (remaining > 0)
            {
                var desired = rng.Next(2, 7);
                var members = Math.Min(desired, remaining);
                remaining -= members;

                var socialClassRoll = rng.NextDouble();
                var socialClass =
                    settlement.Type is "town" or "market_town" && socialClassRoll < 0.05 ? "elite" :
                    socialClassRoll < 0.22 ? "artisan" :
                    socialClassRoll < 0.35 ? "merchant" :
                    "common";

                result.Add(new Household(
                    $"household.{id++}",
                    settlement.Id,
                    members,
                    socialClass,
                    "",
                    0,
                    []));
            }
        }

        return result;
    }

    private static List<PersonStub> GeneratePeople(
        Random rng,
        List<Household> households,
        List<Settlement> settlements)
    {
        var result = new List<PersonStub>();
        var id = 1;

        foreach (var household in households)
        {
            for (var i = 0; i < household.MemberCount; i++)
            {
                var age = i == 0
                    ? rng.Next(24, 66)
                    : rng.NextDouble() < 0.40 ? rng.Next(0, 16) : rng.Next(16, 66);

                result.Add(new PersonStub(
                    $"person.stub.{id++}",
                    GenerateGivenName(rng),
                    age,
                    household.Id,
                    household.SettlementId));
            }
        }

        return result;
    }

    private static List<SettlementEconomy> GenerateSettlementEconomies(
        Random rng,
        List<Settlement> settlements)
    {
        var result = new List<SettlementEconomy>();

        foreach (var settlement in settlements)
        {
            var demands = new List<ResourceAmount>
            {
                new("food", settlement.Population * 10),
                new("fuel", settlement.Population * 3),
                new("clothing", Math.Max(1, settlement.Population / 2)),
                new("tools", Math.Max(1, settlement.Population / 5))
            };

            var production = new List<ResourceAmount>();

            if (settlement.LocalResources.Contains("obilí"))
                production.Add(new("food", settlement.Population * rng.Next(7, 14)));

            if (settlement.LocalResources.Contains("ryby"))
                AddProduction(production, "food", settlement.Population * rng.Next(1, 4));

            if (settlement.LocalResources.Contains("dřevo"))
                production.Add(new("fuel", settlement.Population * rng.Next(3, 7)));

            if (settlement.LocalResources.Contains("dřevo"))
                production.Add(new("tools", Math.Max(1, settlement.Population / rng.Next(5, 9))));

            if (settlement.LocalResources.Contains("železná ruda"))
                AddProduction(production, "tools", Math.Max(1, settlement.Population / rng.Next(3, 6)));

            var balances = demands
                .Select(d =>
                {
                    var produced = production.Where(p => p.Resource == d.Resource).Sum(p => p.Amount);
                    return new ResourceAmount(d.Resource, produced - d.Amount);
                })
                .ToList();

            var jobs = GenerateJobDemand(settlement, demands, production);

            result.Add(new SettlementEconomy(
                settlement.Id,
                demands,
                production,
                balances,
                jobs));
        }

        return result;
    }

    private static List<JobDemand> GenerateJobDemand(
        Settlement settlement,
        List<ResourceAmount> demand,
        List<ResourceAmount> production)
    {
        var jobs = new List<JobDemand>();
        var workingAgePopulation = (int)Math.Round(settlement.Population * 0.55);

        var agriculture = settlement.LocalResources.Contains("obilí")
            ? Math.Max(1, workingAgePopulation * 35 / 100)
            : Math.Max(1, workingAgePopulation * 10 / 100);

        jobs.Add(new("zemědělec", agriculture));

        if (settlement.LocalResources.Contains("dřevo"))
            jobs.Add(new("dřevorubec", Math.Max(1, workingAgePopulation * 8 / 100)));

        if (settlement.LocalResources.Contains("železná ruda"))
            jobs.Add(new("horník", Math.Max(1, workingAgePopulation * 4 / 100)));

        if (settlement.LocalResources.Contains("ryby"))
            jobs.Add(new("rybář", Math.Max(1, workingAgePopulation * 4 / 100)));

        jobs.Add(new("řemeslník", Math.Max(1, workingAgePopulation * 12 / 100)));
        jobs.Add(new("služby", Math.Max(1, workingAgePopulation * 8 / 100)));

        if (settlement.Type is "town" or "market_town")
        {
            jobs.Add(new("obchodník", Math.Max(1, workingAgePopulation * 7 / 100)));
            jobs.Add(new("správa", Math.Max(1, workingAgePopulation * 4 / 100)));
        }

        var allocated = jobs.Sum(x => x.WorkersNeeded);
        if (allocated < workingAgePopulation)
            jobs.Add(new("ostatní práce", workingAgePopulation - allocated));

        return jobs;
    }

    private static List<Business> GenerateBusinesses(
        Random rng,
        List<Settlement> settlements,
        List<SettlementEconomy> economies)
    {
        var result = new List<Business>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var economy = economies.Single(x => x.SettlementId == settlement.Id);

            void Add(string type, int workersPerBusiness, int capacityPerWorker, params string[] outputs)
            {
                var job = JobForBusiness(type);
                var workers = economy.JobDemand.FirstOrDefault(x => x.Profession == job)?.WorkersNeeded ?? 0;
                var count = Math.Max(0, (int)Math.Ceiling(workers / (double)workersPerBusiness));

                for (var n = 0; n < count; n++)
                {
                    var required = Math.Min(workersPerBusiness, Math.Max(1, workers - n * workersPerBusiness));
                    result.Add(new Business(
                        $"business.{id++}",
                        GenerateBusinessName(rng, settlement.Name, type),
                        type,
                        settlement.Id,
                        "",
                        [],
                        outputs.ToList(),
                        required,
                        required * capacityPerWorker));
                }
            }

            if (settlement.LocalResources.Contains("obilí"))
                Add("farm", 8, 120, "food");

            if (settlement.LocalResources.Contains("dřevo"))
                Add("woodcutter", 6, 80, "fuel", "wood");

            if (settlement.LocalResources.Contains("železná ruda"))
                Add("mine", 10, 50, "iron_ore");

            if (settlement.LocalResources.Contains("ryby"))
                Add("fishery", 5, 75, "food");

            Add("workshop", 5, 30, "tools");
            Add("inn", 6, 20, "services");

            if (settlement.Type is "town" or "market_town")
                Add("merchant", 5, 25, "trade");
        }

        return result;
    }

    private static void AssignHouseholdOccupations(
        Random rng,
        List<Household> households,
        List<SettlementEconomy> economies)
    {
        foreach (var settlementGroup in households.GroupBy(x => x.SettlementId))
        {
            var economy = economies.Single(x => x.SettlementId == settlementGroup.Key);
            var pool = economy.JobDemand
                .SelectMany(j => Enumerable.Repeat(j.Profession, j.WorkersNeeded))
                .OrderBy(_ => rng.Next())
                .ToList();

            var i = 0;
            foreach (var household in settlementGroup)
            {
                household.PrimaryOccupation = pool.Count == 0
                    ? "ostatní práce"
                    : pool[i++ % pool.Count];

                household.WealthGroats = household.SocialClass switch
                {
                    "elite" => rng.Next(120, 301),
                    "merchant" => rng.Next(70, 181),
                    "artisan" => rng.Next(35, 101),
                    _ => rng.Next(8, 61)
                };
            }
        }
    }

    private static List<Character> GenerateImportantCharacters(
        Random rng,
        List<Household> households,
        List<PersonStub> people,
        List<Settlement> settlements)
    {
        var result = new List<Character>();
        var id = 1;

        foreach (var settlement in settlements)
        {
            var localHouseholds = households
                .Where(x => x.SettlementId == settlement.Id)
                .OrderByDescending(x => x.WealthGroats)
                .Take(Math.Clamp(settlement.Population / 180, 4, 12))
                .ToList();

            foreach (var household in localHouseholds)
            {
                var person = people
                    .Where(x => x.HouseholdId == household.Id && x.Age >= 18)
                    .OrderByDescending(x => x.Age)
                    .FirstOrDefault();

                if (person is null)
                    continue;

                result.Add(new Character(
                    $"character.{id++}",
                    person.Name,
                    person.Age,
                    household.PrimaryOccupation,
                    household.Id,
                    settlement.Id,
                    [],
                    household.WealthGroats,
                    GenerateTraits(rng)));
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

            var workerCount = Math.Min(Math.Max(0, candidates.Count - 1), Math.Min(3, b.RequiredWorkers));
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
                    ? c.Profession == "správa" || c.WealthGroats > 100
                    : f.Type == "guild"
                        ? c.Profession is "obchodník" or "služby" || c.WealthGroats > 80
                        : rng.NextDouble() < 0.12)
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            return c with { FactionIds = memberships };
        }).ToList();
    }

    private static List<Relationship> GenerateRelationships(Random rng, List<Character> characters)
    {
        var result = new List<Relationship>();

        foreach (var group in characters.GroupBy(x => x.HomeSettlementId))
        {
            var local = group.ToList();
            for (var i = 0; i < local.Count; i++)
            for (var j = i + 1; j < local.Count; j++)
            {
                if (rng.NextDouble() > 0.35) continue;

                result.Add(new Relationship(
                    local[i].Id,
                    local[j].Id,
                    rng.Next(25, 81),
                    rng.Next(25, 81),
                    rng.Next(10, 71),
                    rng.Next(0, 31),
                    rng.Next(0, 31),
                    rng.Next(10, 71),
                    rng.Next(0, 41)));
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
            events.Add(new HistoricalEvent(
                rng.Next(startYear, 1400),
                $"{settlement.Name} se ustálilo jako významnější místní sídlo.",
                [settlement.Id],
                []));

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

        var diff = target - local.Sum(s => s.Population);
        if (diff != 0)
            local[0].Population += diff;
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

    private static List<string> DeriveLandmarks(string type, List<string> localResources)
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

        if (localResources.Contains("obilí")) result.Add("sýpky");
        if (localResources.Contains("dřevo")) result.Add("dřevní sklad");
        if (localResources.Contains("železná ruda")) result.Add("kovárna");
        if (localResources.Contains("ryby")) result.Add("rybí trh");

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

    private static void AddProduction(List<ResourceAmount> production, string resource, int amount)
    {
        var existing = production.FindIndex(x => x.Resource == resource);
        if (existing >= 0)
            production[existing] = production[existing] with { Amount = production[existing].Amount + amount };
        else
            production.Add(new ResourceAmount(resource, amount));
    }

    private static string JobForBusiness(string type) => type switch
    {
        "farm" => "zemědělec",
        "woodcutter" => "dřevorubec",
        "mine" => "horník",
        "fishery" => "rybář",
        "merchant" => "obchodník",
        "inn" => "služby",
        _ => "řemeslník"
    };

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
            return [new MapPoint(x1, y1)];

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

    private static double Distance01(int x, int y, double cx, double cy, int width, int height)
    {
        var dx = (x - cx) / width;
        var dy = (y - cy) / height;
        return Math.Clamp(Math.Sqrt(dx * dx + dy * dy), 0, 1);
    }

    private static List<string> GenerateTraits(Random rng)
    {
        string[] pool =
            ["praktický", "společenský", "opatrný", "ambiciózní", "pracovitý", "nedůvěřivý", "štědrý", "tvrdohlavý"];
        return pool.OrderBy(_ => rng.Next()).Take(2).ToList();
    }

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
        "mine" => $"Důl {GenerateSurname(rng)}",
        "fishery" => $"Rybářství {GenerateSurname(rng)}",
        "woodcutter" => $"Dřevorubecký dvůr {GenerateSurname(rng)}",
        "merchant" => $"Obchod {GenerateSurname(rng)}",
        "farm" => $"Hospodářství {GenerateSurname(rng)}",
        "workshop" => $"Dílna {GenerateSurname(rng)}",
        _ => $"{type} {GenerateSurname(rng)}"
    };

    private static string GenerateSurname(Random rng) =>
        Pick(rng, "Novák", "Beran", "Kovář", "Mlynář", "Růžička", "Vlk", "Holub", "Černý", "Jelen");

    private static string GenerateGivenName(Random rng) =>
        Pick(rng, "Matěj", "Jan", "Ondřej", "Václav", "Anna", "Kateřina", "Eliška", "Markéta", "Jakub", "Petr");

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
        new(seed, "late_medieval", "high", "low", "regional", "medieval", "mixed", "medium", "deep", 80);
}

internal sealed record GeneratedWorld(
    WorldConfig Config,
    DateTime CurrentDate,
    List<Region> Regions,
    List<Settlement> Settlements,
    List<Road> Roads,
    List<Household> Households,
    List<PersonStub> People,
    List<SettlementEconomy> SettlementEconomies,
    List<Faction> Factions,
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
    List<string> LocalResources)
{
    public int Population { get; set; } = InitialPopulation;
}

internal sealed record Household(
    string Id,
    string SettlementId,
    int MemberCount,
    string SocialClass,
    string InitialOccupation,
    int InitialWealthGroats,
    List<string> MemberIds)
{
    public string PrimaryOccupation { get; set; } = InitialOccupation;
    public int WealthGroats { get; set; } = InitialWealthGroats;
}

internal sealed record PersonStub(
    string Id,
    string Name,
    int Age,
    string HouseholdId,
    string SettlementId);

internal sealed record SettlementEconomy(
    string SettlementId,
    List<ResourceAmount> Demands,
    List<ResourceAmount> Production,
    List<ResourceAmount> Balances,
    List<JobDemand> JobDemand);

internal sealed record ResourceAmount(string Resource, int Amount);

internal sealed record JobDemand(string Profession, int WorkersNeeded);

internal sealed record Faction(
    string Id,
    string Name,
    string Type,
    string SeatSettlementId,
    int Influence,
    List<string> Interests);

internal sealed record Character(
    string Id,
    string Name,
    int Age,
    string Profession,
    string HouseholdId,
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
    List<string> Outputs,
    int RequiredWorkers,
    int Capacity);

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
        var householdIds = world.Households.Select(x => x.Id).ToHashSet();
        var characterIds = world.Characters.Select(x => x.Id).ToHashSet();
        var factionIds = world.Factions.Select(x => x.Id).ToHashSet();

        foreach (var region in world.Regions)
        {
            if (region.Cells.Count != region.Width * region.Height)
                errors.Add($"Region {region.Id} has invalid cell count.");

            if (region.PopulationBreakdown.Total != region.Population)
                errors.Add($"Region {region.Id} population breakdown does not match total.");

            var settledPopulation = world.Settlements
                .Where(x => x.RegionId == region.Id)
                .Sum(x => x.Population);

            if (settledPopulation != region.PopulationBreakdown.Settled)
                errors.Add($"Region {region.Id} settled population mismatch.");
        }

        foreach (var settlement in world.Settlements)
        {
            var householdPopulation = world.Households
                .Where(x => x.SettlementId == settlement.Id)
                .Sum(x => x.MemberCount);

            var peoplePopulation = world.People
                .Count(x => x.SettlementId == settlement.Id);

            if (householdPopulation != settlement.Population)
                errors.Add($"Settlement {settlement.Id} household population mismatch.");

            if (peoplePopulation != settlement.Population)
                errors.Add($"Settlement {settlement.Id} people population mismatch.");

            if (!world.SettlementEconomies.Any(x => x.SettlementId == settlement.Id))
                errors.Add($"Settlement {settlement.Id} has no economy.");
        }

        foreach (var household in world.Households)
            if (!settlementIds.Contains(household.SettlementId))
                errors.Add($"Household {household.Id} references missing settlement.");

        foreach (var person in world.People)
            if (!householdIds.Contains(person.HouseholdId))
                errors.Add($"Person {person.Id} references missing household.");

        foreach (var character in world.Characters)
        {
            if (!householdIds.Contains(character.HouseholdId))
                errors.Add($"Character {character.Id} references missing household.");

            foreach (var faction in character.FactionIds)
                if (!factionIds.Contains(faction))
                    errors.Add($"Character {character.Id} references missing faction.");
        }

        foreach (var business in world.Businesses)
        {
            if (!settlementIds.Contains(business.SettlementId))
                errors.Add($"Business {business.Id} references missing settlement.");

            if (!characterIds.Contains(business.OwnerCharacterId))
                errors.Add($"Business {business.Id} references missing owner.");

            foreach (var worker in business.WorkerCharacterIds)
                if (!characterIds.Contains(worker))
                    errors.Add($"Business {business.Id} references missing important character worker.");
        }

        foreach (var economy in world.SettlementEconomies)
        {
            if (!settlementIds.Contains(economy.SettlementId))
                errors.Add($"Economy references missing settlement.");

            foreach (var balance in economy.Balances)
            {
                var demand = economy.Demands.FirstOrDefault(x => x.Resource == balance.Resource)?.Amount ?? 0;
                var production = economy.Production.Where(x => x.Resource == balance.Resource).Sum(x => x.Amount);

                if (production - demand != balance.Amount)
                    errors.Add($"Economy balance mismatch for {economy.SettlementId}/{balance.Resource}.");
            }
        }

        foreach (var relation in world.Relationships)
        {
            if (!characterIds.Contains(relation.FromCharacterId) ||
                !characterIds.Contains(relation.ToCharacterId))
                errors.Add("Relationship references missing character.");
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
