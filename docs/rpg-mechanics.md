# RPG mechaniky a ověřování akcí

Stav: schválený princip mechanického vyhodnocování enginem; konkrétní atributy, číselné rozsahy a kostkový vzorec jsou návrhy k prototypování.

## Zásadní pravidlo

Úspěch a neúspěch akcí hráče i NPC vyhodnocuje **simulační engine**, nikoli AI. Engine zohledňuje vlastnosti postavy, naučené dovednosti, její skutečné znalosti, vybavení, zdravotní stav, okolnosti, obtížnost a v relevantních případech náhodný hod kostkou. AI interpretuje volný hráčský vstup do strukturovaného záměru a odvypráví výhradně ověřený výsledek.

## Základní atributy a dovednosti

Pracovní kandidáti atributů: síla, obratnost, odolnost, inteligence, vnímání a charisma. Jde o návrh, nikoli finální seznam. Diplomacie, přesvědčování, řemeslo, plížení, obchodní vyjednávání či výzkum jsou příklady **naučených dovedností**. Znalosti, osobnost, motivace, vztahy a frakční postoje jsou samostatné veličiny, nikoli automatické deriváty atributů.

Atributy, schopnosti a počáteční znalosti se odvozují mimo jiné od PlayerOrigin/lifepath nebo historie a životních zkušeností NPC. Postavy se mohou v čase učit a zlepšovat.

## Vyhodnocení akce

1. AI nebo kontextové UI předá strukturovaný záměr (kdo, co, vůči komu/čemu, kde).
2. Engine ověří, zda je akce fyzicky a kontextově možná a zda postava má prostředky a relevantní informace.
3. Rutinní činnosti bez rizika či nejistoty mohou proběhnout bez hodu. Jen relevantně nejisté akce spouštějí mechanický test.
4. Engine stanoví relevantní atributy/dovednosti, obtížnost, modifikátory, případně protihod jiné postavy a vygeneruje náhodný výsledek z reprodukovatelného herního zdroje.
5. Engine aplikuje skutečné účinky, čas, zdroje, dostupné nové znalosti a následné události do kanonického světa.
6. AI dostane závazný výsledek a podle něj vytvoří popis scény a reakce NPC.

Pro prototyp otestovat jednoduše čitelný hod **d20 + příslušný bonus proti obtížnosti**, včetně případných protihodů. Konečné stupnice, přirozené hody, kritické úspěchy/neúspěchy a přesné modifikátory zatím nejsou schválené; jejich funkčnost se musí ověřit na příkladech.

## Sociální interakce

Úspěšný test diplomacie nebo přesvědčování není kontrola mysli: cíl jedná v mezích svého charakteru, zájmů, znalostí, pravomocí a situace. Reakce mohou podléhat protihodu i dalším pravidlům. Změny vztahů provádí engine podle [Relationship Modelu](relationship-model.md) a mohou být žádné nebo odložené.

## Pravidla pro hráče a NPC

Hráč a NPC používají stejný mechanismus. Významné činy NPC mohou spustit stejné testy jako činy hráče. Méně významné dění ve vzdálených oblastech lze agregovat, ale bez rozporů s pravděpodobnostmi a již uloženými fakty.

Výsledek testu a použité modifikátory by měly být dohledatelné pro ladění a konzistenci; herní UI je může zobrazovat volitelně podle zvoleného stylu hraní.

## Co dále rozhodnout

- definitivní seznam a škála atributů i dovedností;
- vzorec pro obtížnosti a protihody;
- fyzický konflikt a jeho případná míra detailu;
- vývoj dovedností a zkušenosti v čase;
- kolik mechanických informací a hodů zobrazit hráči.
