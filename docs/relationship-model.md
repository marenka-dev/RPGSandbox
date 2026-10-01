# Relationship Model — vztahy mezi postavami a organizacemi

Status: schválené koncepční principy. Číselné změny a algoritmy jsou zatím návrh k testování.

## Základní pravidla

- Každý vztah je nezávislý, směrový a může být asymetrický: A může B důvěřovat, zatímco B nedůvěřuje A.
- Sympatie ani nepřátelství se automaticky nepřenášejí přes společné známé, státy, organizace nebo frakce.
- Samotná objektivní příslušnost ke znepřátelené organizaci nemění osobní vztah, pokud se o ní postava nedozví nebo ji nevnímá.
- Ani známá příslušnost nevytváří povinně změnu vztahu. Záleží na osobní loajalitě k vlastní organizaci, postoji k druhé organizaci, osobnosti, předchozích zkušenostech, důvěře ve zdroj a kontextu.
- Stejně tak spojenectví organizací automaticky nedělá z jejich členů přátele.
- Konkrétní činy mohou vztah ovlivnit, pokud se o nich postava dozví, zažije jejich důsledky nebo si vytvoří přesvědčení o tom, kdo za nimi stojí.

> Frakční nepřátelství vytváří potenciál konfliktu, nikoliv automatický osobní konflikt.

## Měření vztahů

Místo jediné souhrnné hodnoty používáme samostatné dimenze v rozmezí 0–100:

- důvěra,
- respekt,
- náklonnost,
- strach,
- závist,
- loajalita,
- odpor.

Orientace: 0–20 velmi nízká, 21–40 nízká, 41–60 střední, 61–80 vysoká, 81–100 velmi vysoká. Hranice jsou jen orientační a nesmí vytvářet náhlé mechanické skoky. Rozličné dimenze nelze sčítat do univerzální známky přátelství. Rozsahy dopadu drobných (zhruba 1–3), významných (5–15) a zásadních událostí jsou pouze hypotéza k ověření v prototypu.

Hráč standardně nevidí přesná interní čísla: vidí projevy chování a slovní popis podle svých znalostí.

## Skutečnost, znalost a interpretace

Při vyhodnocení interakce rozlišujeme:

1. **Reality:** faktická členství postav a skutečné vztahy frakcí.
2. **Knowledge:** co jedna postava ví nebo se domnívá o členství či jednání druhé osoby; včetně zdroje, jistoty a aktuálnosti.
3. **Attitude:** individuální vazba postavy k její vlastní i cizí organizaci (loajalita, hrdost, závislost, strach, ideový soulad, osobní užitek).
4. **Interpretation:** jak postava konkrétní informaci vyhodnotí s ohledem na osobnost, životní zkušenosti a dosavadní vztah.
5. **Effect:** případná změna jedné či více dimenzí osobního vztahu, uložená jako událost s příčinou.

Příklad: Johann a Mira se stanou přáteli, aniž by věděli, že jsou členy konkurenčních cechů. Později Johann odhalí Miřino členství. Jeho reakce může být neutrální, opatrná, nepřátelská nebo dokonce pozitivní podle jeho osobních zájmů a postoje k vlastnímu cechu. Miřin vztah k Johannovi se sám o sobě nemění, dokud sama nezíská relevantní zkušenost či informaci.

## Navržené datové vazby

- `CharacterRelationship`: směrové osobní hodnocení více dimenzemi.
- `FactionRelationship`: samostatný vztah organizací (napětí, obchodní spolupráce, důvěra, právní stav).
- `CharacterFactionMembership`: objektivní členství a funkce postavy.
- `CharacterFactionAttitude`: soukromý postoj postavy ke konkrétní organizaci — vlastní i cizí.
- `KnownAffiliation`: co si jedna postava myslí o příslušnosti jiné postavy, s původem informace a mírou jistoty.
- `RelationshipEvent`: proč, kdy a na základě jaké znalosti či zkušenosti se vztah změnil.

`KnownAffiliation` je možné implementovat jako specializovanou podobu stávajícího obecného Knowledge modelu, nikoli nezbytně jako oddělenou databázovou tabulku. Konkrétní implementaci určí technická architektura.

## Integrace

- **Character Model:** individuální osobnost, hodnoty, loajalita, motivace, veřejná a skrytá členství.
- **Faction Model:** objektivní vztahy frakcí a jejich historie, které nevytvářejí automatické osobní efekty.
- **Knowledge Model:** informace, omyly, zvěsti, zdroje a stupně jistoty.
- **Event Engine:** vyhodnotí reakci při získání informace nebo při konkrétním činu; změnu uloží a umožní dohledat její příčinu.
- **AI vypravěč:** dialog interpretuje podle toho, co dané NPC skutečně ví, a nemá přístup k utajeným údajům jako ke znalosti dané postavy.
