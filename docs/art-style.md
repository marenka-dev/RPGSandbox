# Grafický styl a pixel art

## Směr

Pro první verze hry je preferovaný pixel-art styl.

Důvody:

- nízká produkční náročnost,
- dobrá stylová konzistence,
- menší citlivost na drobné AI chyby,
- snadné ukládání a opakované použití,
- vhodnost pro portréty, lokace, mapy, předměty i UI.

## Základní pravidlo

Grafika důležité postavy se negeneruje při každém zobrazení znovu.

Po vytvoření vizuální identity se uloží jako asset navázaný na permanentní ID postavy.

## Vizuální profil NPC

Postava může mít uložené:

- základní slovní popisy vzhledu,
- barvu vlasů,
- oči,
- typ oblečení,
- věk,
- významné znaky,
- barevnou paletu,
- základní portrét,
- varianty emocí,
- sprite pro mapu.

## Varianty generování postav

### A. Jednorázově generovaný portrét

Při prvním významném setkání vznikne pixel-art portrét a dále se používá jako kanonická podoba.

Výhody:
- jednoduché,
- vhodné pro hlavní NPC.

Nevýhody:
- náročnější na konzistenci při tvorbě nových póz.

### B. Modulární portréty

Postava se skládá z vrstev:

- obličej,
- oči,
- účes,
- vousy,
- oblečení,
- doplňky,
- barevné varianty.

Výhody:
- vysoká konzistence,
- rychlost,
- nízké náklady,
- snadné emoce a změny oblečení.

### C. Hybrid

Preferovaný dlouhodobý směr:

- běžná NPC používají modulární systém,
- hlavní NPC mohou mít unikátní AI nebo ručně vytvořený portrét,
- vizuální identita zůstává uložená.

## Grafické vrstvy hry

Pixel art lze využít pro:

- portréty NPC,
- ikony inventáře,
- malé sprity na mapě,
- obrázky lokací,
- znaky frakcí,
- budovy,
- jednoduché scény,
- mapu města nebo regionu.

## Generování scén

Není nutné generovat nový obrázek po každém tahu.

Nový obraz je vhodný například:

- při vstupu do nové lokace,
- při významné události,
- při prvním setkání s důležitou postavou,
- při velké změně prostředí.

Běžné dialogy mohou používat existující lokaci a portréty postav.

## Ukládání assetů

Každý asset by měl mít stabilní ID.

Například:

- character_id,
- portrait_id,
- sprite_id,
- location_art_id.

Díky tomu lze grafiku znovu používat a verzovat bez změny identity postavy.


## Aktualizace výtvarného směru a Visual Engine

Na základě schválených ukázkových obrazovek preferujeme pro **velké scény a portréty detailní, atmosférický malovaný styl** s konzistentními postavami a prostředím. Dříve preferovaný pixel art zůstává možnou úspornou alternativou a inspirací pro malé grafické prvky; není nadále povinným stylem všech ilustrací. UI: tmavší elegantní historizující rámy a jemné zlaté akcenty, ale dobrá čitelnost textu má přednost před dekoracemi.

**Visual Engine** odděluje požadavky na grafiku od AI a simulace. Pracuje s trvale přiřazenými assety podle ID světa, lokace, postavy či události:

1. Ze strukturovaných kanonických dat získá stabilní vizuální popis a ověří existující odpovídající grafiku.
2. Pokud obrázek existuje, načte jej bez generování.
3. Nový obrázek vytvoří jen podle potřeby, například při prvním důležitém setkání či odkrytí nové významné lokace; při běžných akcích se znovu používá stejný podklad.
4. Výsledek uloží a přiřadí ke kanonické entitě; pozdější varianty musí zachovat identitu i architektonické či geografické charakteristiky.
5. Pokud generování není dostupné nebo není hotové, zobrazí vhodnou stávající ilustraci či připravený zástupný obrázek; hraní nesmí být blokováno.

**Dvě cesty pro NPC:** významné postavy mají individuálně vytvořené a uložené portréty; méně významné postavy využívají předpřipravenou či modulárně skládanou knihovnu obličejů a oblečení. Proměny vzhledu (věk, výraz, šaty) musí zachovat rozpoznatelnost.

**Města a lokace:** vytvořit trvalou vizuální identitu z geografie, kultury, historie, technologie a známých staveb. Pro opakované návštěvy používat uložená panoramata/interiéry; méně významné místnosti smějí používat kompatibilní obecné ilustrace. Nové obrazy pro významné události (např. katastrofa, slavnost) vytvářet výběrově, nikoliv při každém tahu.

**Výkon a náklady:** připravit sadu počáteční lokace a důležitých postav při vzniku hry; ostatní generovat až při potřebě či s předstihem u pravděpodobných příštích míst. Cache assetů a knihovna variant mají minimalizovat čekání, volání generativních modelů i spotřebu úložiště. Konkrétní službu, licencování výstupů, formáty a výkon ověříme při technickém návrhu.
