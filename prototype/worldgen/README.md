# World Generator v0.2 – Procedural Region

v0.2 je první verze, ve které **seed skutečně mění vznik regionu**. Rabenfeld, Linden ani Hvozdec už nejsou pevně vložený obsah worldgenu.

## Co řídí generování

Výsledný svět vzniká z:

```text
WorldConfig
+ Seed
+ generační pravidla
+ později historická simulace
```

### WorldConfig

Aktuálně obsahuje například:

- éru a technologickou úroveň;
- realismus a fantasy;
- rozsah simulace;
- klimatický bias;
- politickou stabilitu;
- hloubku ekonomiky;
- počet let historie.

Hráč později nebude nastavovat všechny interní hodnoty. Má volit charakter světa a engine z něj odvodí podrobné parametry.

### Seed

Seed vybírá konkrétní variantu uvnitř těchto pravidel:

- vlhkost;
- teplotu;
- nadmořský charakter;
- úrodnost;
- lesnatost;
- dostupnost vody;
- obchodní průchodnost;
- zdroje;
- velikost populace;
- počet a typ sídel;
- názvy;
- podniky;
- část sociální struktury.

Stejný config + stejný seed musí vytvořit stejný svět.

## Příčinná pipeline v0.2

```text
seed + WorldConfig
        ↓
teplota / vlhkost / reliéf
        ↓
klima
        ↓
úrodnost / lesnatost / voda / obchodní průchodnost
        ↓
přírodní zdroje
        ↓
nosná kapacita regionu a populace
        ↓
počet a typ sídel
        ↓
ekonomické specializace sídel
        ↓
podniky podle zdrojů a populace
        ↓
rodiny a lehké persistentní NPC
        ↓
vlastníci a pracovníci
        ↓
lokální frakce a vztahy
        ↓
základní historie
        ↓
startovní scéna z vygenerovaného regionu
```

Důležitý rozdíl proti v0.1:

Mlýn už nevzniká proto, že je v kódu „Lindenský mlýn“. Vzniká pouze tehdy, když má sídlo dost obyvatel, region produkuje obilí a je dostupná voda.

Podobně hostince, pekárny, kovárny, dřevorubecké provozy a obchodní podniky vycházejí z prostředí.

## Spuštění

```powershell
cd prototype\worldgen
dotnet run -- --seed 111
dotnet run -- --seed 222
dotnet run -- --seed 333
```

Můžeš také ovlivnit charakter regionu:

```powershell
dotnet run -- --seed 111 --climate cold
dotnet run -- --seed 111 --climate dry
dotnet run -- --seed 111 --climate wet
dotnet run -- --seed 111 --climate mountain
```

Politická stabilita:

```powershell
dotnet run -- --seed 111 --politics low
```

Aktuální hodnoty jsou prototypové a budou se dál kalibrovat.

## Co sledovat při testu

Porovnej zejména:

- název regionu;
- klima a terén;
- úrodnost, lesnatost, vodu a obchodní průchodnost;
- přírodní zdroje;
- populaci regionu;
- počet a velikost sídel;
- ekonomické specializace;
- vzniklé podniky;
- frakce;
- startovní scénu.

Různé seedy už mají produkovat **skutečně odlišné regiony**, ne stejnou šablonu s jiným číslem.

## Co v0.2 ještě není

Historie je zatím spíš odvozená kronika a ještě plně nemění současný stav světa. Populace je reprezentována jen omezenou sadou důležitějších persistentních rodin a NPC, ne všemi obyvateli. Geografie zatím není skutečná 2D mapa.

Další důležitý krok bude spojit **historickou simulaci se současným stavem** a rozhodnout, jak reprezentovat prostor regionu tak, aby umístění řek, cest, sídel a zdrojů nebylo pouze seznam vlastností.
