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
