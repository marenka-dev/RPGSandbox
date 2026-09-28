# Simulace světa

## Historie před začátkem hry

Při vytvoření nové hry by se neměl generovat pouze startovní stav, ale i minulost světa.

Simulace může vytvořit například 100–500 let historie, přičemž se neukládají stovky stran textu, ale strukturované události.

Příklad:

- rok 812 – založení města u obchodní stezky,
- rok 846 – rod Falkenů získal kontrolu nad doly,
- rok 871 – hladomor a migrace obyvatel,
- rok 903 – válka s okolním městem,
- rok 917 – mírová smlouva,
- rok 941 – vznik obchodnického cechu,
- rok 962 – smrt vládce bez dědice,
- rok 963 – spor šlechtických rodů,
- současnost – hospodářský růst, ale silné politické napětí.

Hráč vstupuje do světa, který už má vlastní příčiny a důsledky.

## Vrstvy simulace

### Geografie

- kontinenty a regiony,
- řeky,
- hory,
- lesy,
- klima,
- přírodní zdroje,
- obchodní trasy.

### Osídlení

- vesnice,
- města,
- hrady,
- přístavy,
- doly,
- významná obchodní centra.

### Populace

- kultury,
- jazyky,
- náboženství,
- migrace,
- porodnost a úmrtnost,
- nemoci,
- války,
- sociální vrstvy.

### Ekonomika

- produkce,
- spotřeba,
- ceny,
- nedostatek a přebytek,
- obchod,
- vlastnictví,
- dluhy,
- pracovní síla.

### Politika

- království,
- města,
- šlechtické rody,
- cechy,
- církve,
- zájmové skupiny,
- vztahy mezi frakcemi,
- zákony,
- povstání a války.

### Postavy

Významné postavy mohou mít:

- rodinu,
- majetek,
- ambice,
- povahu,
- rivaly,
- spojence,
- historii,
- společenské postavení,
- politické postoje.

## Kolektivní paměť a různé verze historie

Objektivní průběh události nemusí být totožný s tím, jak si ji lidé pamatují.

Příklad:

Fakt:
- král ustoupil z bitvy, protože armádě došly zásoby.

Oficiální verze:
- král zachránil armádu před obklíčením.

Nepřátelská propaganda:
- král zbaběle utekl.

Lidová verze:
- existuje posměšná píseň, která příběh ještě více překrucuje.

NPC tedy nemusí znát objektivní pravdu. Mohou znát jen informace odpovídající jejich původu, vzdělání, frakci a osobním zkušenostem.

## Level of Detail simulace

Není nutné simulovat každého člověka na světě ve stejné hloubce.

### Svět
Hrubá simulace:
- roky,
- měsíce,
- populační a ekonomické trendy,
- vztahy velkých frakcí.

### Region hráče
Střední detail:
- obchodní trasy,
- migrace,
- regionální konflikty,
- produkce a ceny.

### Město hráče
Vyšší detail:
- podniky,
- lokální politika,
- pracovní místa,
- kriminalita,
- dostupnost zboží.

### NPC v okolí hráče
Nejvyšší detail:
- osobní vztahy,
- cíle,
- vzpomínky,
- denní činnost,
- emoce,
- majetek.

## Zkonkretizování NPC

Velká část populace může existovat statisticky.

Když se běžný člověk stane pro hráče relevantní, systém jej „zkonkretizuje“ a vytvoří permanentní entitu.

Například:

- jméno,
- věk,
- rodinu,
- povolání,
- osobnost,
- majetek,
- cíle,
- vztahy,
- vizuální podobu.

Od tohoto okamžiku má postava vlastní historii a může se vracet do hry.

## Vývoj bez hráče

Při opuštění oblasti se lokální svět nezastaví.

Po návratu po několika herních letech může hráč zjistit, že:

- obchod změnil majitele,
- některé NPC zemřely nebo zestárly,
- změnila se vláda,
- vznikla nová čtvrť,
- ekonomická krize skončila nebo se zhoršila,
- starý konflikt vedl k válce nebo míru.

To je jeden z klíčových principů hry.


## Objektivní svět a poznání postav

Simulace musí oddělovat to, **co ve světě skutečně existuje**, od toho, **co o tom jednotlivé postavy vědí**.

Technologicky pokročilá civilizace může existovat ve stejném světě jako izolovaná vesnice, jejíž obyvatelé o pokročilých technologiích nemají žádné informace.

Stejný princip platí pro:

- technologie,
- geografii,
- historii,
- politiku,
- náboženství,
- magii,
- jiné kultury,
- bytosti a druhy,
- existenci jiných planet.

Postavy získávají znalosti prostřednictvím zkušeností, cestování, rozhovorů, knih, vzdělání a dalších informačních zdrojů.

Podrobný návrh je veden v [Znalostním modelu světa](knowledge-model.md).


## Počáteční generování frakcí a politiky

Generování počátečního světa musí zahrnovat nejen geografii, populaci a ekonomiku, ale také **historii organizací, frakcí a institucí**, které formovaly současný stav světa.

Engine by měl při generování vytvořit například:

- vznik a vývoj hlavních států,
- vznik městských samospráv,
- významné šlechtické rody nebo dynastie,
- cechy a obchodní organizace,
- náboženské instituce,
- armády a bezpečnostní struktury,
- politická hnutí,
- tajné a opoziční skupiny,
- významná spojenectví a rivality,
- zaniklé nebo rozdělené frakce.

Každá významná frakce by měla mít vlastní historii:

- datum a okolnosti vzniku,
- zakladatele,
- důležité předchozí vůdce,
- významné konflikty,
- změny ideologie,
- rozdělení a slučování,
- změny majetku a území,
- historické vztahy s jinými frakcemi.

### Generování institucí a funkcí

Součástí počáteční simulace je také vytvoření politických a organizačních funkcí.

Například město může mít:

- výkonnou funkci,
- radu,
- správní role,
- bezpečnostní funkce,
- ekonomické funkce,
- soudní role.

Konkrétní názvy a pravidla obsazování funkcí vycházejí z:

- období,
- kultury,
- typu vlády,
- technologické úrovně,
- historie dané společnosti.

Engine poté vygeneruje nebo přiřadí konkrétní NPC jako aktuální držitele těchto funkcí.

### Politická historie vytváří současný stav

Aktuální politika nemá vzniknout náhodným rozdáním hodnot.

Má být výsledkem předchozí historie.

Příklad:

```text
rok 811 – vznik obchodního cechu
rok 842 – cech financuje obranu města
rok 846 – získává dvě místa v radě
rok 883 – konflikt s rodem Varenů
rok 891 – starosta podporovaný cechem vyhrává volby
současnost – obchodní cech má vysoký politický vliv
```

Díky tomu má aktuální mocenská struktura vysvětlitelný původ.

### Politická historie a NPC

Počáteční generování světa ovlivňuje také charakteristiky NPC.

Postava může zdědit nebo získat:

- členství ve frakci,
- politické vazby rodiny,
- funkci nebo úřad,
- loajalitu,
- povinnosti,
- nepřátele,
- spojence,
- veřejnou reputaci,
- politické názory ovlivněné prostředím.

Tím se historie světa přímo promítá do osobních příběhů postav.

### Lazy generation i pro politiku

Stejně jako u geografie není nutné detailně vygenerovat každou organizaci ve vzdáleném světě.

Na začátku mohou existovat pouze:

- hlavní mocnosti,
- významné frakce,
- základní vztahy,
- klíčové historické události.

Lokální rady, menší cechy, konkrétní funkce a drobné politické konflikty lze zkonkretizovat až tehdy, když se oblast stane relevantní.

Jakmile jsou ale jednou vygenerovány, stávají se součástí kanonického světa.
