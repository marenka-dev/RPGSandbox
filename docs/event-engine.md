# Event Engine — univerzální systém událostí

Stav: schválená koncepce; konkrétní algoritmy a číselné parametry budou ověřeny prototypem.

## Základní princip

Jeden společný Event Engine obsluhuje přírodní, politické, společenské, ekonomické, osobní/vztahové i technologické události. Nevytváříme pevné seznamy desítek či stovek konkrétních událostí. Použijeme hybrid:

1. **Event Mechanisms (engine):** stabilní simulační pravidla a podporované účinky (počasí, zdroje, ekonomika, instituce, zdraví, informace, vztahy, technologie...).
2. **Event Templates (rozšiřitelné šablony):** obecné spouštěče a kombinace mechanismů, například sucho, bankrot, politická krize či konflikt mezi postavami. Výchozí knihovna může růst.
3. **Event Instances (konkrétní události):** vznikají ze skutečného stavu světa v určitém místě a čase, s příčinami, účastníky a ověřenými důsledky.

AI může na základě dostupného stavu navrhovat nové konkrétní situace, kombinovat existující mechanismy, navrhovat šablony a vytvářet jejich narativní podobu. **Engine** kontroluje podmínky, pravidla a konzistenci; jedině on potvrzuje a zapisuje změny kanonického světa. Nový typ účinku, který engine neumí provést, nelze automaticky přijmout jako nové fyzikální či simulační pravidlo.

## Oblasti událostí

- **Přírodní:** počasí, klima, geologie, přírodní zdroje, nemoci.
- **Politické a společenské:** rozhodnutí institucí, volby v systémech, které je používají, konflikty, zákony, migrace a společenská hnutí.
- **Ekonomické:** produkce, ceny, obchod, zaměstnání, majetek a podnikání.
- **Osobní a vztahové:** setkání, činy, rodina, práce, důvěra, konflikty a spolupráce.
- **Technologické a objevitelské:** výzkum, objevy, šíření znalostí, inovace a úpadek.

Kategorie se mohou překrývat: například sucho může mít ekonomické a politické důsledky. Není potřeba samostatná předem napsaná šablona pro každou variantu.

## Příčiny, následky a alternativy

Událost má vznikat z příčin a podmínek světa, nikoliv pouze proto, aby se hráč nenudil. Důsledky mohou vytvořit další události, ale řetězec není předem zaručený.

Příklad: nedostatek srážek → snížení úrody → možné zdražení obilí → možné společenské napětí. Nákup zásob, obchodní pomoc nebo jiná opatření mohou další vývoj změnit.

Události mohou být jednorázové i dlouhodobé, mít fáze, účastníky a podmínky ukončení. Konkrétní činy hráče i autonomní činnost NPC používají stejná pravidla důsledků jako události vzniklé okolním prostředím.

## Znalosti a odlišné reakce

Skutečný výskyt události, její objektivní účinky a informace jednotlivých postav jsou oddělené. Zprávy se šíří pozorováním, rozhovory a jinými zdroji; mohou být opožděné, neúplné nebo chybné. Každé NPC reaguje pouze podle svých zkušeností, znalostí, přesvědčení a motivací.

U vztahových událostí platí schválený [Relationship Model](relationship-model.md): jeden čin může u různých postav vyvolat jiné reakce nebo žádnou; neexistuje automatické přenášení sympatií ani nepřátelství přes sociální či frakční vazby.

## Škálování simulace

- Bezprostřední okolí hráče: detailní interakce a rychlá odezva.
- Město a region: agregované každodenní či periodické procesy, konkrétní důležité události.
- Vzdálené oblasti: hrubší trendy a hlavní události, které mohou pokračovat bez hráče.

Frekvence závisí také na významu události, nejen na vzdálenosti. Při pozdějším detailním generování oblastí se nesmějí přepsat již potvrzená historická fakta.

## Základní tok

1. Simulace vyhodnotí aktuální stav a možné spouštěče.
2. Pravidla nebo AI navrhnou konkrétní událost či scénář.
3. Engine ověří podmínky, přípustné účinky a vazby na existující svět.
4. Engine událost provede, případně založí dlouhodobý proces, a zaznamená příčiny i změny.
5. Znalostní systém určí, kdo se o čem dozví; jednotlivá NPC situaci individuálně vyhodnotí.
6. AI na základě ověřených faktů vypráví scénu či dialog; důsledky mohou spustit další vyhodnocení.

## Datový návrh k dopracování

Rozlišovat minimálně **EventTemplate**, **EventInstance**, **OngoingProcess** a **StateChange/EventLog**. Uchovávat identifikátory, čas a místo, aktéry, příčiny, stav, relevanci, viditelnost či šíření informací a ověřené následky. Přesné schéma, pravděpodobnosti a parametry se určí během technického návrhu.

## Ověření v MVP

Testovací scénář: jedno město, několik organizací a přibližně dvacet významných NPC. Jedna hospodářská krize spustí možné změny cen, rozhodnutí institucí a individuální reakce obyvatel. Ověřit konzistenci stavu, paměť příčin, odlišné znalosti NPC a schopnost AI situaci vyprávět.
