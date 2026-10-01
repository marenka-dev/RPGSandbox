# Ekonomický model RPGSandboxu

Stav: schválená koncepce. Konkrétní algoritmy a technická implementace budou ověřeny prototypem.

## Hlavní principy

- Ekonomika je součástí živého světa a funguje i bez hráče.
- Stejná hospodářská pravidla platí pro hráče a NPC.
- Konkrétní hospodářská situace vychází z geografie, zdrojů, historie, politiky, infrastruktury a technologií jednotlivých civilizací.
- Svět nepoužívá jednu univerzální cenu zboží: trhy jsou regionální a konkrétní obchodníci se mohou lišit.
- Vzdálené oblasti se simulují agregovaně, blízké relevantní podniky a NPC podrobně.
- Peníze a zboží nevznikají bez vysvětlitelného zdroje; při přechodu mezi úrovněmi detailu musí sedět zásoby i hospodářské výsledky.

## 1. Ekonomické entity

- **Postava:** zaměstnání, příjmy, výdaje, potřeby, majetek, úspory, dluhy, cíle.
- **Podnik:** vlastník, provozovatel, zaměstnanci, výrobní kapacity, vybavení, zásoby, náklady, tržby a závazky.
- **Organizace:** společný majetek, finanční prostředky, investice, obchodní zájmy a případně podniky.
- **Region:** dostupné zdroje, produkce, spotřeba, skladové zásoby, trhy a obchodní trasy.

Postavy mohou současně zastávat více hospodářských rolí (například být zaměstnancem i vlastníkem). Vlastnictví podniku se odděluje od jeho každodenního řízení; podnik může mít více vlastníků i institucionálního vlastníka.

## 2. Výroba a spotřeba

Výroba musí vycházet z dostupných vstupů a nesmí automaticky vytvářet hotové zboží. Výrobní proces vyžaduje podle kontextu:

- suroviny a případně palivo či energii,
- vybavení, technologii a odpovídající infrastrukturu,
- pracovní sílu a související dovednosti,
- čas, kapacitu a provozní náklady.

Výsledkem jsou určité množství a případně kvalita výrobků. Pokud klíčový vstup chybí, výroba se omezí nebo zastaví. Výrobní receptury a nové produkty se odvozují z technologické úrovně, prostředí a podporovaných simulačních mechanismů. AI může navrhovat nové kombinace a popisy, jejichž proveditelnost ověřuje engine.

Spotřeba postav, podniků a regionů snižuje dostupné zásoby podle jejich skutečných či agregovaných potřeb.

## 3. Trhy a obchod

Každý relevantní region má vlastní tržní stav: nabídku, poptávku, zásoby a místní cenovou hladinu. Ceny ovlivňuje především:

- lokální produkce a spotřeba,
- dostupnost zásob,
- dopravní a obchodní infrastruktura,
- čas, vzdálenost a náklady přepravy,
- cla, daně a regulace,
- bezpečnost cest a politické vztahy,
- sezónnost a mimořádné události.

Konkrétní transakce může mít individuální cenu podle vyjednávání, smluv, osobních vztahů a okolností. Mezi regiony lze obchodovat jen při fyzicky a institucionálně proveditelném propojení. Konkrétní cenotvorbu včetně případné měny, směn a obchodních omezení ještě navrhneme a otestujeme.

## 4. Podnikání, zaměstnání a vlastnictví

Hráč i NPC mohou pracovat, zakládat podniky, najímat zaměstnance, obchodovat, investovat, vlastnit nebo pronajímat majetek a uzavírat smlouvy. Podnik má odlišného vlastníka či vlastníky a provozovatele. Ekonomické podmínky mohou vést k růstu, změně vlastnictví, omezení výroby či bankrotu.

Model majetku zahrne movitý a nemovitý majetek, podíly, zásoby a relevantní práva. Finanční model eviduje příjmy, výdaje, pohledávky, půjčky, dluhy, splatnost, daně a dědictví tam, kde odpovídají kultuře, právnímu řádu a době.

## 5. Osobní ekonomika a motivace NPC

Významné NPC mají detailnější ekonomickou situaci: zdroje příjmů, životní náklady, majetek, závazky, ekonomické cíle a individuální preference. Hospodářská situace ovlivňuje jejich cíle, rozhodování, vztahy a dostupné možnosti. Běžná vzdálená populace může být modelována agregovanou spotřebou a zaměstnaností; při zkonkretizování NPC musí vzniknout stav konzistentní s regionálním modelem.

## 6. Historie, technologie a instituce

Počáteční ekonomický stav musí vznikat z historie světa: osídlení, obchodních cest, konfliktů, objevů, katastrof, rozhodnutí institucí a dlouhodobého vývoje. Výrobní procesy a ekonomické instituce odpovídají technologické úrovni jednotlivých civilizací: řemeslné dílny, průmysl, automatizace či meziplanetární logistika používají kompatibilní základní mechanismy, ale odlišné technologie a organizaci práce.

Frakce a politické instituce mohou upravovat práva vlastnictví, daně, cla, přístup k povoláním, obchodní dohody i bezpečnost tras. Jejich konkrétní dopad určuje skutečný stav světa, nikoli pevný scénář.

## 7. Integrace s Event Enginem

Ekonomika používá společný [Event Engine](event-engine.md) a jeho mechanismy, nikoli samostatný izolovaný generátor. Přírodní, politické či osobní události mohou změnit produkci, logistiku, zaměstnanost a finance. Ekonomické důsledky mohou vyvolat další události a změnit vztahy. AI může navrhovat situace a popisovat je, ale pouze engine ověřuje a zapisuje kanonické změny.

## 8. Koncepční datové objekty

- `EconomicEntity`: hospodářsky aktivní postava či organizace.
- `Asset` a `OwnershipShare`: majetek, vlastnictví a podíly.
- `Business`: podnik s odděleným vlastnictvím a provozem.
- `ProductionProcess`: technologicky přípustná pravidla výroby a její nároky.
- `Resource`: surovina, výrobek či služba.
- `Inventory`: zásoby vlastníka v konkrétním umístění.
- `RegionalMarket`: lokální nabídka, poptávka a ceny.
- `TradeRoute` a `Shipment`: propojení trhů a skutečná přeprava.
- `EconomicTransaction`: hospodářské transakce a převody.
- `Contract`: pracovní poměry, dodávky, půjčky a další závazky.

Jsou to koncepty, nikoli závazné databázové tabulky. Detailní struktura, zaokrouhlování a finanční evidence vzniknou při technickém návrhu.

## 9. Prototyp ekonomiky

Ověření na Rabenfeldu: zemědělci → mlýn → pekárna → místní spotřeba a obchod. Simulovat nedostatek obilí, reakci cen a zásob, možnost dovozu, zásah cechu a individuální rozhodování několika NPC. Kontrolovat konzistenci převodu zásob, peněz, příčiny změn a návaznost na Event Engine.

Viz také [návrh struktury ekonomického modulu](economy-code-structure.md).
