# World Generator v0.4 – Population & Economy

v0.4 navazuje na prostorový region z v0.3 a přidává první skutečnou populační a ekonomickou vrstvu.

## Hlavní změna

Populace hlavních sídel už není jen číslo ani několik reprezentativních NPC.

Každý obyvatel hlavního sídla nyní patří do persistentní domácnosti a zároveň existuje jako lehká persistentní osoba.

```text
region
↓
settlement
↓
households
↓
people
↓
working population
↓
job demand
↓
businesses
↓
production / consumption
↓
economic balance
```

## Domácnosti

Každé sídlo je rozděleno na domácnosti.

Domácnost obsahuje například:

- stabilní ID;
- sídlo;
- počet členů;
- sociální vrstvu;
- hlavní zdroj obživy;
- majetkovou úroveň;
- vazbu na své členy.

Součet členů domácností musí přesně odpovídat populaci sídla.

## Lehká populace

Pro každého obyvatele hlavního sídla vzniká lehká persistentní osoba:

- stabilní ID;
- jméno;
- věk;
- domácnost;
- sídlo.

To není kompletní Character profil. Hlubší osobnost, cíle, paměť a vztahy se mohou doplnit až tehdy, když se osoba stane důležitou.

Důležité je, že postava **už existuje před setkáním s hráčem**.

## Ekonomické potřeby sídla

Každé sídlo má agregované potřeby:

- food;
- fuel;
- clothing;
- tools.

Množství vychází z počtu obyvatel.

## Produkce

Produkce se odvozuje od lokálních zdrojů.

Například:

```text
obilí
→ produkce food

ryby
→ další food

dřevo
→ fuel + část tools

železná ruda
→ tools
```

v0.4 je stále zjednodušený model. Produkce zatím nepracuje s úplnými výrobními recepturami a zásobami.

## Bilance

Pro každou potřebu se počítá:

```text
balance = local production - local demand
```

Výsledek může být:

- kladný – přebytek;
- nulový – přibližná soběstačnost;
- záporný – deficit.

To je budoucí základ pro obchod mezi sídly.

## Pracovní trh

Z populace se odvodí přibližná pracovní síla a následně poptávka po profesích.

Příklady:

- zemědělec;
- dřevorubec;
- horník;
- rybář;
- řemeslník;
- služby;
- obchodník;
- správa.

Podniky vznikají podle pracovní poptávky a lokálních zdrojů, ne přímo podle jednoduchého `population / číslo`.

## Podniky

Podnik v0.4 obsahuje:

- typ;
- sídlo;
- vlastníka;
- několik reprezentativních pracovníků;
- výstupy;
- požadovaný počet pracovníků;
- produkční kapacitu.

Počet pracovníků je zatím agregovaný. Není nutné ukládat tisíce pracovních vazeb jednotlivě, dokud nejsou herně relevantní.

## Významné NPC

Z nejvýznamnějších nebo nejbohatších domácností vznikne menší sada detailnějších Character entit.

Character tedy není náhrada populace.

Je to detailnější vrstva nad již existujícím člověkem/domácností.

## Validace

Worldgen kontroluje mimo jiné:

- součet populace regionu;
- součet populace hlavních sídel;
- součet členů domácností v každém sídle;
- počet lehkých osob proti populaci sídla;
- existenci ekonomiky pro každé sídlo;
- reference domácností;
- reference podniků a vlastníků;
- matematickou správnost ekonomické bilance.

## Spuštění

```powershell
cd prototype\worldgen

dotnet run -- --seed 111
dotnet run -- --seed 222
dotnet run -- --seed 333
```

## Co sledovat

Ve výstupu se nově objeví například:

```text
SÍDLA A EKONOMIKA:
- ...
    domácnosti: ...
    obyvatel v domácnostech: ...
    potřeby: food:..., fuel:...
    produkce: food:..., fuel:...
    bilance: food:+..., tools:-...
    pracovní poptávka: zemědělec:..., řemeslník:...
```

Porovnej hlavně, zda:

1. domácnosti přesně pokrývají populaci sídla;
2. sídla s odlišnými zdroji mají odlišnou pracovní strukturu;
3. deficit a přebytek dávají intuitivní smysl;
4. počet podniků odpovídá pracovní poptávce;
5. různé seedy vytvářejí odlišnou ekonomickou strukturu.

## Co v0.4 ještě není

- žádné skutečné zásoby;
- žádné ceny;
- žádné peněžní toky;
- žádný obchod mezi sídly;
- žádné mzdy;
- žádná detailní zaměstnání všech jednotlivců;
- žádná historická změna ekonomiky.

Před Trade & Market vložíme ještě mezikrok **v0.4.1 – Economic Consistency**: práce → výrobní jednotky → kapacita → skutečná produkce musí být jedna kauzální větev, ne paralelní výpočty.

Současně je schválen další prostorový princip pro pozdější Settlement Spatial Model: regionální buňka s městem není jedna obří scéna. Vstup do sídla vede přes konkrétní entry point do hierarchie čtvrtí, ulic, budov a lokálních scén. Kontextové akce se vždy odvozují jen z aktuální scény, nikoli ze všech obyvatel a podniků sídla.


## v0.4.1 – Economic Consistency

v0.4.1 sjednocuje pracovní sílu, výrobní jednotky a produkci do jedné kauzální větve.

```text
population
→ households
→ labor structure
→ production units
→ output
→ settlement demand
→ balance
```

Produkce už nevzniká samostatným výpočtem podle toho, že sídlo „má obilí“. Každá jednotka produkce musí pocházet z konkrétní výrobní jednotky.

### ProductionUnit

Výrobní jednotka má:

- sídlo;
- typ;
- měřítko;
- počet pracovníků;
- volitelnou vazbu na domácnost;
- konkrétní výstupy.

Měřítka:

- `household` – rodinné hospodářství, domácí řemeslo, drobná služba;
- `business` – hostinec, dílna, obchod, dřevorubecký provoz;
- `estate` – větší statek, důl nebo jiný větší producent;
- `institution` – správní kapacita.

### Důležitý rozdíl

271 zemědělců už neznamená automaticky 34 identických firem.

Část práce se rozdělí mezi rodinná hospodářství a menší počet větších statků.

Podobně služby a obchod mají omezený počet viditelných podniků; zbytek může zůstat jako agregovaná domácí/lokální ekonomická aktivita.

### Produkce je odvozena z jednotek

Pro sídlo platí:

```text
settlement production
=
sum(outputs of all production units)
```

Validátor kontroluje, že čísla v ekonomice přesně odpovídají výrobním jednotkám.

Pokud se později výrobní jednotka zničí, zavře nebo přijde o pracovníky, musí se změnit i produkce sídla.

### Oblečení

v0.4.1 přidává první jednoduchou textilní výrobu. Sídla s pastvinami mají lepší produktivitu, ostatní mohou mít pouze omezenou domácí textilní výrobu. Deficit oblečení tak už není nutně absolutní, ale může přirozeně zůstat importní potřebou.

### Další krok

Až tento model projde testem, dává smysl přejít na **Trade & Market**: přebytky a deficity mezi sídly, přeprava přes existující cesty a následně ceny.


## v0.4.2 – Resource Capacity

v0.4.2 přidává fyzickou kapacitu přírodních zdrojů. Zdroj už není pouze informace typu „v okolí je obilí / les / ruda“.

Každé sídlo může mít explicitní `ResourceCapacity`:

- typ zdroje;
- dostupnou plochu;
- kvalitu;
- maximální počet pracovníků;
- maximální roční výstup.

Aktuálně se modelují zejména:

- `farmland`;
- `forest`;
- `iron_deposit`;
- `fishery`.

### Nová kauzalita

```text
geografie
→ lokální fyzický zdroj
→ jeho kapacita
→ maximum pracovníků
→ výrobní jednotky
→ maximální výstup
→ skutečná produkce
```

Počet zemědělců, dřevorubců, horníků a rybářů je nyní omezen kapacitou lokálního prostředí.

To znamená, že růst populace už automaticky nevytvoří proporcionálně více pracovníků v primární produkci.

### Zemědělství

Zemědělská kapacita vychází z množství vhodných okolních buněk a jejich úrodnosti.

```text
usable farmland area
+ fertility
→ max workers
→ max annual food output
```

Produkce rodinných hospodářství a velkých statků se dělí z jednoho společného maxima.

### Les, ruda a rybolov

Stejný princip se používá pro:

```text
forest
→ sustainable extraction capacity

iron deposit
→ mining capacity

river/fish resource
→ fishing capacity
```

v0.4.2 ještě neřeší dlouhodobé vyčerpávání zásob nebo regeneraci lesa v čase. Připravuje ale datový základ, aby tyto změny bylo možné později simulovat.

### Co sledovat při testu

Ve výstupu se nově objeví:

```text
kapacity zdrojů:
farmland area:... quality:... workers≤... output≤...
forest area:... quality:... workers≤... output≤...
...
```

Sleduj hlavně:

1. zda se počet pracovníků v primárních profesích vejde do limitu zdroje;
2. zda sídla s menší zemědělskou kapacitou mohou mít významný deficit jídla;
3. zda větší populace automaticky nevede k téměř přesné potravinové soběstačnosti;
4. zda rozdílná geografie vytváří rozdílné fyzické limity.

Po ověření této vrstvy může **v0.5 Trade & Market** pracovat s přebytky a deficity, které už mají skutečnou fyzickou příčinu.


## v0.4.3 – Hinterland & Resource Calibration

v0.4.3 propojuje hlavní sídla s venkovským hospodářským zázemím a sjednocuje měřítko produkce a spotřeby.

### Hinterland

Každá nesídelní buňka regionu je přiřazena nejbližšímu hlavnímu sídlu.

```text
region cells
→ nearest settlement
→ settlement hinterland
```

Tím vzniká ne překrývající se hospodářské zázemí.

Každé zázemí dostává podíl:

- rozptýlené venkovské populace;
- populace malých osad;
- obyvatel samot;
- konkrétních mapových buněk.

Součty všech hinterlandů musí přesně odpovídat regionálnímu populačnímu účetnictví.

### Ekonomická populace

Ekonomika centra nyní zahrnuje:

```text
settlement population
+
assigned hinterland population
=
economic population
```

To znamená, že venkovská populace už není pouze statistika bez ekonomické role.

### Kapacita z celého zázemí

Farmland, forest, iron deposit a fishery se již nepočítají jen z malého radiusu kolem města.

Kapacita vzniká z mapových buněk skutečně přiřazených danému sídlu.

### Kalibrované jednotky

Základní spotřeba používá roční abstraktní jednotky společné s výrobou.

Prototyp používá:

```text
food = 1 jednotka / osoba / rok
fuel = 0.65 jednotky / osoba / rok
clothing = 0.18 jednotky / osoba / rok
tools = 0.08 jednotky / osoba / rok
```

Nejde o kilogramy. Jde o normalizované roční potřeby, aby produkce a spotřeba používaly stejnou škálu.

### Co ověřit

Při testu sleduj hlavně:

1. kolik venkovské populace připadlo jednotlivým sídlům;
2. zda součet zázemí odpovídá regionální populaci mimo hlavní sídla;
3. zda farmland a další kapacity výrazněji odpovídají skutečnému okolí;
4. zda některá sídla mají přebytek a jiná deficit bez téměř automatické soběstačnosti;
5. zda velké město může být závislé na svém venkovském zázemí, ale není bez obchodu automaticky odsouzeno k hladomoru.

Po ověření této vrstvy je dalším krokem **v0.5 Trade & Market**.
