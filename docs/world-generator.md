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
