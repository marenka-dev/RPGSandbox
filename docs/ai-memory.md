# AI a dlouhodobá paměť

## Základní princip

Dlouhodobý kontext nesmí být řešen tím, že se do každého promptu vloží celá historie hry.

To by vedlo k:

- obrovskému kontextu,
- vyšším nákladům,
- pomalejším odpovědím,
- ztrátě konzistence,
- postupnému zapomínání důležitých faktů.

Proto musí být paměť hry strukturovaná.

## 1. Kanonická data světa

To jsou závazná fakta uložená v databázi.

Například:

- identita postavy,
- věk,
- povolání,
- lokace,
- inventář,
- peníze,
- rodina,
- vztahy,
- frakce,
- cíle,
- aktuální stav města,
- ceny,
- politická situace.

AI nesmí tato fakta svévolně přepisovat.

## 2. Event log

Každá významná událost se ukládá jako strukturovaný záznam.

Příklad:

- hráč se setkal s Elise,
- rozhovor proběhl na mostě,
- hráč jí řekl o plánu vydat se na sever,
- vztah se zlepšil,
- Elise nově zná hráčův plán.

Výhoda event logu:

- historie je dohledatelná,
- lze generovat kroniku,
- lze hledat příčiny současného stavu,
- lze dělat replay nebo diagnostiku,
- případně rollback.

## 3. Krátkodobá paměť

Obsahuje posledních několik tahů nebo poslední scénu.

Používá se pro:

- přirozenou návaznost dialogu,
- bezprostřední reakce,
- aktuální atmosféru,
- krátkodobé záměry.

## 4. Dlouhodobá osobní paměť

Každé významné NPC může mít vlastní vzpomínky.

NPC nemusí vědět všechno, co ví hra.

Může si pamatovat například:

- hráč mu pomohl,
- hráč ho urazil,
- hráč mu dluží peníze,
- hráč slíbil návrat,
- hráč mu prozradil citlivou informaci.

Vzpomínky mohou mít:

- význam,
- emocionální náboj,
- stáří,
- jistotu,
- zdroj informace.

## 5. Světová paměť

Shrnutí dlouhodobých událostí:

- historie regionu,
- války,
- politické změny,
- hospodářské krize,
- migrace,
- vztahy frakcí,
- významné katastrofy.

## 6. Aktivní kontext

Do konkrétního promptu pro AI se posílá jen výběr relevantních dat.

Například:

- aktuální lokace a čas,
- přítomná NPC,
- poslední tahy,
- vztah hráče k přítomným NPC,
- relevantní dlouhodobé vzpomínky,
- důležité lokální konflikty,
- případné cíle a aktivní události.

## Typický průběh jednoho tahu

1. Hráč zadá přirozeným jazykem akci.
2. Engine načte relevantní stav světa.
3. Memory systém vybere relevantní události a vzpomínky.
4. Sestaví se pracovní kontext.
5. AI vytvoří narativní reakci.
6. AI nebo pomocná logika navrhne strukturované změny.
7. Engine ověří, zda jsou změny povolené.
8. Změny se zapíší do kanonického stavu.
9. Vznikne event log.
10. Podle potřeby se aktualizují dlouhodobá shrnutí.

## Komprese historie

Po stovkách hodin hry vzniknou tisíce událostí.

Starší detaily se proto mohou:

- archivovat,
- seskupovat,
- sumarizovat do vyšší úrovně.

Příklad:

Místo desítek běžných návštěv hostince:

> Během prvních týdnů v Rabenfeldu hráč pravidelně navštěvoval hostinec a vybudoval si bližší vztah s Elise.

Původní události mohou zůstat uloženy, ale běžně se neposílají AI.

## Fakta vs. interpretace

Důležité rozdělení:

### Fakta

- Elise je servírka.
- Žije v Rabenfeldu.
- Chce mít vlastní podnik.
- Zná hráče.

### Interpretace

- působí nervózně,
- hlas má unavený,
- při zmínce o cestě na sever se zasní.

AI může kreativně pracovat hlavně s interpretací, nikoliv libovolně měnit fakta.

## Databáze

Pro MVP může stačit SQLite.

Později lze přejít například na PostgreSQL.

Možné entity:

- worlds,
- regions,
- settlements,
- factions,
- characters,
- relationships,
- inventories,
- events,
- memories,
- quests,
- assets,
- game_sessions,
- player_characters.

## Snapshoty

Kromě event logu je vhodné pravidelně ukládat snapshoty současného stavu.

To zrychlí načítání hry a zabrání nutnosti přepočítávat celý svět od jeho vzniku.
