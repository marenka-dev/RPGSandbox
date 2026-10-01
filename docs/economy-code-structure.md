# Návrh budoucí struktury ekonomického modulu

Tento dokument je technologicky nezávislé rozdělení odpovědností **nikoli hotová implementace**. Skutečné názvy souborů, programovací jazyk, databázi a API určí schválená architektura simulačního jádra.

```text
simulation/
  economy/
    models/          # Entity, podnik, vlastnictví, suroviny, trh, transakce
    production/      # Výrobní pravidla, kapacity, spotřeba vstupů
    consumption/     # Potřeby postav, podniků a agregovaných regionů
    markets/         # Regionální nabídka, poptávka a cenotvorba
    trade/           # Transakce, trasy a fyzická přeprava
    finance/         # Příjmy, výdaje, závazky a majetkové převody
    simulation/      # Aktualizace ekonomiky podle úrovně detailu
    integration/     # Ověřená rozhraní k Event Enginu, NPC a frakcím
    persistence/     # Ukládání a obnova kanonických dat
    tests/           # Konzistence zásob, transakcí a scénáře Rabenfeldu
```

## Doporučená oddělení

1. **Doménový model** nemá záviset na AI ani uživatelském rozhraní.
2. **Deterministická simulace** pro stejný seed, počáteční stav a posloupnost akcí produkuje reprodukovatelné výsledky v rozsahu možností zvolené architektury.
3. **Události**: ekonomický modul přijímá již ověřené události a navrhuje možné hospodářské důsledky; jejich zápis řídí společný Event Engine.
4. **AI rozhraní** poskytuje relevantní, oprávněně dostupný kontext a přijímá strukturované návrhy; AI nemění databázi přímo.
5. **Úroveň detailu**: lokální simulace podniků a agregovaná simulace regionů sdílejí konzistentní bilanční pravidla.
6. **Testování**: žádné záporné zásoby bez výslovně modelovaného mechanismu, žádné nevysvětlené převody majetku či peněz; každá důležitá změna má dohledatelnou příčinu.

## Neuzavřené technické otázky

- programovací jazyk a běhové prostředí;
- lokální/webová/hybridní architektura;
- SQLite pro prototyp či jiná databáze;
- časový krok a napojení na více úrovní detailu;
- přesný mechanismus cen, finanční evidence a více měn;
- rozhraní pro AI návrhy událostí a jejich validaci.

Neimplementovat před finálním rozhodnutím o architektuře. Podkladem pro implementaci je [ekonomický model](economy-model.md).
