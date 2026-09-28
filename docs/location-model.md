# Model lokací

## Základní princip

Lokace se mají skládat hierarchicky od nejvyšší úrovně světa až po nejnižší herně relevantní prostor.

Doporučená hierarchie:

```text
World
└── CelestialBody
    └── Continent
        └── Region
            └── Territory / State
                └── Settlement
                    └── District
                        └── Building
                            └── Room / LocalArea
```

Ne všechny vrstvy musí být použity v každém světě. Ve středověkém světě může být CelestialBody pouze technická úroveň, zatímco ve sci-fi může být zásadní.

## Room / LocalArea

Nejnižší úroveň lokace nemusí být vždy klasická místnost.

Může jít například o:

- pokoj,
- sklep,
- dílnu,
- kuchyň,
- výčep,
- nádvoří,
- část tržiště,
- most,
- lesní mýtinu,
- nástupiště,
- kajutu lodi,
- jiný lokálně významný prostor.

Proto je vhodnější pracovat s obecnějším konceptem `Room / LocalArea`.

## Postupné generování detailu

Detailní lokace se negenerují všechny předem.

Například vzdálený hostinec může v systému existovat pouze jako budova. Teprve když do něj hráč vstoupí nebo se stane herně relevantním, mohou být vygenerovány konkrétní prostory:

- výčep,
- kuchyň,
- pokoje,
- sklep,
- sklad.

Jakmile takový prostor jednou vznikne, stává se kanonickou součástí světa a jeho vlastnosti se dále ukládají a vyvíjejí.

## Herní význam místností a lokálních oblastí

Místnosti nejsou pouze vizuální detail.

Ovlivňují například:

- kdo se nachází ve stejné scéně,
- kdo může slyšet rozhovor,
- soukromí,
- přístupnost,
- zamčené nebo zakázané prostory,
- krádeže,
- stealth,
- tajná setkání,
- ukládání předmětů,
- vlastnictví prostoru,
- lokální události.

Příklad:

> Elise je v kuchyni hostince.

NPC ve výčepu nemusí slyšet její rozhovor s hráčem.

## Možné vlastnosti lokace

Každý lokální prostor může mít například:

- id,
- název,
- typ,
- parent_location_id,
- vlastníka,
- kapacitu,
- vstupy a výstupy,
- propojené lokace,
- přístupová pravidla,
- soukromí,
- hluk,
- osvětlení,
- bezpečnost,
- aktuální stav,
- přítomné postavy,
- předměty a vybavení,
- vizuální asset,
- stav znalosti hráče o lokaci.

## Známé a neznámé lokace

Existence lokace v databázi neznamená, že o ní hráč ví.

To navazuje na znalostní model:

- unknown,
- rumor,
- approximate,
- known,
- visited.

Hráč může například vědět, že hostinec má sklep, ale neznat jeho přesné uspořádání.

## Kanonizace

Platí stejné pravidlo jako pro ostatní části světa:

> Jakmile je konkrétní lokace nebo její vlastnost jednou vygenerována a potvrzena simulací, stává se součástí kanonického stavu světa.

AI ji následně nesmí svévolně měnit bez herní příčiny.
