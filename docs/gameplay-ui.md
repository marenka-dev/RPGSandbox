# Herní forma a uživatelské rozhraní

Stav: schválený hlavní směr, detailní rozvržení UI a technologické řešení se ještě navrhne.

## Hlavní koncept

RPGSandbox je **narativní textové sandbox RPG s grafickými prvky a nezávislým simulačním jádrem**. Hráč neovládá postavu klávesnicí po mapě, nýbrž rozhoduje o jejím životě a zadává akce. Inspirací v míře přímého řízení je Europa 1400: The Guild; oproti němu bude mít hra výraznou textovou a AI narativní vrstvu.

Vizuální obsah (především ilustrace lokací, portréty známých NPC, případně mapy a důležité předměty) dotváří atmosféru. Nepotřebujeme animovanou chůzi, kolize ani souvislý průchod obrovskou mapou.

## Hlavní herní obrazovka

- **Ilustrace aktuální scény:** místo, místnost nebo významná událost; nemusí vznikat při každé akci. Vizuální identita jednou vytvořených míst a postav zůstává konzistentní.
- **Narativní panel:** aktuální situace, vjemy, uskutečněné akce, dialogy a vyhodnocené důsledky.
- **Stav postavy:** totožnost, věk, místo a datum/čas, peníze, relevantní zdraví/energie a závazky, podle toho, co postava skutečně ví.
- **Kontextový panel:** známé přítomné postavy, viditelné předměty, dostupné lokace a navrhované akce.
- **Otevíratelné panely:** deník, mapa známého světa, postavy, vztahy, frakce, inventář, majetek/podnikání a kronika.

Skryté znalosti NPC či objektivní tajemství světa se hráči automaticky nezobrazují.

## Tři způsoby interakce

1. **Doporučené kontextové akce:** několik relevantních možností, aby hráč nezůstal před prázdným vstupem. Jsou to nabídky, nikoli úplný seznam povolených činností.
2. **Volný text:** hráč může přirozeným jazykem navrhnout vlastní záměr, položit otázku, jednat s NPC nebo kombinovat činnosti.
3. **Kontextové menu:** kliknutí na známou lokaci, NPC, předmět či správní panel otevře příslušnou interakci.

AI interpretuje záměr hráče, ale engine validuje dosažitelnost, potřebný čas, lokální stav, přístupové podmínky a následky. AI nemůže vymýšlet hráči neznámá fakta jako potvrzené informace ani přímo měnit kanonický svět.

## Základní herní smyčka

1. Engine připraví relevantní fakta aktuální scény a dostupné informace hráče.
2. AI odvypráví atmosféru a stav; UI zobrazí scénu a nabídku možných kroků.
3. Hráč zvolí kontextovou možnost nebo napíše vlastní akci.
4. Engine vyhodnotí proveditelnost, časovou náročnost a skutečné důsledky; složitější varianty může AI strukturovaně navrhnout.
5. Aktualizuje se svět, herní čas, znalosti, vztahy a případné události; AI odvypráví ověřený výsledek.
6. Objeví se aktualizovaná scéna a hráč pokračuje.

Nepředepisujeme hlavní quest ani povinnou profesi. Hra může nabízet podněty z reálného stavu světa, aktuálních vztahů a závazků, nikoliv uměle nutit děj.

## Cestování a lokace

Lokace používají hierarchii z Location Modelu. Hráč vybírá známá místa či popisuje cestovní záměr; není nutný ruční pohyb postavy. Engine vyhodnotí cestu, čas, dostupnost a možné události. Strategická mapa je především přehledem známých míst a nástrojem cestování, nikoli povinným ovládáním figurky.

Scéna může reprezentovat místnost hostince, ulici, obchod nebo jiné LocalArea, má známé předměty, přítomná NPC a dostupné interakce.

## Čas

Pro tuto herní formu upřednostňujeme **čas posouvaný akcemi**, nikoliv nepřetržitý real-time běh. Hráč má neomezený čas na čtení a promýšlení; potvrzený rozhovor, práce, cesta, spánek či čekání posune herní hodiny podle okolností. Při delším posunu se vzdálené a méně významné dění agreguje, důležité naplánované události se však správně vyhodnotí. Svět se v rámci herního času vyvíjí nezávisle na přítomnosti hráče; vypnutá hra standardně neběží podle skutečného kalendáře.

Konkrétní délky činností a případný volitelný režim plynulého času se ověří v prototypu.

## Ukázková úvodní scéna

Hráč se probudí v pokoji hostince v Rabenfeldu. Zobrazí se trvale přiřazená ilustrace místnosti, aktuální herní čas a krátký text o slyšitelném ruchu trhu a známých dnešních závazcích. Nabídnou se například akce: prohlédnout brašnu, podívat se z okna, sejít do výčepu, odpočinout si. Hráč může místo toho napsat libovolný vlastní uskutečnitelný záměr. Po každé akci engine upraví stav a čas a AI vypráví výsledek.

## Dopad na technologii a MVP

Simulační jádro, UI a AI zůstávají oddělené. Předběžná volba Godotu se tím znovu otevírá: pro tento typ hry může být vhodnější desktopové UI než engine určený pro přímý pohyb po mapě. O konkrétní technologii rozhodneme až porovnáním vhodných řešení.

První MVP: jedna detailní oblast, omezené množství lokací a důležitých NPC, ilustrace scén a portréty, volný text i doporučené akce, akční posun času, jednoduchý deník a napojení na skutečné simulační události.
