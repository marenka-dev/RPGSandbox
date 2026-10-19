# RPGSandbox – Rabenfeld vertical slice v0.2 Natural Input

První souvislá hratelná konzolová smyčka s hybridní interpretací přirozeného českého vstupu.

## Co prototyp umí

- lokální kanonický stav světa v SQLite;
- herní čas postupující pouze podle akcí;
- tři lokace: pokoj U Tří korun, společenská místnost hostince a rabenfeldské náměstí;
- Matěje s jednoduchou perzistentní pamětí;
- Havla a kanonickou cenu obilí 4 groše za míru;
- **přímé systémové dotazy bez AI**: kde jsem, koho znám, kolik mám peněz, kolik je hodin;
- **deterministický parser** pro běžné a bezpečně rozpoznatelné akce;
- **Qwen3 8B jako fallback intent interpreter** pro volnější české formulace;
- aktivní konverzační cíl pro přirozené navazování typu „Jak se máš?“;
- Engine Contract pro každou přijatou akci;
- **Gemma 3 12B pouze jako renderer prózy**;
- historii přijatých akcí v tabulce `world_events`.

AI intent interpreter nikdy nerozhoduje výsledek akce. Vrací pouze návrh `intent + target`; engine ho validuje podle lokace a povolených akcí. Renderer nikdy nezapisuje do SQLite.

## Spuštění

Z kořene repozitáře:

```powershell
cd prototype\vertical-slice
dotnet restore
dotnet run -- --model gemma3:12b --intent-model qwen3:8b
```

Pokud oba modely už máš v Ollamě, není potřeba nic dalšího stahovat.

Databáze zůstává `rabenfeld-v01.db`, takže předchozí save lze dál použít.

Jen bez intent AI:

```powershell
dotnet run -- --model gemma3:12b --no-intent-ai
```

Bez jakékoliv AI:

```powershell
dotnet run -- --no-ai
```

## Přirozené příklady

```text
kde jsem?
koho znám?
kolik mám peněz?
podívám se po pokoji
podívám se z okna
sejdu dolů
jak se máš?
máš něco k jídlu?
poprosím Matěje o snídani
je dnes ve městě rušno?
pozdravím Havla
prohlédnu si Havla
zkusím ho přesvědčit, aby mi prodal obilí za polovinu
```

Parser nejdřív použije rychlá bezpečná pravidla. Teprve pokud jim vstup neodpovídá, Qwen vrátí strukturovaný návrh akce. Pokud ani ten nejde bezpečně validovat, svět se vůbec nezmění.

## Architektonický princip v0.2

**AI chápe jazyk. Engine chápe svět.**

Pipeline:

```text
hráčův text
  ↓
přímý dotaz / deterministický parser
  ↓ pokud nerozpozná
Qwen intent interpreter → pouze JSON intent + target
  ↓
engine validace a kanonický výsledek
  ↓
Engine Contract
  ↓
Gemma renderer
  ↓
text hráči
```

Další krok bude vyhodnotit playtest v0.2, zejména přesnost volného vstupu, latenci fallback interpreteru a chování navazujících dialogů.
