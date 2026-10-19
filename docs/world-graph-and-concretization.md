# Generování světa, World Graph a lazy concretization

Stav: schválený architektonický směr.

## Zásadní princip

Svět se **nevytváří podle otázek hráče**. Při založení nové hry vznikne kanonický makrosvět a jeho historie. Některé jeho části jsou konkrétní od začátku, jiné zůstávají úmyslně v nižším rozlišení.

Hráčova otázka nebo akce může vyvolat rozbalení dosud neurčeného detailu, ale nový detail musí být konzistentní s již existujícím světem.

> **Svět existuje před hráčem v určitém stupni rozlišení. Interakce hráče pouze určuje, kde je potřeba zvýšit jeho detail.**

## Čtyři vrstvy

### 1. Makrosvět

Generuje se při založení hry a po simulaci historie.

Obsahuje například:
- geografii;
- města a regiony;
- velikost a strukturu populace;
- významné rody a postavy;
- politické a institucionální vazby;
- podniky a významná hospodářská aktiva;
- výrobní a obchodní vazby;
- kulturu a technologickou úroveň;
- historii a důležité události.

Makrosvět může například určit, že město má přibližně 2 800 obyvatel, určitý počet řemeslníků a několik hostinců, aniž by každý obyvatel měl při startu plný profil.

### 2. Konkretizovaný kanonický svět

Důležité entity jsou vytvořené konkrétně už při startu nebo během historie:
- významná NPC;
- rodiny;
- budovy;
- firmy;
- instituce;
- důležité předměty.

Každá persistentní entita má stabilní ID.

### 3. Lazy detail

Dosud neurčené části lze konkretizovat až ve chvíli, kdy jsou relevantní.

Příklady:
- anonymní člen populační skupiny se stane konkrétním NPC;
- běžná nabídka hostince dostane konkrétní položky;
- interiér budovy dostane konkrétní běžný předmět;
- dosud nerozlišený podnik dostane konkrétního provozovatele.

Concretizer však nesmí přepisovat minulost ani porušit existující omezení.

Pokud například makrosvět říká, že ve městě žádný hodinář není, dotaz hráče nesmí hodináře vytvořit. Pokud má hostinec zákaz alkoholu, dotaz na pivo nesmí pivo přidat.

Jakmile je detail konkretizován, stává se kanonickým a persistentním.

### 4. Znalostní graf

Objektivní svět a znalosti postav jsou oddělené.

```text
OBJECTIVE WORLD:
Jan --debt--> 20 grošů

PLAYER KNOWLEDGE:
Jan --debt--> UNKNOWN
```

Query Engine vždy pracuje s tím, co smí daná postava vědět nebo právě vnímat.

## World Graph

Kanonický svět lze reprezentovat obecnými trojicemi:

```text
subject --predicate--> object/value
```

Například:

```text
Matěj --profession--> hostinský
Matěj --location--> U Tří korun
Havel --sells--> obilí
židle --carryable--> true
dveře --blockable--> true
```

To umožňuje dotazovat svět obecněji než přes ručně psané odpovědi.

## Vazba na Semantic Actions

Semantic Interpreter nepřistupuje přímo k databázi a nesmí tvořit kanonické skutečnosti.

Pipeline:

```text
hráčův přirozený text
        ↓
Semantic Interpreter
        ↓
Query / Action / budoucí ActionPlan
        ↓
World Graph + Knowledge Filter + Affordance Rules
        ↓
případná bezpečná concretization
        ↓
kanonický výsledek
        ↓
Scene Contract
        ↓
Narrative Renderer
```

## Concretization policy

Každá kategorie lazy detailu musí mít jasný zdroj omezení.

Například nové NPC:
- vzniká z existující populační skupiny;
- jeho profese nesmí porušit strukturu místní ekonomiky;
- věk a rodinné vztahy musí být slučitelné s demografií;
- bydliště musí existovat nebo být vytvořeno v rámci kapacity lokality;
- nesmí zpětně měnit již známé historické skutečnosti.

Nová obchodní položka:
- musí odpovídat typu podniku;
- technologické a kulturní úrovni;
- dostupnosti surovin a obchodním trasám;
- cenovému modelu;
- již existujícím zákazům či nedostatkům.

## Perzistence

Jakmile concretizer vytvoří detail, uloží se stejně jako ostatní kanonická fakta. Při dalším spuštění hry se znovu negeneruje.

Cílem je, aby hráč nikdy nepoznal rozdíl mezi entitou vytvořenou při worldgen a entitou konkretizovanou později: obě jsou po vzniku plnohodnotnou součástí stejného světa.
