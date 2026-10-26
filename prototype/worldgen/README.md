# World Generator v0.3 – Spatial Region

v0.3 přidává skutečnou prostorovou vrstvu regionu. Region už není jen seznam vlastností; skládá se z konkrétních buněk mapy se souřadnicemi a lokálními vlastnostmi.

## Co je nové

### Prostorová mapa regionu

Každý region má mřížku buněk. Každá buňka má vlastní:

- souřadnice X/Y;
- nadmořský charakter;
- vlhkost;
- teplotu;
- úrodnost;
- lesnatost;
- informaci o řece;
- typ terénu;
- lokální zdroje.

Řeka už není pouze regionální vlastnost. Prochází konkrétními buňkami mapy.

### Sídla mají konkrétní polohu

Sídlo vzniká jen na vhodné buňce a dostává souřadnice.

Výběr místa bere v úvahu například:

- úrodnost;
- vodu;
- reliéf;
- lokální zdroje;
- regionální obchodní průchodnost.

Sídla jsou od sebe prostorově oddělená a následně propojena cestami.

### Lokální zdroje místo globálních

Ekonomická specializace sídla se už neodvozuje z celého regionu.

Sídlo čerpá pouze zdroje dostupné ve svém okolí.

Příklad:

```text
vesnice u řeky + obilná půda
→ obilí + voda
→ mlýn + zemědělství

osada u lesa
→ dřevo
→ dřevorubectví + tesařství

sídlo u rudonosné vrchoviny
→ železná ruda
→ kovárna
```

### Cesty

Worldgen vytvoří základní síť cest mezi sídly. Každá cesta má:

- počáteční sídlo;
- cílové sídlo;
- délku v buňkách;
- konkrétní trasu přes mapu.

v0.3 zatím používá jednoduchou přímou trasu. Později se cesta bude vyhýbat nevhodnému terénu a využívat brody, průsmyky a další přirozené koridory.

### Populace je explicitně rozdělená

Regionální populace už není nevysvětlený rozdíl mezi součtem sídel a celkem.

Je rozdělena na:

```text
hlavní sídla
rozptýlené venkovské domácnosti
malé osady
samoty a izolovaná místa
```

Tím má engine explicitní zdroj budoucích domácností, pracovní síly a drobných lokalit.

### Oddělené RNG streamy

Jednotlivé části worldgenu mají vlastní deterministický random stream:

```text
root seed
├─ geography
├─ settlements
├─ economy
├─ population
├─ politics
├─ history
└─ scene
```

Změna politické stability proto nemá měnit geografii, názvy sídel nebo tok řeky.

## Spuštění

```powershell
cd prototype\worldgen

dotnet run -- --seed 111
dotnet run -- --seed 222
dotnet run -- --seed 333
```

Důležitý regresní test:

```powershell
dotnet run -- --seed 111
dotnet run -- --seed 111 --politics low
```

Geografie, souřadnice sídel a základní mapa mají zůstat stejné. Změnit se má politická vrstva a případně historie, která na ní závisí.

Klimatický bias naopak geografii měnit smí:

```powershell
dotnet run -- --seed 111 --climate dry
dotnet run -- --seed 111 --climate wet
dotnet run -- --seed 111 --climate mountain
```

## Co sledovat ve výstupu

- rozměr mapy;
- počet říčních buněk;
- souřadnice sídel;
- lokální zdroje jednotlivých sídel;
- rozdílné ekonomické specializace v rámci stejného regionu;
- cestní síť;
- explicitní rozdělení regionální populace;
- startovní souřadnici a její skutečné lokální vlastnosti.

## Co v0.3 ještě není

Mapa je stále abstraktní mřížka, ne finální herní mapa. Řeka je jednoduchý procedurální pás a cesty jsou vedené přímou trasou. Historie stále ještě plně nepřepisuje současný stav světa.

Další hlavní krok bude **Population & Economy v0.4**: domácnosti, pracovní místa, poptávka po profesích, produkce a spotřeba navázaná na konkrétní sídla a jejich lokální zdroje.
