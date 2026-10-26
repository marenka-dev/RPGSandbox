using Microsoft.Data.Sqlite;
using System.Globalization;

namespace RPGSandbox.VerticalSlice;

/// <summary>
/// Persistent world graph for v0.5.
/// It separates objective facts from what the player knows and supports
/// deterministic lazy concretization of previously unspecified detail.
/// </summary>
internal sealed class WorldGraph : IDisposable
{
    private readonly SqliteConnection _db;

    public WorldGraph(string savePath)
    {
        _db = new SqliteConnection($"Data Source={savePath}");
        _db.Open();
    }

    public void Initialize(WorldState state)
    {
        Exec("""
            CREATE TABLE IF NOT EXISTS world_entities (
                id TEXT PRIMARY KEY,
                type TEXT NOT NULL,
                name TEXT NOT NULL,
                source TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS world_facts (
                subject TEXT NOT NULL,
                predicate TEXT NOT NULL,
                object_id TEXT,
                value TEXT,
                PRIMARY KEY(subject, predicate, object_id, value)
            );
            CREATE TABLE IF NOT EXISTS player_knowledge (
                subject TEXT NOT NULL,
                predicate TEXT NOT NULL,
                object_id TEXT,
                value TEXT,
                PRIMARY KEY(subject, predicate, object_id, value)
            );
            """);

        if (ScalarInt("SELECT COUNT(*) FROM world_entities;") == 0)
            Seed(state);
    }

    public void ResetAndSeed(WorldState state)
    {
        Exec("DELETE FROM player_knowledge; DELETE FROM world_facts; DELETE FROM world_entities;");
        Seed(state);
    }

    private void Seed(WorldState state)
    {
        AddEntity("player", "Person", "ty", "seed");
        AddEntity("loc.inn.room", "Location", "pokoj v hostinci U Tří korun", "seed");
        AddEntity("loc.inn.common", "Location", "společenská místnost hostince U Tří korun", "seed");
        AddEntity("loc.market", "Location", "rabenfeldské náměstí", "seed");
        AddEntity("place.inn", "Business", "hostinec U Tří korun", "seed");
        AddEntity("person.matej", "Person", "Matěj", "seed");
        AddEntity("person.havel", "Person", "Havel", "seed");
        AddEntity("item.grain", "Commodity", "obilí", "seed");
        AddEntity("group.market.townspeople", "PopulationGroup", "měšťané na trhu", "macro");
        AddEntity("group.inn.patrons", "PopulationGroup", "další hosté v hostinci", "macro");
        AddEntity("feature.market.stalls", "FeatureGroup", "tržní stánky", "macro");
        AddEntity("object.chair.room", "Furniture", "židle v pokoji", "seed");
        AddEntity("object.table.common", "Furniture", "stůl ve společenské místnosti", "seed");
        AddEntity("object.chair.common", "Furniture", "židle ve společenské místnosti", "seed");
        AddEntity("object.window.common", "Object", "okno ve společenské místnosti", "seed");
        AddEntity("object.door.room", "Object", "dveře pokoje", "seed");
        AddEntity("object.door.common", "Object", "vchodové dveře hostince", "seed");
        AddEntity("loc.street.inn", "Location", "ulice před hostincem U Tří korun", "seed");
        AddEntity("service.inn.breakfast", "MealOffering", "běžná snídaně U Tří korun", "seed");

        AddFact("player", "location", LocationIdToEntity(state.Location));
        AddFact("player", "money_groats", value: state.MoneyGroats.ToString(CultureInfo.InvariantCulture));

        AddFact("person.matej", "profession", value: "hostinský");
        AddFact("person.matej", "location", "loc.inn.common");
        AddFact("person.matej", "works_at", "place.inn");

        AddFact("person.havel", "profession", value: "kupec");
        AddFact("person.havel", "location", "loc.market");
        AddFact("person.havel", "sells", "item.grain");
        AddFact("person.havel", "price:item.grain", value: state.GrainPriceGroats.ToString(CultureInfo.InvariantCulture));

        AddFact("place.inn", "contains_location", "loc.inn.room");
        AddFact("place.inn", "contains_location", "loc.inn.common");
        AddFact("place.inn", "contains_location", "loc.street.inn");
        AddFact("place.inn", "service_category", value: "food_and_drink");
        AddFact("place.inn", "ordinary_medieval_inn", value: "true");
        AddFact("place.inn", "offers", "service.inn.breakfast");

        AddFact("loc.inn.room", "part_of", "place.inn");
        AddFact("loc.inn.common", "part_of", "place.inn");
        AddFact("loc.street.inn", "adjacent_to", "loc.inn.common");
        AddFact("loc.inn.common", "adjacent_to", "loc.street.inn");
        AddFact("loc.inn.common", "adjacent_to", "loc.inn.room");
        AddFact("loc.inn.room", "adjacent_to", "loc.inn.common");

        AddFact("service.inn.breakfast", "offered_at", "place.inn");
        AddFact("service.inn.breakfast", "category", value: "breakfast");
        AddFact("service.inn.breakfast", "detail_status", value: "abstract");

        AddFact("group.market.townspeople", "location", "loc.market");
        AddFact("group.market.townspeople", "population_kind", value: "generic_people");
        AddFact("group.inn.patrons", "location", "loc.inn.common");
        AddFact("group.inn.patrons", "population_kind", value: "generic_people");
        AddFact("feature.market.stalls", "location", "loc.market");
        AddFact("feature.market.stalls", "count_class", value: "multiple");

        AddFact("object.chair.room", "location", "loc.inn.room");
        AddFact("object.chair.room", "movable", value: "true");
        AddFact("object.chair.room", "carryable", value: "true");

        AddFact("object.table.common", "location", "loc.inn.common");
        AddFact("object.chair.common", "location", "loc.inn.common");
        AddFact("object.chair.common", "movable", value: "true");
        AddFact("object.chair.common", "carryable", value: "true");
        AddFact("object.window.common", "location", "loc.inn.common");
        AddFact("object.window.common", "window", value: "true");

        AddFact("object.door.room", "location", "loc.inn.room");
        AddFact("object.door.room", "openable", value: "true");
        AddFact("object.door.room", "blockable", value: "true");
        AddFact("object.door.room", "connects", "loc.inn.common");

        AddFact("object.door.common", "location", "loc.inn.common");
        AddFact("object.door.common", "openable", value: "true");
        AddFact("object.door.common", "blockable", value: "true");
        AddFact("object.door.common", "connects", "loc.street.inn");

        KnowEntity("person.matej");
        KnowEntity("place.inn");
        KnowEntity("loc.market");
        KnowFact("person.matej", "profession", value: "hostinský");
        KnowFact("person.matej", "works_at", "place.inn");
    }

    public void SyncPlayerState(WorldState state)
    {
        ReplaceFact("player", "location", objectId: LocationIdToEntity(state.Location));
        ReplaceFact("player", "money_groats", value: state.MoneyGroats.ToString(CultureInfo.InvariantCulture));
        ReplaceFact("person.havel", "price:item.grain", value: state.GrainPriceGroats.ToString(CultureInfo.InvariantCulture));
    }

    public List<string> VisibleNames(LocationId location)
    {
        var loc = LocationIdToEntity(location);
        var result = NamesForFact("location", loc);
        if (location == LocationId.MarketSquare)
        {
            if (!result.Contains("tržní stánky")) result.Add("tržní stánky");
            if (!result.Contains("měšťané na trhu")) result.Add("měšťané na trhu");
        }
        return result;
    }

    public List<string> KnownNames()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT e.name
            FROM player_knowledge k
            JOIN world_entities e ON e.id = k.subject OR e.id = k.object_id
            ORDER BY e.name;
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<string>();
        while (reader.Read())
        {
            var value = reader.GetString(0);
            if (!list.Contains(value)) list.Add(value);
        }
        return list;
    }

    public string? ResolveEntityId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var n = TextNorm.Normalize(raw);

        if (n is "player" or "hrac" or "ja" or "já" || n.Contains("moje postava")) return "player";
        if (n.Contains("matej")) return "person.matej";
        if (n.Contains("havl") || n == "havel") return "person.havel";
        if (n.Contains("obil")) return "item.grain";
        if (n.Contains("piv")) return FindEntityByNameContains("piv");
        if (n.Contains("hostinc") || n.Contains("tri korun")) return "place.inn";
        if (n.Contains("namest") || n.Contains("trh")) return "loc.market";
        if (n.Contains("pokoj")) return "loc.inn.room";
        if (n.Contains("spolecensk") || n.Contains("mistnost")) return "loc.inn.common";
        if (n.Contains("ulic") || n.Contains("ven z hostince") || n == "ven") return "loc.street.inn";
        if (n.Contains("jideln") || n.Contains("menu") || n.Contains("nabidk")) return "place.inn";
        if (n.Contains("snidan")) return "service.inn.breakfast";
        if (n.Contains("okn") && n.Contains("spolec")) return "object.window.common";
        if (n.Contains("okn")) return "object.window.common";
        if (n.Contains("zidl") && n.Contains("spolec")) return "object.chair.common";
        if (n.Contains("zidl")) return "object.chair.room";
        if (n.Contains("dver") && (n.Contains("vchod") || n.Contains("hostinc"))) return "object.door.common";
        if (n.Contains("dver")) return "object.door.room";
        if (n.Contains("stan")) return "feature.market.stalls";
        if (n.Contains("host") || n.Contains("nekdo dalsi")) return "group.inn.patrons";
        if (n.Contains("mestan") || n.Contains("lid")) return "group.market.townspeople";

        return FindEntityByNameContains(n);
    }

    public string? CanonicalPersonName(string? raw)
    {
        var id = ResolveEntityId(raw);
        if (id is null || EntityType(id) != "Person") return null;
        return EntityName(id);
    }

    public bool IsVisible(string? raw, LocationId location)
    {
        var id = ResolveEntityId(raw);
        if (id is null) return false;
        var loc = FactObject(id, "location");
        return loc == LocationIdToEntity(location);
    }

    public LocationId? KnownLocationOf(string? raw)
    {
        var id = ResolveEntityId(raw);
        if (id is null) return null;

        // Current perception is allowed to reveal current location.
        var objective = FactObject(id, "location");
        if (objective is not null && objective == FactObject("player", "location"))
        {
            KnowFact(id, "location", objective);
            return EntityToLocationId(objective);
        }

        var known = KnowledgeObject(id, "location");
        return known is null ? null : EntityToLocationId(known);
    }

    public string Describe(string? raw, WorldState state)
    {
        var id = ResolveEntityId(raw);
        if (id is null) return "Takovou konkrétní entitu ve svých znalostech zatím nemáš.";

        if (IsVisible(raw, state.Location))
            KnowEntity(id);

        if (!KnowsEntity(id) && id != "player")
            return $"O entitě „{EntityName(id)}“ zatím nemáš dost spolehlivých znalostí.";

        var name = EntityName(id);
        var type = EntityType(id);
        var facts = KnownOrVisibleFacts(id, state);

        if (id == "player")
            return $"Jsi hráčská postava. Nacházíš se v lokaci {WorldData.LocationName(state.Location)} a máš {state.MoneyGroats} grošů.";

        var parts = new List<string> { $"{name} ({HumanType(type)})" };
        foreach (var fact in facts)
        {
            if (fact.Predicate == "profession" && fact.Value is not null)
                parts.Add($"povolání: {fact.Value}");
            else if (fact.Predicate == "location" && fact.ObjectId is not null)
                parts.Add($"lokace: {EntityName(fact.ObjectId)}");
            else if (fact.Predicate == "sells" && fact.ObjectId is not null)
                parts.Add($"prodává: {EntityName(fact.ObjectId)}");
        }

        return string.Join("; ", parts) + ".";
    }

    public string InventoryAnswer(WorldState state)
    {
        var carried = NamesForFact("carried_by", "player");
        return carried.Count == 0
            ? $"Nemáš evidovaný žádný nesený předmět. Máš {state.MoneyGroats} grošů."
            : $"U sebe máš: {string.Join(", ", carried)}. Peníze: {state.MoneyGroats} grošů.";
    }

    public string PerceptionAnswer(string? raw, WorldState state)
    {
        var id = ResolveEntityId(raw);
        if (id is null) return "Takovou entitu v aktuálním výřezu světa nerozpoznáváš.";
        return IsVisible(raw, state.Location)
            ? $"Ano, {EntityName(id)} je v tvé aktuální lokaci a můžeš ho/ji vnímat."
            : $"Ne, {EntityName(id)} v aktuální lokaci přímo nevnímáš.";
    }

    public string ContentsAnswer(LocationId location)
    {
        var names = VisibleNames(location);
        return names.Count == 0
            ? "V aktuální lokaci nemáš zaznamenané žádné konkrétní viditelné entity."
            : $"V aktuální lokaci vnímáš: {string.Join(", ", names)}.";
    }

    public string LocationDescription(LocationId location)
    {
        var locId = LocationIdToEntity(location);
        var contents = NamesForFact("location", locId)
            .Where(x => x != "ty")
            .ToList();

        var exits = ObjectNamesForFact(locId, "adjacent_to");
        var text = $"Jsi v lokaci {EntityName(locId)}.";
        if (contents.Count > 0)
            text += $" Vidíš zde: {string.Join(", ", contents)}.";
        if (exits.Count > 0)
            text += $" Z lokace se lze dostat do: {string.Join(", ", exits)}.";
        return text;
    }

    public string FurnitureAnswer(LocationId location)
    {
        var locId = LocationIdToEntity(location);
        var furniture = EntityNamesByTypeAndLocation("Furniture", locId);
        return furniture.Count == 0
            ? "V této lokaci zatím nemáš konkretizovaný žádný nábytek."
            : $"Z nábytku zde vidíš: {string.Join(", ", furniture)}.";
    }

    public string RoomsInBuildingAnswer(LocationId location)
    {
        var locId = LocationIdToEntity(location);
        var buildingId = FactObject(locId, "part_of") ?? (location is LocationId.InnRoom or LocationId.InnCommonRoom ? "place.inn" : null);
        if (buildingId is null)
            return "Pro aktuální lokaci není evidována nadřazená budova.";

        var rooms = ObjectNamesForFact(buildingId, "contains_location")
            .Where(x => !TextNorm.Normalize(x).Contains("ulice"))
            .ToList();
        return rooms.Count == 0
            ? "V budově zatím nejsou evidované další místnosti."
            : $"V budově jsou evidované tyto prostory: {string.Join(", ", rooms)}.";
    }

    public string ExitAnswer(LocationId location)
    {
        var locId = LocationIdToEntity(location);
        var exits = ObjectNamesForFact(locId, "adjacent_to");
        return exits.Count == 0
            ? "Z této lokace zatím není v grafu evidovaná cesta ven."
            : $"Z této lokace vede cesta do: {string.Join(", ", exits)}.";
    }

    public string ServiceCatalogAnswer(WorldState state)
    {
        var items = ObjectNamesForFact("place.inn", "offers");
        var beer = FindEntityByNameContains("pivo");
        if (beer is not null && !items.Contains(EntityName(beer)))
            items.Add(EntityName(beer));

        return items.Count == 0
            ? "Hostinec zatím nemá konkretizovanou žádnou nabídku."
            : $"Hostinec nabízí: {string.Join(", ", items)}.";
    }

    public string RelationshipAnswer(string? raw)
    {
        var id = ResolveEntityId(raw);
        if (id != "person.matej")
            return "K této osobě zatím nemáš evidovaný vztah.";

        return "Matěje znáš z několika návštěv. Vztah je zatím neutrálně přátelský; přesné číselné hodnoty vztahových dimenzí prototyp ještě neukládá.";
    }

    public string BreakfastDetailAnswer()
    {
        var id = ConcretizeBreakfastDetail();
        var parts = ObjectNamesForFact(id, "contains");
        return parts.Count == 0
            ? "Běžná snídaně zatím nemá konkretizované složení."
            : $"Běžná snídaně obsahuje: {string.Join(", ", parts)}.";
    }

    public string PropertyAnswer(string? raw, string? predicate, WorldState state)
    {
        var id = ResolveEntityId(raw);
        var p = TextNorm.Normalize(predicate ?? "");

        // Controlled lazy concretization: an ordinary medieval inn with food_and_drink
        // may concretize a requested ordinary beverage, but only once and persistently.
        if (id is null && TextNorm.Normalize(raw ?? "").Contains("piv") &&
            state.Location == LocationId.InnCommonRoom)
        {
            id = ConcretizeInnBeer();
        }

        if (id is null)
            return "Takovou entitu svět zatím nemá konkretizovanou a dostupná pravidla ji nyní neumějí bezpečně doplnit.";

        if (IsVisible(raw, state.Location) || id.StartsWith("offer."))
            KnowEntity(id);

        if (id == "person.havel" && (p.Contains("prodav") || p.Contains("zboz") || p.Contains("cena")))
        {
            KnowFact("person.havel", "sells", "item.grain");
            KnowFact("person.havel", "price:item.grain", value: FactValue("person.havel", "price:item.grain"));
            return $"Havel prodává obilí za {FactValue("person.havel", "price:item.grain")} groše za míru.";
        }

        if (id.StartsWith("offer.beer"))
        {
            var price = FactValue(id, "price_groats");
            return $"Ano. Hostinec U Tří korun nabízí pivo za {price} groš.";
        }

        if (id == "loc.inn.common" && (p.Contains("appearance") || p.Contains("vzhled") || p.Contains("popis")))
            return LocationDescription(LocationId.InnCommonRoom);

        if (id == "place.inn" && (p.Contains("offer") || p.Contains("nabid") || p.Contains("menu") || p.Contains("jideln")))
            return ServiceCatalogAnswer(state);

        if (id == "service.inn.breakfast" && (p.Contains("contain") || p.Contains("obsah") || p.Contains("slozen")))
            return BreakfastDetailAnswer();

        var value = FactValue(id, predicate ?? "");
        if (value is not null)
            return $"{EntityName(id)}: {predicate} = {value}.";

        return "Tuto vlastnost ze svého současného pohledu a znalostí spolehlivě neurčíš.";
    }

    public string ExistsHereAnswer(string? raw, WorldState state)
    {
        var n = TextNorm.Normalize(raw ?? "");

        if (n.Contains("piv") && state.Location == LocationId.InnCommonRoom)
        {
            var beer = ConcretizeInnBeer();
            KnowEntity(beer);
            return $"Ano. {EntityName(beer)} je součástí nabídky hostince.";
        }

        if (n.Contains("stan"))
            return state.Location == LocationId.MarketSquare
                ? "Ano. Na náměstí je více tržních stánků."
                : "V této lokaci tržní stánky neeviduješ.";

        var id = ResolveEntityId(raw);
        if (id is not null)
            return IsVisible(raw, state.Location)
                ? $"Ano. {EntityName(id)} je v aktuální lokaci."
                : $"Ne. {EntityName(id)} v aktuální lokaci není.";

        return "Z dostupného modelu světa to zatím nelze bezpečně určit.";
    }

    public string AvailableSocialContactAnswer(LocationId location)
    {
        if (location == LocationId.MarketSquare)
        {
            var npc = ConcretizeMarketNpc();
            KnowEntity(npc);
            return $"Ano. Mezi dosud anonymními měšťany se nyní konkretizovala postava {EntityName(npc)}. Od této chvíle je persistentní součástí světa.";
        }

        if (location == LocationId.InnCommonRoom)
            return "Ano. Kromě Matěje je ve společenské místnosti evidována generická skupina dalších hostů; konkrétní osoba by se vytvořila až při skutečném kontaktu.";

        return "V aktuální lokaci není dostupná další osoba k seznámení.";
    }

    public LocationId? ResolveDestination(string? raw, WorldState state)
    {
        var id = ResolveEntityId(raw);
        if (id is null) return null;

        if (EntityType(id) == "Location")
        {
            if (id == "loc.street.inn") return LocationId.MarketSquare;
            return EntityToLocationId(id);
        }

        if (EntityType(id) == "Business" && id == "place.inn")
            return LocationId.InnCommonRoom;

        var locationId = FactObject(id, "location");
        return locationId is null ? null : EntityToLocationId(locationId);
    }

    public int? OfferingPrice(string? raw, WorldState state)
    {
        var id = ResolveEntityId(raw);
        if (id is null && TextNorm.Normalize(raw ?? "").Contains("piv") &&
            state.Location == LocationId.InnCommonRoom)
            id = ConcretizeInnBeer();

        var value = id is null ? null : FactValue(id, "price_groats");
        return int.TryParse(value, out var price) ? price : null;
    }

    public string EnsureInnBeer() => ConcretizeInnBeer();

    public bool TryTake(string? raw, WorldState state, out string message)
    {
        var id = ResolveEntityId(raw);
        if (id is null)
        {
            message = "Takový předmět svět v aktuálním detailu nezná.";
            return false;
        }

        if (FactValue(id, "carryable") != "true")
        {
            message = $"{EntityName(id)} není podle současných vlastností přenositelný předmět.";
            return false;
        }

        if (FactObject(id, "location") != LocationIdToEntity(state.Location))
        {
            message = $"{EntityName(id)} není v tvé aktuální lokaci.";
            return false;
        }

        DeleteFacts(id, "location");
        ReplaceFact(id, "carried_by", objectId: "player");
        KnowEntity(id);
        message = $"Zvedl jsi předmět {EntityName(id)}.";
        return true;
    }

    public bool TryPut(string? raw, string? targetRaw, string? purpose, WorldState state, out string message)
    {
        var id = ResolveEntityId(raw);
        var target = ResolveEntityId(targetRaw);
        if (id is null || target is null)
        {
            message = "Předmět nebo cíl se nepodařilo ve světě jednoznačně určit.";
            return false;
        }

        if (FactObject(id, "carried_by") != "player")
        {
            message = $"{EntityName(id)} právě neneseš.";
            return false;
        }

        if (FactObject(target, "location") != LocationIdToEntity(state.Location))
        {
            message = $"{EntityName(target)} není v aktuální lokaci.";
            return false;
        }

        var normalizedPurpose = TextNorm.Normalize(purpose ?? "");
        if (normalizedPurpose.Contains("block") || normalizedPurpose.Contains("zablok"))
        {
            if (FactValue(target, "blockable") != "true")
            {
                message = $"{EntityName(target)} podle současného modelu nelze tímto způsobem blokovat.";
                return false;
            }
            ReplaceFact(id, "blocks", objectId: target);
        }

        DeleteFacts(id, "carried_by");
        ReplaceFact(id, "location", objectId: LocationIdToEntity(state.Location));
        ReplaceFact(id, "near", objectId: target);
        message = $"Umístil jsi {EntityName(id)} k objektu {EntityName(target)}.";
        return true;
    }

    public string OfferingName(string id) => EntityName(id);


    public string EntityNameFromRaw(string? raw) =>
        ResolveEntityId(raw) is { } id ? EntityName(id) : raw ?? "";

    private string ConcretizeInnBeer()
    {
        const string id = "offer.beer.threecrowns";
        if (EntityExists(id)) return id;

        if (FactValue("place.inn", "service_category") != "food_and_drink")
            throw new InvalidOperationException("Inn cannot concretize beverage without food_and_drink service.");

        AddEntity(id, "BeverageOffering", "pivo U Tří korun", "lazy:inn-service");
        AddFact(id, "offered_at", "place.inn");
        AddFact(id, "category", value: "beer");
        AddFact(id, "price_groats", value: "1");
        AddFact(id, "consumable", value: "true");
        return id;
    }

    private string ConcretizeBreakfastDetail()
    {
        const string breakfastId = "service.inn.breakfast";
        if (FactValue(breakfastId, "detail_status") == "concrete")
            return breakfastId;

        AddEntity("food.bread.threecrowns", "Food", "chléb", "lazy:breakfast");
        AddEntity("food.cheese.threecrowns", "Food", "sýr", "lazy:breakfast");
        AddFact(breakfastId, "contains", "food.bread.threecrowns");
        AddFact(breakfastId, "contains", "food.cheese.threecrowns");
        ReplaceFact(breakfastId, "detail_status", value: "concrete");
        return breakfastId;
    }

    private string ConcretizeMarketNpc()
    {
        const string id = "person.ondrej.kozeluh";
        if (EntityExists(id)) return id;

        if (!EntityExists("group.market.townspeople"))
            throw new InvalidOperationException("Macro population bucket is missing.");

        AddEntity(id, "Person", "Ondřej", "lazy:market-population");
        AddFact(id, "profession", value: "koželuh");
        AddFact(id, "location", "loc.market");
        AddFact(id, "origin_bucket", "group.market.townspeople");
        return id;
    }

    private IEnumerable<GraphFact> KnownOrVisibleFacts(string id, WorldState state)
    {
        var visible = FactObject(id, "location") == LocationIdToEntity(state.Location);
        using var cmd = _db.CreateCommand();
        cmd.CommandText = visible
            ? "SELECT predicate, object_id, value FROM world_facts WHERE subject=$s;"
            : "SELECT predicate, object_id, value FROM player_knowledge WHERE subject=$s;";
        cmd.Parameters.AddWithValue("$s", id);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            yield return new GraphFact(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private void KnowEntity(string id) =>
        KnowFact(id, "known_entity", value: "true");

    private bool KnowsEntity(string id) =>
        ScalarInt("SELECT COUNT(*) FROM player_knowledge WHERE subject=$s;", ("$s", id)) > 0;

    private void KnowFact(string subject, string predicate, string? objectId = null, string? value = null)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO player_knowledge(subject,predicate,object_id,value)
            VALUES($s,$p,$o,$v);
            """;
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        cmd.Parameters.AddWithValue("$o", (object?)objectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$v", (object?)value ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private string? KnowledgeObject(string subject, string predicate)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT object_id FROM player_knowledge WHERE subject=$s AND predicate=$p AND object_id IS NOT NULL LIMIT 1;";
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        return cmd.ExecuteScalar() as string;
    }

    private void AddEntity(string id, string type, string name, string source)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO world_entities(id,type,name,source) VALUES($i,$t,$n,$s);";
        cmd.Parameters.AddWithValue("$i", id);
        cmd.Parameters.AddWithValue("$t", type);
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$s", source);
        cmd.ExecuteNonQuery();
    }

    private void AddFact(string subject, string predicate, string? objectId = null, string? value = null)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO world_facts(subject,predicate,object_id,value)
            VALUES($s,$p,$o,$v);
            """;
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        cmd.Parameters.AddWithValue("$o", (object?)objectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$v", (object?)value ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }


    private void DeleteFacts(string subject, string predicate)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM world_facts WHERE subject=$s AND predicate=$p;";
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        cmd.ExecuteNonQuery();
    }

    private void ReplaceFact(string subject, string predicate, string? objectId = null, string? value = null)
    {
        using var tx = _db.BeginTransaction();
        using var del = _db.CreateCommand();
        del.Transaction = tx;
        del.CommandText = "DELETE FROM world_facts WHERE subject=$s AND predicate=$p;";
        del.Parameters.AddWithValue("$s", subject);
        del.Parameters.AddWithValue("$p", predicate);
        del.ExecuteNonQuery();

        using var ins = _db.CreateCommand();
        ins.Transaction = tx;
        ins.CommandText = "INSERT INTO world_facts(subject,predicate,object_id,value) VALUES($s,$p,$o,$v);";
        ins.Parameters.AddWithValue("$s", subject);
        ins.Parameters.AddWithValue("$p", predicate);
        ins.Parameters.AddWithValue("$o", (object?)objectId ?? DBNull.Value);
        ins.Parameters.AddWithValue("$v", (object?)value ?? DBNull.Value);
        ins.ExecuteNonQuery();
        tx.Commit();
    }

    private List<string> NamesForFact(string predicate, string objectId)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT e.name
            FROM world_facts f
            JOIN world_entities e ON e.id=f.subject
            WHERE f.predicate=$p AND f.object_id=$o
            ORDER BY e.name;
            """;
        cmd.Parameters.AddWithValue("$p", predicate);
        cmd.Parameters.AddWithValue("$o", objectId);
        using var reader = cmd.ExecuteReader();
        var result = new List<string>();
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private List<string> ObjectNamesForFact(string subject, string predicate)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT e.name
            FROM world_facts f
            JOIN world_entities e ON e.id=f.object_id
            WHERE f.subject=$s AND f.predicate=$p AND f.object_id IS NOT NULL
            ORDER BY e.name;
            """;
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        using var reader = cmd.ExecuteReader();
        var result = new List<string>();
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private List<string> EntityNamesByTypeAndLocation(string type, string locationId)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT e.name
            FROM world_entities e
            JOIN world_facts f ON f.subject=e.id
            WHERE e.type=$t AND f.predicate='location' AND f.object_id=$l
            ORDER BY e.name;
            """;
        cmd.Parameters.AddWithValue("$t", type);
        cmd.Parameters.AddWithValue("$l", locationId);
        using var reader = cmd.ExecuteReader();
        var result = new List<string>();
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private string? FindEntityByNameContains(string normalized)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT id,name FROM world_entities;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            if (TextNorm.Normalize(reader.GetString(1)).Contains(normalized, StringComparison.Ordinal))
                return reader.GetString(0);
        return null;
    }

    private bool EntityExists(string id) =>
        ScalarInt("SELECT COUNT(*) FROM world_entities WHERE id=$i;", ("$i", id)) > 0;

    private string EntityName(string id)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT name FROM world_entities WHERE id=$i;";
        cmd.Parameters.AddWithValue("$i", id);
        return cmd.ExecuteScalar() as string ?? id;
    }

    private string EntityType(string id)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT type FROM world_entities WHERE id=$i;";
        cmd.Parameters.AddWithValue("$i", id);
        return cmd.ExecuteScalar() as string ?? "Unknown";
    }

    private string? FactObject(string subject, string predicate)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT object_id FROM world_facts WHERE subject=$s AND predicate=$p AND object_id IS NOT NULL LIMIT 1;";
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        return cmd.ExecuteScalar() as string;
    }

    private string? FactValue(string subject, string predicate)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT value FROM world_facts WHERE subject=$s AND predicate=$p AND value IS NOT NULL LIMIT 1;";
        cmd.Parameters.AddWithValue("$s", subject);
        cmd.Parameters.AddWithValue("$p", predicate);
        return cmd.ExecuteScalar() as string;
    }

    private int ScalarInt(string sql, params (string Name, object Value)[] args)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        foreach (var arg in args) cmd.Parameters.AddWithValue(arg.Name, arg.Value);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
    }

    private void Exec(string sql)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string LocationIdToEntity(LocationId id) => id switch
    {
        LocationId.InnRoom => "loc.inn.room",
        LocationId.InnCommonRoom => "loc.inn.common",
        _ => "loc.market"
    };

    private static LocationId? EntityToLocationId(string id) => id switch
    {
        "loc.inn.room" => LocationId.InnRoom,
        "loc.inn.common" => LocationId.InnCommonRoom,
        "loc.market" => LocationId.MarketSquare,
        _ => null
    };

    private static string HumanType(string type) => type switch
    {
        "Person" => "osoba",
        "Business" => "podnik",
        "Location" => "místo",
        "Commodity" => "zboží",
        "BeverageOffering" => "nabídka nápoje",
        _ => type
    };

    public void Dispose() => _db.Dispose();

    private sealed record GraphFact(string Predicate, string? ObjectId, string? Value);
}
