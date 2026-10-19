# RPGSandbox – Rabenfeld vertical slice v0.3 Semantic Actions

Vertical slice už nepoužívá rostoucí seznam konkrétních intentů typu `AskFood`, `AskSecret` nebo `BargainHalf`. Hráčův text se převádí na malý obecný sémantický jazyk, který engine validuje proti skutečnému stavu světa.

## Hlavní princip

> **AI navrhuje význam a plán. Engine rozhoduje, co je ve světě možné a co se skutečně stalo.**

Příklad:

```text
Hráč: Tak za 3?
```

se v kontextu rozhovoru s Havlem může převést na:

```json
{
  "kind": "action",
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

Engine následně ověří, že Havel je dostupný, že skutečně prodává obilí a jaká je kanonická cena. Až potom určí výsledek.

## Obecné operace v0.3

Action operace:

```text
observe
inspect
move_to
approach
speak
ask
tell
request
offer
wait
```

Query operace:

```text
current_location
time
money
known_people
visible_people
location_of
exists_here
property
available_social_contact
```

Nejde o konečný seznam celé hry. Smyslem je, aby operace byly **obecné a skládací**. Nová formulace hráče nemá vyžadovat nový intent, pokud její význam lze vyjádřit existujícími operacemi a parametry.

## World Query

Otázky hráče se pokud možno nepřekládají na pevně napsanou odpověď, ale na dotaz nad stavem a znalostmi světa.

Například:

```text
Kde je Matěj?
→ kind=query, op=location_of, subject=Matěj

Jsou tu další stánky?
→ kind=query, op=exists_here, subject=stánky

Můžu se s někým seznámit?
→ kind=query, op=available_social_contact
```

Engine odpovídá jen z toho, co hráč skutečně ví nebo může v aktuální lokaci zjistit.

## Kontext rozhovoru

Engine si drží jednoduchý kontext:

- aktuální konverzační cíl;
- aktuální předmět hovoru.

Díky tomu může:

```text
Promluvím s Havlem.
To je moc drahé.
Tak za 3?
```

znamenat postupně rozhovor s Havlem → téma obilí/cena → nabídku 3 grošů, bez nutnosti opakovat jméno ani předmět.

## Spuštění

```powershell
git pull
cd prototype\vertical-slice
dotnet restore
dotnet run -- --model gemma3:12b --intent-model qwen3:8b
```

Pro nový playtest doporučujeme ve hře:

```text
reset
```

## Co zkoušet

Nesnaž se hledat správný příkaz. Zkoušej věty jako:

```text
Kde je Matěj?
Půjdu za Havlem.
Co prodáváš?
To je moc drahé.
Tak za 3?
Jsou tu i jiné stánky?
Můžu se s někým seznámit?
Co o Matějovi vlastně vím?
Počkám deset minut.
```

A klidně i něco, co engine ještě neumí:

```text
Vezmu židli a zablokuju s ní dveře.
```

Správné chování není „vymyslet výsledek“, ale bezpečně říct, že význam sice může být pochopen, ale engine zatím nemá fyzikální pravidla potřebná k jeho provedení.

## Důležité omezení prototypu

v0.3 ještě není obecný simulační engine. Má jen první malý slovník operací a malý datový model Rabenfeldu. Cíl této verze je ověřit, že **počet přirozených vět hráče nemusí růst společně s počtem ručně programovaných intentů**.

Renderer Gemma navíc dostává explicitní `CANONICAL LOCATION`, aby nevytvářel nové interiéry typu „Havlův krám“, pokud engine stále eviduje hráče na náměstí.
