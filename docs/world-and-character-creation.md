# Tvorba světa a hráčské postavy

## Základní princip

Tvorba nové hry má spojovat tři věci:

1. **kdo je hráčská postava,**
2. **v jakém typu světa žije,**
3. **co o tomto světě skutečně zná.**

Hráč nemá definovat konkrétní historii světa, státy, rody, konflikty nebo ceny. Nastavuje především pravidla reality a minulost své postavy. Engine z toho vygeneruje konkrétní svět, vztahy, znalosti a počáteční situaci.

Základní pravidlo:

> **Hráč definuje svou minulost a pravidla světa. Engine vytvoří konkrétní realitu a z minulosti hráče odvodí, co umí, koho zná a kam vůbec může vědomě vstoupit.**

---

## 1. Tvorba hráčské postavy

Hráč může zadat nebo zvolit například:

- jméno,
- věk,
- místo narození,
- sociální původ,
- rodinné prostředí,
- povolání rodičů,
- prostředí dětství,
- vzdělání,
- významné události před začátkem hry,
- předchozí cestování,
- počáteční majetek,
- případně základní osobnostní rysy.

Tyto volby nejsou pouze příběhový text. Musí mít přímé herní důsledky.

### Příklady původu

#### Rodina zemědělců

Může odvodit:

- základní znalost zemědělství,
- práci se zvířaty,
- znalost počasí a sezón,
- lokální geografii,
- kontakty mezi sedláky,
- omezenější znalost vzdáleného světa.

#### Rodina učenců

Může odvodit:

- čtení a psaní,
- historické znalosti,
- přístup ke knihám,
- širší znalost světa,
- základy vědy, medicíny nebo filozofie,
- společenské kontakty mezi vzdělanci.

#### Rodina obchodníků

Může odvodit:

- počítání,
- vyjednávání,
- znalost cen,
- obchodní kontakty,
- znalost cest a trhů,
- širší geografickou orientaci.

#### Rodina cestovatelů

Může odvodit:

- širší mapu známého světa,
- více jazyků nebo dialektů,
- znalost jiných kultur,
- více známých lokalit,
- zkušenost s cestováním,
- menší vazbu na jednu konkrétní komunitu.

Původ postavy tedy přímo ovlivňuje zejména:

```text
skills
knowledge
relationships
known_locations
social_status
starting_assets
beliefs
```

---

## 2. Lifepath místo formuláře

Tvorba postavy by neměla působit jako vyplnění desítek izolovaných polí.

Vhodnější je několik životních kapitol.

### Dětství

Například:

- zemědělská vesnice,
- hornické město,
- obchodní přístav,
- šlechtické sídlo,
- klášter,
- kočovná komunita.

### Rodina

Například:

- zemědělci,
- řemeslníci,
- obchodníci,
- učenci,
- vojáci,
- cestovatelé,
- šlechta.

### Vzdělání

Například:

- žádné formální,
- rodinné učení,
- učednictví,
- klášterní škola,
- soukromý učitel,
- univerzita,
- vojenský výcvik.

### Významná zkušenost

Například:

- válka,
- hladomor,
- nemoc,
- obchodní cesta,
- stěhování,
- ztráta člena rodiny,
- služba v armádě,
- dlouhé cestování.

### Poslední roky

Například:

- práce,
- studium,
- služba,
- cestování,
- podnikání,
- tuláctví,
- péče o rodinu.

Každá volba upravuje stav postavy a může přidávat:

- dovednosti,
- znalosti,
- vztahy,
- majetek,
- sociální status,
- známé lokace,
- přesvědčení,
- životní zkušenosti.

---

## 3. Nastavení světa hráčem

Hráč nenastavuje konkrétní obsah světa, ale jeho základní pravidla a hranice.

### Maximální technologická úroveň

Tento parametr představuje **potenciální technologický strop světa**, nikoliv aktuální stav všech civilizací.

Možné úrovně:

- prehistorická,
- starověká,
- středověká,
- industriální,
- moderní,
- blízká budoucnost,
- meziplanetární,
- mezihvězdná.

Příklad:

```text
technology_ceiling = interstellar
```

Neznamená to, že svět už umí cestovat mezi hvězdami. Znamená to, že vývoj světa se k tomu za vhodných podmínek může někdy dopracovat.

Aktuální technologická úroveň jednotlivých civilizací je generována enginem.

---

## 4. Fantasy parametry

Fantasy by neměla být pouze přepínač ano/ne.

Možné dimenze:

```text
fantasy:
  magic_exists: true
  prevalence: low
  public_awareness: rare
  creatures: moderate
  divine_intervention: very_low
```

To dovoluje vytvořit například:

- realistický svět,
- low fantasy,
- klasickou fantasy,
- high fantasy,
- dark fantasy,
- svět, ve kterém magie existuje, ale většina obyvatel o ní neví.

Fantasy nastavení musí respektovat znalostní model. Objektivní existence magie neznamená, že o ní hráč nebo NPC vědí.

---

## 5. Další parametry světa

Aby tvorba nebyla přehlcená, většina nastavení by měla být řešena přes několik jednoduchých voleb nebo presetů.

Možné parametry:

### Realismus

- historicky realistický,
- lehce stylizovaný,
- výrazně fantastický.

### Nebezpečnost světa

- klidnější,
- standardní,
- tvrdý svět.

Může ovlivňovat:

- kriminalitu,
- války,
- hladomory,
- úmrtnost,
- bezpečnost cestování.

### Politická nestabilita

- stabilní,
- dynamická,
- chaotická.

### Ekonomická komplexita

- jednoduchá,
- standardní,
- hluboká.

### Sociální mobilita

- rigidní společnost,
- střední,
- otevřená.

V rigidním světě je společenský postup obtížnější. V otevřeném světě může být snazší přejít mezi sociálními vrstvami.

---

## 6. Počáteční měřítko simulace

Hráč může zvolit počáteční rozsah detailní simulace:

- lokální,
- regionální,
- kontinentální,
- globální.

Tato volba není absolutní hranicí světa.

Příklad:

```text
initial_simulation_scope = local
potential_extent = planetary
```

Svět tedy může být rozsáhlý, ale na začátku se detailně simuluje pouze relevantní oblast.

---

## 7. Počáteční lokace

Hráč si nemá vybírat startovní lokaci z úplné mapy světa.

Nabídka startovních míst vychází výhradně z toho, co jeho postava **skutečně zná ze svého života**.

Příklad:

Postava vyrůstala jako syn zemědělce a nikdy necestovala daleko.

Známé lokace:

- rodná vesnice,
- okolní pole,
- les,
- sousední tržní městečko.

Počáteční nabídka může obsahovat například:

- rodnou vesnici,
- tržní městečko,
- cestu mezi oběma.

Pokud lifepath obsahuje:

> Ve 12 letech jsi s otcem navštívil hlavní město.

pak se hlavní město přidá mezi možné počáteční lokace.

Tím je tvorba postavy přímo propojena se znalostním modelem.

---

## 8. Co generuje engine

Engine generuje konkrétní realitu světa.

Hráč by neměl při tvorbě hry určovat například:

- konkrétní státy,
- konkrétní politické konflikty,
- konkrétní šlechtické rody,
- přesné ceny,
- detailní historii,
- přesné rozmístění měst,
- konkrétní technologickou úroveň každé civilizace.

Důvodem je zachování překvapení a pocitu objevování.

Engine z hráčových parametrů vygeneruje například:

- svět a jeho historii,
- rodnou oblast,
- rodinu hráče,
- známé NPC,
- počáteční sociální vazby,
- známé lokace,
- počáteční dovednosti,
- počáteční znalosti,
- počáteční majetek,
- současný politický a ekonomický stav,
- důvod, proč příběh začíná právě teď.

---

## 9. Proč příběh začíná právě teď

Hra může vygenerovat nenucený spouštěcí okamžik života postavy.

Například:

- právě zemřel rodič,
- skončilo učednictví,
- válka skončila,
- postava byla propuštěna ze služby,
- rodina ji vyslala na cestu,
- přišla o práci,
- vrací se po letech domů,
- rozhodla se opustit dosavadní život.

Nemusí jít o klasický quest.

Jde pouze o okamžik, kdy začínáme sledovat další život postavy.

---

## 10. Odvozené znalosti a počáteční svět

Při vytvoření hry může engine pracovat například s tímto vstupem:

```text
PlayerSeed
- age: 24
- origin: farming_village
- family: farmers
- education: basic
- travel_history: local

WorldSeed
- technology_ceiling: interstellar
- starting_era_bias: medieval
- fantasy_level: low
- realism: high
- political_instability: medium
- danger: standard
- simulation_scope: local
```

Engine z toho vytvoří:

```text
PlayerState
- skills: generated
- knowledge: generated
- relationships: generated
- known_locations: generated
- social_status: generated
- starting_assets: generated
```

a současně konkrétní svět, do kterého tato minulost přirozeně zapadá.

---

## 11. Návrh budoucího datového rozdělení

Z této koncepce přirozeně vycházejí tři oddělené objekty:

### WorldConfig

Neměnné nebo dlouhodobé parametry reality:

- technologický strop,
- fantasy pravidla,
- realismus,
- úroveň nebezpečí,
- sociální mobilita,
- základní pravidla simulace.

### WorldState

Aktuální stav konkrétně vygenerovaného světa:

- datum,
- civilizace,
- technologický stav,
- politika,
- ekonomika,
- historie,
- vygenerované regiony,
- současné události.

### PlayerOrigin

Minulost hráčské postavy:

- původ,
- rodina,
- dětství,
- vzdělání,
- životní události,
- cestování,
- předchozí vztahy.

PlayerOrigin následně generuje výchozí PlayerState a Knowledge.


---

## 12. Politické a frakční generování při vytvoření světa

Počáteční generování světa musí vytvořit také politickou a institucionální vrstvu.

Engine z WorldConfig a vygenerované historie odvodí například:

- hlavní státy a mocnosti,
- lokální samosprávy,
- významné frakce a organizace,
- formy vlády,
- politické funkce,
- pravidla obsazování funkcí,
- aktuální držitele klíčových úřadů,
- vztahy mezi frakcemi,
- současné politické konflikty,
- historické důvody aktuálního rozložení moci.

Tato vrstva musí být propojena s generováním hráčské postavy.

PlayerOrigin může být například ovlivněn tím, že:

- rodina patří k určitému cechu,
- rodič zastává veřejnou funkci,
- rod je politicky významný,
- rodina podporuje konkrétní hnutí,
- postava vyrůstala v regionu s politickým konfliktem,
- minulá válka nebo převrat ovlivnil její dětství.

Hráč však nemusí znát celou objektivní politickou realitu. Jeho počáteční znalosti se opět odvozují pouze z původu, vzdělání a životních zkušeností.
