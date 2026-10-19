# RPGSandbox – Rabenfeld vertical slice v0.4 World Graph & Concretization

v0.4 propojuje sémantické akce s persistentním **World Graphem**. Cílem je, aby engine nereagoval na konkrétní věty ručně napsanými odpověďmi, ale pracoval s obecnými entitami, vlastnostmi a vztahy.

## Základní vrstvy

1. **Objektivní svět** – kanonické entity a fakta.
2. **Lazy detail** – dosud neurčené detaily lze konkretizovat pouze v mezích už existujícího světa.
3. **Znalosti hráče** – samostatná vrstva toho, co hráč skutečně ví.
4. **Semantic Actions / Queries** – jazyková vrstva nad světem.
5. **Scene Contract** – Gemma pouze odvypráví výsledek enginu.

## World Graph

SQLite nyní obsahuje mimo původní save také:

```text
world_entities
world_facts
player_knowledge
```

Příklady objektivních vztahů:

```text
Matěj --profession--> hostinský
Matěj --location--> U Tří korun
Havel --profession--> kupec
Havel --sells--> obilí
Havel --price:obilí--> 4
židle --carryable--> true
dveře --blockable--> true
```

Objektivní fakt automaticky neznamená, že jej hráč zná.

## Lazy concretization

v0.4 obsahuje dva malé experimenty.

### Nabídka hostince

Hostinec má od začátku makro-vlastnost:

```text
service_category = food_and_drink
ordinary_medieval_inn = true
```

Pokud hráč poprvé řeší pivo, engine může z tohoto rámce konkretizovat persistentní nabídku:

```text
pivo U Tří korun
category = beer
price_groats = 1
offered_at = U Tří korun
```

Od této chvíle je existence i cena kanonická a uložená v databázi. AI cenu nevymýšlí.

### Dosud anonymní obyvatel

Tržiště má makro-vrstvu obyvatel. Při požadavku na nový sociální kontakt může concretizer vytvořit persistentní NPC z tohoto populačního rámce. Prototyp používá deterministický první příklad, aby šlo ověřit perzistenci.

## Obecnější dotazy

Nové query operace zahrnují také:

```text
inventory
describe
can_perceive
contents
```

Takže věty jako:

```text
Co mám u sebe?
Kdo je Havel?
Vidím Havla?
Co vidím?
```

už míří na World Graph místo na ručně vybranou odpověď.

## Affordances

v0.4 zavádí první dvě fyzické operace:

```text
take
put
```

Předměty mají schopnosti jako:

```text
židle --carryable--> true
dveře --blockable--> true
```

Engine tedy může rozhodovat podle vlastností objektů, ne podle přesné věty hráče.

## Spuštění

```powershell
git pull
cd prototype\vertical-slice
dotnet restore
dotnet run -- --model gemma3:12b --intent-model qwen3:8b
```

Doporučený čistý test:

```text
reset
```

Pak zkus přirozeně například:

```text
Co mám u sebe?
Jak vypadám?
Kde je Matěj?
Půjdu za Havlem.
Vidím Havla?
Kdo je Havel?
Půjdu za Matějem.
Prodávají zde pivo a kolik stojí?
Dám si pivo.
Kolik mám peněz?
Půjdu do pokoje.
Vezmu židli.
Co mám u sebe?
```

## Co v0.4 ještě není

World Graph je zatím malý prototypový model, ne kompletní obecný simulační svět. Concretizer má pouze několik bezpečných pravidel. Dlouhodobě nesmí generovat detail libovolně; vždy musí vycházet z makrosvěta, historie, kultury, ekonomiky, populace a dalších již kanonických omezení.

Zásadní pravidlo:

> **Otázka hráče nevytváří svět. Pouze může vyvolat konkretizaci detailu, který byl dosud neurčený, ale musí být slučitelný s již existujícím světem.**
