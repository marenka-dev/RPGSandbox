# Frakce, organizace a politika

## Základní princip

Konkrétní frakce nejsou napevno definované názvem ani obsahem.

Engine pracuje s obecnými typy frakcí a organizací, ze kterých při generování světa vznikají konkrétní instance odpovídající období, kultuře, technologické úrovni a historii světa.

Příklady typů:

- stát,
- městská samospráva,
- šlechtický rod,
- cech,
- obchodní společnost,
- korporace,
- církev,
- armáda,
- gang,
- politické hnutí,
- tajná organizace,
- vědecká společnost,
- separatistická skupina,
- komunitní nebo rodová skupina.

Konkrétní frakce mohou během hry:

- vznikat,
- zanikat,
- slučovat se,
- rozdělovat se,
- měnit vedení,
- měnit ideologii,
- získávat nebo ztrácet území,
- měnit vztahy k ostatním frakcím.

---

## FactionType vs. Faction

### FactionType

Obecná šablona nebo pravidlo.

Například:

- MerchantGuild,
- NobleHouse,
- Church,
- Corporation,
- PoliticalMovement.

### Faction

Konkrétní instance ve světě.

Například:

- Obchodní cech Rabenfeldu,
- Rod Varenů,
- Církev sedmi světel,
- Helix Mining Corporation.

---

## Formální, neformální a tajné skupiny

### Formální

- stát,
- městská rada,
- cech,
- firma,
- armáda,
- škola,
- církev.

### Neformální

- rodina,
- klan,
- lokální komunita,
- skupina přátel,
- zájmová klika.

### Tajné

- kult,
- špionážní síť,
- podzemní hnutí,
- pašerácká síť,
- tajná politická skupina.

---

## Oddělení lokace a organizace

Město nebo stát není totéž jako politická organizace, která ho spravuje.

Příklad:

```text
Settlement: Rabenfeld
GovernmentOrganization: Rabenfeld City Council
Faction: Merchant League
Faction: House Varen
Faction: Church of Light
```

Tyto entity mohou soupeřit o vliv nad stejným územím.

---

## Organizační role

Každá organizace může mít vlastní strukturu funkcí.

Role nemají být napevno svázané s konkrétním historickým titulem.

Obecné typy rolí mohou být například:

- výkonná,
- zákonodárná / radní,
- správní,
- soudní,
- vojenská,
- ekonomická,
- náboženská,
- reprezentativní,
- bezpečnostní,
- odborná.

Konkrétní název role vychází z kultury a období.

Například stejná výkonná role může být pojmenována:

- starosta,
- rychtář,
- guvernér,
- správce,
- lord,
- regionální ředitel,
- velitel stanice.

---

## GovernmentSystem

Politická struktura města, státu nebo jiné spravované entity má vlastní systém vlády.

Příklad:

```text
SettlementGovernment
government_type: merchant_republic

positions:
- Mayor
- Council Member
- Treasurer
- Captain of the Guard
- Magistrate
- Guild Representative
```

Jiný svět:

```text
government_type: feudal

positions:
- Lord
- Steward
- Bailiff
- Captain of the Guard
- Tax Collector
```

Cyberpunk:

```text
government_type: corporate

positions:
- Regional Director
- Operations Manager
- Security Chief
- Finance Director
```

---

## Office

Funkce existuje nezávisle na osobě, která ji právě zastává.

Příklad:

```text
Office
id: office_rabenfeld_mayor
organization: Rabenfeld City Council
role_type: executive
title: Mayor
term_length: 4 years
selection_method: election
current_holder: npc_142
```

Pokud držitel zemře, odstoupí nebo je odvolán, funkce dál existuje.

---

## OfficeHolder

Propojuje konkrétní postavu s konkrétní funkcí.

Může evidovat:

- datum nástupu,
- datum konce,
- způsob získání funkce,
- legitimitu,
- podporu,
- důvod odchodu,
- případné dočasné zastupování.

---

## Způsoby obsazování funkcí

Možné mechanismy:

- volby,
- dědictví,
- jmenování,
- hlasování rady,
- seniorita,
- koupě funkce,
- náboženská volba,
- vojenské převzetí,
- los,
- konkurz,
- firemní jmenování.

Výběr mechanismu závisí na dané organizaci a kultuře.

---

## Více rolí jedné postavy

Jedna postava může mít více funkcí současně.

Například Johann Keller může být:

- obchodník,
- člen obchodního cechu,
- městský radní,
- majitel skladu,
- hlava rodiny.

To vytváří přirozené střety zájmů.

---

## Vnitřní struktura organizací

Každá organizace může definovat:

- vedení,
- hierarchii,
- členství,
- pravomoci,
- pravidla vstupu,
- pravidla odchodu,
- majetek,
- sídlo,
- území,
- ekonomickou sílu,
- vojenskou sílu,
- politický vliv,
- veřejnou reputaci,
- cíle,
- ideologii,
- vnitřní konflikty.

---

## Politika jako důsledek systému

Politika nevzniká pouze přes hodnotu "political_power".

Vzniká hlavně z toho:

- kdo zastává jakou funkci,
- jaké má pravomoci,
- komu odpovídá,
- kdo ho podporuje,
- která frakce má vliv na jeho rozhodování,
- jaké má vztahy k ostatním držitelům funkcí,
- jakým způsobem může být nahrazen.

Základní pravidlo:

> **Každá organizace může mít vlastní strukturu funkcí a pravidla jejich obsazování. Politika vzniká z toho, kdo tyto funkce drží, jaké mají pravomoci a jaké frakce je podporují.**

---

## Budoucí rozšíření

Na tento model lze později navázat:

- volbami,
- hlasováním rad,
- zákony,
- daněmi,
- korupcí,
- převraty,
- občanskými nepokoji,
- politickými kampaněmi,
- nástupnictvím,
- diplomatickými vztahy.
