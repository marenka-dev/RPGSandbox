# Znalostní model světa

## Základní myšlenka

Hráč ani NPC nemají automaticky přístup k objektivní pravdě světa.

Svět může obsahovat technologie, místa, civilizace, jevy nebo historické události, o kterých konkrétní postava vůbec neví. To, co postava zná, se odvíjí od jejího původu, vzdělání, zkušeností, rozhovorů, knih, cestování a dalších zdrojů.

Hráč tedy svět skutečně **objevuje**, místo aby od začátku četl úplnou encyklopedii.

## Čtyři vrstvy informace

### Reality

Co ve světě objektivně existuje nebo co se skutečně stalo.

Příklad:
- vesmírné lety existují,
- elektřina existuje,
- vzdálený kontinent existuje,
- draci skutečně existují.

### Knowledge

Co konkrétní postava skutečně zná nebo chápe.

Příklad:
- vesničan zná základní metalurgii,
- učenec zná astronomii,
- hráč ví o existenci sousedního království.

### Belief

Čemu postava věří, i když to nemusí být pravda.

Příklad:
- místní věří, že zatmění je božské znamení,
- učenec používá zastaralou teorii,
- hráč věří informaci ze staré knihy.

### Rumor

Neověřená informace, která se šíří světem.

Příklad:
- „Na západě existují vozy, které jedou bez koní.“
- „Za horami žijí obři.“
- „Král je údajně mrtvý.“

Rumor může být pravdivý, nepravdivý nebo částečně pravdivý.

## Znalost není binární

Informace nemusí být pouze známá nebo neznámá.

Možné stavy:

- unknown,
- rumor,
- partial,
- known,
- understood,
- mastered.

Příklad technologické znalosti:

1. hráč o parním stroji nikdy neslyšel,
2. zaslechne pověst o vozech bez koní,
3. uvidí parní stroj,
4. pochopí jeho základní princip,
5. naučí se s ním pracovat,
6. získá dostatečné znalosti pro výrobu nebo vývoj.

## Individuální znalost postavy

Každá významná postava může mít vlastní soubor znalostí.

Příklad:

```text
KnowledgeEntry

subject:
  type: technology
  id: steam_engine

state: rumor
confidence: 0.35
source: traveler
accuracy: unknown
learned_at: village_inn
```

Možné vlastnosti znalosti:

- subject,
- knowledge_state,
- confidence,
- source,
- accuracy,
- learned_at,
- learned_when,
- last_verified,
- emotional_significance.

## Zdroje znalostí

Postava může získat informace například:

- vlastní zkušeností,
- pozorováním,
- rozhovorem s NPC,
- knihou,
- školou,
- cestováním,
- mapou,
- kronikou,
- úředním oznámením,
- cechem,
- náboženskou institucí,
- špionáží,
- pověstí.

Zdroj ovlivňuje spolehlivost informace.

## Objektivní svět vs. známý svět

Svět může být technologicky velmi pokročilý, ale postava žijící v izolované vesnici o tom nemusí mít žádné tušení.

Například:

```text
Reality:
  spaceflight: true
  electricity: true
  printing_press: true

Local village knowledge:
  spaceflight: unknown
  electricity: unknown
  printing_press: rumor

Player knowledge:
  spaceflight: unknown
  electricity: rumor
  printing_press: known
```

To umožňuje, aby i v jednom světě existovaly společnosti s dramaticky rozdílnou úrovní znalostí.

## Znalost technologií

Technologický rozvoj by neměl být pouze vlastností celého světa.

Je vhodné oddělit:

- zda technologie objektivně existuje,
- která civilizace ji zná,
- která instituce ji ovládá,
- která konkrétní postava o ní ví,
- zda ji postava chápe,
- zda ji dokáže prakticky použít.

Technologie se tak může šířit:

- obchodem,
- migrací,
- válkou,
- špionáží,
- vzděláváním,
- knihami,
- cestovateli,
- dobytím území.

## Znalost geografie

Stejný princip platí pro mapu světa.

Hráč nemusí na začátku vidět celý kontinent.

Může znát pouze:

- svou vesnici,
- okolní les,
- cestu do nejbližšího města.

Vzdálenější oblasti mohou existovat v databázi, ale pro postavu jsou:

- neznámé,
- přibližně známé,
- zakreslené na staré mapě,
- popsané cestovatelem,
- osobně navštívené.

## Znalost historie a politiky

Postavy mohou znát různé verze stejné události.

Například:

Reality:
- král ustoupil kvůli nedostatku zásob.

Official history:
- král zachránil armádu před obklíčením.

Enemy narrative:
- král zbaběle utekl.

Local folklore:
- existuje posměšná píseň o útěku krále.

AI při dialogu nemá používat objektivní historii automaticky. Má odpovídat podle znalostí konkrétní postavy.

## Chybné a zastaralé znalosti

Znalost může být:

- pravdivá,
- nepravdivá,
- částečně pravdivá,
- zastaralá,
- neověřená.

Příklad:

Hráč získá 200 let starou knihu. Informace v ní byla tehdy správná, ale současný svět už vypadá jinak.

Hráč tedy může činit rozumné rozhodnutí na základě nesprávných informací.

## AI dialogy

Při rozhovoru se NPC by AI neměla dostat automaticky všechny informace světa jako znalosti dané postavy.

Kontext pro dialog má obsahovat:

- fakta o NPC,
- její osobní zkušenosti,
- relevantní KnowledgeEntries,
- její přesvědčení,
- relevantní pověsti,
- případné předsudky nebo propagandu.

Otázka:

> „Co víš o hvězdách?“

tak může vést k úplně jiné odpovědi od:

- vesničana,
- kněze,
- učence,
- astronoma,
- příslušníka technologicky vyspělé civilizace.

## Herní význam

Tento systém umožňuje:

- skutečné objevování světa,
- informační asymetrii,
- tajemství,
- propagandu,
- dezinformace,
- průzkum,
- vzdělávání postavy,
- technologický transfer,
- smysluplné cestování,
- rozdílné perspektivy NPC.

Znalostní model je proto základní součást simulace, nikoliv pouze doplněk dialogového systému.
