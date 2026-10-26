# World Generator – generování světa před prvním tahem

Stav: schválený směr po revizi prototypů přirozeného vstupu.

## Zásadní změna směru

Pružnost hry nemá stát na tom, že engine dovede odpovědět na libovolnou otázku o dosud prázdném světě.

Nejdřív musí vzniknout **svět, který skutečně existuje**, a teprve nad ním vzniká scéna, kontextové možnosti a případný volný text hráče.

Základní pravidlo:

> **Hráč nikdy nevstoupí do nevygenerované scény.**

## Úrovně detailu

Svět se negeneruje všude stejně podrobně.

### LOD 1 – celý svět / velké regiony

Při založení hry vzniká:
- základní geografie;
- klima;
- hlavní zdroje;
- velké regiony;
- hlavní politické celky;
- hrubá populace;
- významné obchodní a dopravní trasy.

### LOD 2 – region

Vzniká:
- síť sídel;
- lesy, řeky, cesty a další významné lokality;
- regionální ekonomika;
- významné frakce;
- populační struktura;
- významné podniky a zdroje.

### LOD 3 – sídlo

Vzniká:
- budovy a podniky;
- domácnosti a rodiny;
- velká část obyvatel alespoň v lehkém persistentním profilu;
- lokální správa, cechy a instituce;
- pracovní místa a vlastnictví;
- vazby na okolní ekonomiku.

### LOD 4 – konkrétní budova nebo lokace

Před vstupem hráče musí být známé:
- vchody a výstupy;
- místnosti nebo lokální podprostory;
- významné vybavení;
- aktuálně přítomné osoby nebo populační skupiny;
- služby a funkce lokace.

### LOD 5 – aktuální scéna

Před zobrazením scény engine konkretizuje:
- to, co lze právě vnímat;
- důležité předměty;
- přítomné postavy;
- dostupné směry;
- relevantní affordances;
- kontextové akce.

## Postavy

Ne všechny NPC musí mít stejnou hloubku.

### Významné NPC

Kompletní persistentní profil už při worldgenu:
- rodina;
- profese;
- majetek;
- vztahy;
- frakce;
- cíle;
- základní charakter;
- historie.

### Běžná NPC

Persistentní lehký profil:
- stabilní ID;
- jméno;
- věk;
- domácnost;
- profese;
- bydliště;
- zaměstnavatel;
- základní majetkový a sociální stav.

Hlubší osobnost, portrét, osobní cíle a dlouhodobá paměť se mohou doplnit až při relevanci, ale nesmějí měnit již vytvořená fakta.

### Statistická populace

Používá se pouze tam, kde není důvod vytvářet jednotlivce. Při přechodu do vyššího detailu se z populačních statistik vytvoří konkrétní persistentní lidé konzistentní s již vygenerovanou strukturou.

## Historie

Aktuální svět nemá vzniknout pouhým náhodným rozdáním hodnot.

Worldgen pipeline:

```text
WorldConfig + Seed
        ↓
geografie
        ↓
regiony a sídla
        ↓
populace a rodiny
        ↓
profese, podniky, vlastnictví
        ↓
frakce a instituce
        ↓
vztahy
        ↓
historická simulace
        ↓
současný stav světa
        ↓
hráčský původ a znalosti
        ↓
startovní oblast
        ↓
lokální scéna
        ↓
kontextové možnosti
```

Historické události musí postupně měnit stav světa, nikoli být pouze textovou kronikou.

## Kontextové akce

UI má primárně nabízet akce odvozené ze scény.

Příklad lesa:

```text
scene:
- cesta
- stopy v blátě
- padlý kmen
- potok v doslechu

actions:
- prohlédnout stopy
- vydat se po cestě
- jít k potoku
- prohlédnout padlý kmen
```

Volný text zůstává zachovaný jako doplňková kreativní možnost, ale základní hratelnost nesmí být závislá na tom, že AI vymyslí chybějící svět.

## Determinismus

Worldgen používá seed. Stejná verze generátoru + stejné parametry + stejný seed musí vytvořit stejný výchozí svět.

To umožňuje:
- reprodukovatelné testy;
- ladění;
- sdílení světů;
- kontrolu regresí.

## Validace

Po worldgenu musí před spuštěním hry proběhnout automatická validace:
- všechny reference směřují na existující entity;
- člen rodiny skutečně existuje;
- vlastník podniku existuje;
- zaměstnanec existuje;
- frakční členství odkazuje na existující frakci;
- domácnosti a bydliště existují;
- historie neodkazuje na neexistující entity;
- počáteční scéna je odvoditelná ze skutečně existující oblasti.

Nevalidní svět nesmí být spuštěn.

## Prototyp

První implementace je v:

`prototype/worldgen`

World Generator v0.1 ověřil datovou pipeline, konzistenci a vznik startovní scény. v0.2 začíná proceduralizovat samotný region: seed ovlivňuje klima, reliéf, úrodnost, lesnatost, vodu, zdroje, populaci, sídla a z nich odvozenou základní ekonomiku.

### Vztah WorldConfig, seedu a pravidel

Základní rovnice worldgenu:

```text
WORLD =
WorldConfig
+ Seed
+ generační pravidla
+ historická simulace
```

**WorldConfig** vymezuje prostor možných světů. Určuje například éru, technologický strop, fantasy pravidla, klimatický bias, politickou stabilitu a ekonomickou komplexitu.

**Seed** neurčuje pravidla reality. Vybírá konkrétní variantu uvnitř těchto pravidel.

**Generační pravidla** převádějí fyzické podmínky na důsledky:

```text
klima + reliéf
→ úrodnost + voda + lesnatost
→ zdroje
→ nosná kapacita
→ populace
→ sídla
→ ekonomické potřeby
→ profese a podniky
```

Tím se chceme vyhnout nezávislému náhodnému generování prvků, které by spolu nedávaly smysl.


## Prostorová vrstva regionu

Od v0.3 má region skutečnou prostorovou reprezentaci.

Každá buňka regionu má vlastní fyzické vlastnosti:

```text
MapCell
- x / y
- elevation
- moisture
- temperature
- fertility
- forest_cover
- has_river
- terrain
- local_resources
```

Sídla se umísťují do konkrétních buněk na základě vhodnosti prostředí. Ekonomické specializace a podniky se odvozují z lokálně dosažitelných zdrojů, ne z globálního seznamu zdrojů regionu.

To znamená, že dva body stejného regionu mohou mít zcela odlišnou ekonomiku.

### Prostorová kauzalita

```text
konkrétní buňka mapy
→ voda / půda / les / ruda / reliéf
→ vhodnost pro osídlení
→ vznik sídla
→ lokální dostupné zdroje
→ specializace sídla
→ podniky a pracovní místa
```

### Cesty

Sídla jsou propojena explicitními Road entitami. Cesta obsahuje skutečnou trasu přes souřadnice mapy.

První verze používá jednoduchou trasu mezi sídly. Budoucí pathfinding má zohlednit:
- sklon terénu;
- řeky a brody;
- mokřady;
- lesy;
- průsmyky;
- existující cesty.

### Populační účetnictví

Regionální populace se musí přesně rozdělit:

```text
total_population =
settled_population
+ rural_households
+ small_hamlets
+ isolated_population
```

Tím nevzniká nevysvětlená populace mimo sídla.

### Stabilní RNG streamy

Každý subsystém používá vlastní random stream odvozený ze stejného root seedu.

Změna parametru politiky proto nesmí spotřebou náhodných čísel změnit geografii. Tento princip je důležitý pro reprodukovatelnost, testování i budoucí migrace saveů.


## Populace, domácnosti a ekonomika

Od v0.4 je populace hlavních sídel reprezentována persistentními domácnostmi a lehkými osobami.

Zásadní pravidlo:

> **Detailní Character není obyvatel vytvořený navíc. Je to hlubší reprezentace člověka, který už ve světě existuje.**

### Domácnosti

```text
Settlement
→ Household[]
→ PersonStub[]
```

Domácnost je základní sociálně-ekonomická jednotka. Uchovává počet členů, sociální vrstvu, hlavní zdroj obživy a majetkovou úroveň.

### Populační konzistence

Pro každé hlavní sídlo musí platit:

```text
settlement.population
=
sum(household.member_count)
=
count(persistent_people)
```

Statistická populace mimo hlavní sídla zůstává zatím agregovaná v regionálním populačním účetnictví.

### Potřeby a výroba

Každé sídlo má agregovanou poptávku po základních kategoriích a produkci odvozenou od lokálních zdrojů.

```text
population
→ demand

local resources
→ possible production
→ labor demand
→ businesses
→ production capacity
```

Ekonomická bilance je:

```text
local_balance(resource)
=
local_production(resource)
-
local_demand(resource)
```

Kladný zůstatek je potenciální export. Záporný zůstatek je potenciální importní potřeba.

### Pracovní síla

Poptávka po profesích se odvozuje od velikosti populace, lokálních zdrojů a typu sídla.

Podniky následně vznikají jako organizační struktura, která tuto práci a produkci zajišťuje.

Tím se worldgen posouvá od:

```text
random business
```

k:

```text
lidé
→ potřeby
→ práce
→ podniky
→ výroba
```

### Hranice v0.4

v0.4 ještě nesimuluje zásoby, ceny, mzdy, peněžní toky ani skutečné přepravy. Tyto vrstvy mají navázat až na ověřenou populační a produkční strukturu.


## Hierarchie prostoru a lokální scéna

Regionální buňka není vždy konečná herní scéna.

Zásadní pravidlo:

> **Kontextové akce se generují pouze z aktuálně vnímatelné lokální scény, ne ze všech entit celé regionální buňky.**

Hierarchie prostoru:

```text
WORLD
→ REGION
→ REGION CELL
→ SETTLEMENT
→ DISTRICT
→ STREET / SQUARE / BUILDING
→ ROOM / LOCAL SCENE
→ VISIBLE ENTITIES
→ CONTEXT ACTIONS
```

### Jednoduchá krajina

Lesní, polní nebo horská regionální buňka může být dostatečně jednoduchá, aby přímo poskytla lokální scénu.

Příklad:

```text
region cell
- les
- cesta
- padlý kmen
- potok

→ přímo lokální scéna
```

### Sídlo jako vstup do další úrovně detailu

Pokud regionální buňka obsahuje město nebo vesnici, hráč nevstupuje do abstraktního seznamu všech obyvatel a podniků.

Městská buňka odkazuje na vlastní prostorovou strukturu:

```text
Settlement
├─ gates / entry points
├─ districts
├─ streets
├─ squares
├─ buildings
└─ local scenes
```

Směr příchodu určí konkrétní vstupní bod. Příchod po východní cestě může například vytvořit aktuální scénu „Východní brána“, nikoli „celé město“.

### Strukturální existence vs. detail

Sídlo musí předem existovat strukturálně. Worldgen může znát například:

- počet a hranice čtvrtí;
- uliční síť;
- náměstí;
- brány;
- budovy;
- podniky;
- domácnosti;
- základní funkci budov a jejich vlastnictví.

Detail interiéru konkrétní budovy nebo místnosti se může doplnit před vstupem hráče, ale pouze v rámci již existující struktury.

Lazy concretization tedy smí doplňovat detail, nikoli dodatečně vytvářet existenci významné budovy, podniku nebo instituce jen proto, že se na ni hráč zeptal.

### Viditelnost lidí a podniků

To, že ve městě existuje 1 000 obyvatel a desítky podniků, neznamená, že jsou všichni součástí aktuální scény.

Engine pracuje s prostorovou a časovou distribucí:

```text
population of settlement
→ current schedules / aggregate presence
→ population in district
→ people in local scene
→ visible / interactable people
```

Běžná populace může být mimo hráčovu relevanci agregovaná. Při vstupu do konkrétní scény engine vybere skutečné persistentní osoby konzistentní s jejich domácností, profesí, časem a místem.

### Znalostní filtr

UI nesmí automaticky nabídnout všechny existující podniky nebo osoby ve městě.

Cílové akce typu „Jít ke kovárně“ se nabídnou pouze tehdy, pokud je místo hráči známé, právě viditelné nebo jej lze rozumně odvodit z aktuálního kontextu.

Tím se zachovává oddělení:

```text
objective world truth
≠
player knowledge
≠
current perception
```


## Ekonomická konzistence

Od v0.4.1 se produkce nesmí generovat nezávisle na ekonomických entitách.

Správná kauzalita je:

```text
populace
→ pracovní síla
→ pracovní struktura
→ výrobní jednotky
→ jejich kapacita a výstupy
→ produkce sídla
→ spotřeba
→ přebytek / deficit
```

Výrobní jednotka může být rodinná, podniková, velkoprodukční nebo institucionální.

To zabraňuje dvěma chybám:

1. vysoký počet pracovníků už automaticky nevytváří desítky identických podniků;
2. zničení nebo změna výrobní jednotky se musí promítnout do skutečné ekonomické bilance.

Trade & Market má být postaven až nad tímto konzistentním základem.


## Kapacita fyzických zdrojů

Od v0.4.2 nejsou přírodní zdroje pouze binární vlastnost lokace.

Sídlo získává od svého skutečného okolí `ResourceCapacity`, která určuje fyzický strop primární produkce.

```text
map cells
→ usable local resource
→ area / quality
→ max workers
→ max output
→ production units
```

Tím se obrací předchozí logika „více obyvatel → více producentů“.

Správně je:

```text
prostředí určí kapacitu
↓
kapacita omezuje práci a produkci
↓
produkce omezuje dlouhodobě udržitelnou populaci nebo vytváří potřebu obchodu
```

Tento základ je nutný před Trade & Market, protože deficit musí mít fyzickou příčinu, nikoli vznikat pouze náhodným koeficientem.


## Hospodářské zázemí sídel

Od v0.4.3 se hlavní sídlo neposuzuje jako izolovaná ekonomická tečka.

Každá venkovská buňka regionu je deterministicky přiřazena nejbližšímu sídlu a vytváří jeho `Hinterland`.

```text
settlement
↕
assigned rural cells
↕
rural households / hamlets / isolated population
↕
farmland / forest / deposits / rivers
```

Hinterland je ekonomické zázemí, nikoli nutně politická hranice.

### Populační invariant

Pro region musí platit:

```text
Σ hinterland.rural_population = region.rural_households_population
Σ hinterland.hamlet_population = region.small_hamlets_population
Σ hinterland.isolated_population = region.isolated_population
```

### Kalibrace produkce a spotřeby

Produkce a spotřeba musí používat společnou abstraktní roční škálu.

Pro potraviny je základní interpretace:

> **1 food unit = základní roční potravinová potřeba jednoho člověka.**

Ostatní základní zdroje používají analogické normalizované jednotky.

To umožňuje srovnávat fyzickou produkční kapacitu s populací bez falešné přesnosti kilogramů nebo historických měr, které zatím nejsou součástí simulace.

### Důsledek

Město může být:

- zásobované vlastním hinterlandem;
- částečně závislé na importu;
- exportním centrem;
- zranitelné ztrátou venkovského zázemí.

Tím může budoucí válka, katastrofa nebo politická změna ovlivnit město i bez přímého zásahu do městské buňky.
