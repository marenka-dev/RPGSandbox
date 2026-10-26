# RPGSandbox – Rabenfeld vertical slice v0.5 Location Graph, Services & Knowledge Routing

v0.5 rozšiřuje World Graph o **topologii lokací, obsah místností, služby podniků a směrování znalostí NPC**. Cíl není přidávat další pevné věty, ale dát enginu bohatší strukturu světa, nad kterou lze klást obecné otázky.

## Co je nové

### Location Graph

Hostinec už není jen jedna lokace. World Graph zná vazby:

```text
U Tří korun
  contains_location -> společenská místnost
  contains_location -> pokoj

společenská místnost
  adjacent_to -> pokoj
  adjacent_to -> ulice před hostincem

pokoj
  adjacent_to -> společenská místnost
```

Lokace obsahují konkrétní objekty a nábytek, například stůl, židle, okno a dveře.

Díky tomu mohou dotazy typu:

```text
Jak vypadá místnost?
Jaký je zde nábytek?
Jsou v budově další místnosti?
Jsou zde dveře?
Jsou zde okna?
Jak se dostanu z hostince?
```

pracovat nad grafem světa místo nad ručně napsanou odpovědí.

### Services

Hostinec má obecné služby:

```text
U Tří korun --service_category--> food_and_drink
U Tří korun --offers--> běžná snídaně
```

Běžná snídaně může zpočátku existovat pouze jako abstraktní nabídka. Teprve při dotazu na její složení ji engine bezpečně konkretizuje a výsledek uloží natrvalo.

Pivo funguje stejným principem lazy concretization: AI cenu ani existenci neurčuje.

### Knowledge routing NPC

Matěj už není jen sada několika ručně napsaných odpovědí. Pokud se hráč ptá na služby hostince, Matějova odpověď se skládá z World Graphu hostince.

Například:

```text
Co mi můžeš nabídnout?
Máte jídelní lístek?
Co obsahuje běžná snídaně?
```

se směrují na fakta podniku a jeho nabídku.

### Dialog má přednost v aktivní konverzaci

Pokud hráč právě mluví s Matějem, věta:

```text
Jak se dnes máš?
```

se interpretuje jako dialogová otázka Matějovi, nikoli jako obecný query dotaz do World Graphu.

## Spuštění

```powershell
git pull
cd prototype\vertical-slice
dotnet restore
dotnet run -- --model gemma3:12b --intent-model qwen3:8b
```

Pro čistý test v0.5 použij:

```text
reset
```

## Doporučený playtest

```text
Rozhlédnu se po místnosti.
Jak vypadá místnost?
Jaký je zde nábytek?
Jsou v budově i další místnosti?
Jsou zde dveře?
Jsou zde okna?
Jak se dostanu z hostince?
Pozdravím Matěje.
Jak se dnes máš?
Co mi můžeš nabídnout?
Máte jídelní lístek?
Co obsahuje běžná snídaně?
Prodáváte pivo?
Kolik stojí?
```

Pak pokračuj úplně volně.

## Architektonický smysl

v0.5 ověřuje, že pružnost hry má vyrůstat z bohatého modelu světa:

**entity + vztahy + služby + affordances + znalosti**

nikoliv z nekonečného seznamu textových intentů.
