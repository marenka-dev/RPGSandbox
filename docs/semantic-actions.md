# Sémantické akce a dotazy nad světem

Stav: schválený architektonický směr.

## Problém

Hráč má mít možnost napsat prakticky libovolnou přirozenou větu. Není realistické ani žádoucí vytvářet samostatný pevný intent pro každou otázku a akci.

Nevhodný směr:

```text
AskFood
AskSecret
AskWhereMatej
AskWhereHavel
BargainHalf
BargainThreeGroats
...
```

Takový systém by pouze skrýval klasický parser příkazů pod AI vrstvou.

## Řešení

AI převádí jazyk na **obecný sémantický příkaz**. Engine zná omezenou sadu elementárních operací a datový model světa.

Minimální struktura:

```json
{
  "kind": "action | query",
  "op": "offer",
  "target": "Havel",
  "subject": "obilí",
  "predicate": null,
  "text": null,
  "args": {
    "price": "3"
  }
}
```

### Zásada

**AI chápe jazyk. Engine chápe svět.**

AI smí:
- určit význam věty;
- identifikovat cíl, předmět, vlastnost a parametry;
- rozložit budoucí složitější požadavek na elementární kroky.

AI nesmí:
- rozhodnout úspěch akce;
- vytvořit neexistující postavu či předmět;
- změnit kanonický stav;
- doplnit chybějící cenu, motivaci nebo jiný fakt, který hráč neřekl a engine ho neposkytl.

## Action DSL

První pracovní atomické operace:

- `observe`
- `inspect`
- `move_to`
- `approach`
- `speak`
- `ask`
- `tell`
- `request`
- `offer`
- `wait`

Pozdější kandidáti:

- `take`
- `put`
- `give`
- `use`
- `open`
- `close`
- `follow`
- `search`
- `buy`
- `sell`
- `attack`
- `help`
- `work`

Operace nesmí být příliš konkrétní. Například `offer(price=3)` je vhodnější než `BargainThreeGroats`.

## Query DSL

Dotazy se provádějí nad modelem světa a znalostí hráče, ne nad pevně připravenými odpověďmi.

První pracovní operace:

- `current_location`
- `time`
- `money`
- `known_people`
- `visible_people`
- `location_of(subject)`
- `exists_here(subject)`
- `property(subject, predicate)`
- `available_social_contact`

Dlouhodobě má query vrstva fungovat nad obecnými vztahy a vlastnostmi:

```text
entity --predicate--> value/entity
```

Například:

```text
Matěj --profession--> hostinský
Matěj --location--> U Tří korun
Havel --sells--> obilí
obilí --price_at(Havel)--> 4 groše/míra
```

Dotazy se vždy filtrují znalostním modelem hráče. Existence pravdy v databázi automaticky neznamená, že ji hráč zná.

## Kontext a elipsa

Přirozená konverzace často vynechává informace:

```text
Havel: Čtyři groše za míru.
Hráč: To je moc drahé.
Hráč: Tak za tři?
```

Interpreter proto dostává:
- aktuální konverzační cíl;
- aktuální předmět hovoru;
- aktuální lokaci;
- viditelné entity;
- známé entity.

Z toho může bezpečně doplnit, že „za tři“ znamená nabídku Havlovi za obilí. Nesmí ale vymyslet konkrétní cenu ve větě „to je moc drahé“.

## Složené akce

Budoucí vrstva může vytvořit Action Plan:

```text
Vezmu židli a zablokuju s ní dveře.
```

```json
[
  {"op":"take","subject":"židle"},
  {"op":"put","subject":"židle","target":"dveře","args":{"purpose":"obstruction"}}
]
```

Engine každý krok ověří:
- existuje objekt?
- je dosažitelný?
- může jej postava nést?
- je cílové místo dostupné?
- podporuje fyzikální model takové použití?

Pokud některý krok nejde provést, engine nevymyslí úspěch.

## Lazy concretization

Generické davy a populace nemusí být od začátku reprezentované plnými NPC profily.

Například náměstí může obsahovat:
- několik obchodníků;
- několik desítek měšťanů;
- povozy a stánky.

Pokud hráč skutečně osloví dosud anonymní osobu, engine může v tom okamžiku vytvořit persistentní NPC se stabilním ID a doplnit ho způsobem konzistentním s již simulovaným světem. Od té chvíle je tato osoba kanonická a trvalá.

Tím lze spojit rozsáhlý svět s rozumnými nároky na simulaci a úložiště.

## Narativní výstup

Po provedení sémantické akce engine vytvoří kanonický výsledek a Scene/Engine Contract. Narativní model pouze renderuje ověřený výsledek.

Pipeline:

```text
přirozený vstup
    ↓
semantic interpreter
    ↓
SemanticCommand / ActionPlan
    ↓
engine validation
    ↓
simulace a kanonický výsledek
    ↓
Scene Contract
    ↓
narativní renderer
```

Tento model je základem požadované hráčské svobody bez přenesení autority nad světem na AI.
