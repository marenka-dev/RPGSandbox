# Technická architektura – Local First

Stav: schválený architektonický směr. Konkrétní modely a framework pro UI vybereme až na základě prototypu a testů.

## Cíl

RPGSandbox bude plnohodnotná desktopová hra. Uživatel si při instalaci zvolí způsob provozu AI. Preferovaný režim je **Local First**: hra, databáze, svět, paměť, grafika a volitelné AI modely jsou lokálně na počítači hráče. Po stažení potřebných komponent může lokální režim fungovat bez připojení a bez průběžných poplatků za AI volání. Volitelně bude možné používat cloudovou či hybridní AI, nikoliv jako nutnost.

## Nezávislé komponenty

- **Desktop UI:** narativní scéna, ilustrace, dialog, stav postavy, deník, mapy, vztahy, frakce, inventář, majetek.
- **Simulační jádro:** pracovní volba C#/.NET; jediná autorita nad herními pravidly, stavem světa, hody, ekonomikou a Event Enginem.
- **Perzistence:** pracovní volba lokální SQLite pro kanonická fakta, události, stavy, znalosti a paměť; samostatně uložené obrazové assety. Světy i zálohy zůstávají ve vlastnictví hráče.
- **AI Gateway:** jednotné rozhraní pro lokální textovou AI i nepovinné cloudové poskytovatele, výměna modelu bez změny uloženého světa. AI interpretuje záměr a vytváří vyprávění, ale nemění kanonický svět.
- **Visual Engine:** nejprve vyhledá lokální asset a načte ho, nový generuje jen podle potřeby; uchovává trvalou podobu postav a lokací. Preferuje uloženou grafiku a knihovnu obecných ilustrací.

Pro první verzi preferujeme modulární lokální aplikaci, nikoli síť mikroslužeb. Grafický framework desktopové aplikace dosud není definitivně vybrán; vzhledem k textové herní formě otestovat UI vhodné pro narativní panely.

## Režimy při prvním spuštění

1. **Lokální:** průvodce otestuje OS, RAM, CPU, GPU, VRAM a disk; nabídne kompatibilní jazykový model a případně lokální obrazový model. Stažení může vyžadovat internet, následně mohou modely běžet lokálně. Bez poplatků za jednotlivé požadavky, ale za cenu zatížení hardwaru a spotřeby elektřiny.
2. **Hybridní:** lokální základ a možnost podle přání přepnout náročnější textové nebo obrazové úkoly na cloud.
3. **Cloudový:** lokální hra a databáze, ale AI přes externího poskytovatele; odesílání kontextu musí být volitelné, transparentní a pod kontrolou hráče.

Přepínání režimů nesmí poškodit uložený svět. Lokální AI server se má vázat pouze na místní zařízení; žádné automatické odesílání herní historie či telemetrie mimo počítač. U cloudového provozu transparentně určit rozsah odesílaných dat.

## Volitelné technické kandidáty

- Lokální textové modely: **Ollama** pro první prototyp; případně **llama.cpp** pro pozdější těsnější integraci. Není rozhodnuto, jaký konkrétní model zvolíme.
- Obrazové generování: pro experimenty **ComfyUI** nebo kompatibilní lokální řešení; do hry se nedostane jako povinná samostatně ovládaná aplikace.
- Finální UI: otestovat desktopové možnosti odpovídající textovému RPG s ilustracemi. Nepředpokládat, že Godot je definitivní volba.

Distribuce modelů musí zohledňovat jejich licence, velikost a kompatibilitu s hardwarem.

## Latence a náklady

- Jednoduché činnosti (inventář, ekonomika, pravidla, pohyb času, hody) vypočítá místní engine bez AI.
- AI kontext sestavovat jen z relevantních faktů, znalostí daného NPC, aktuálního děje a potřebných vzpomínek; neposílat modelu celou databázi.
- Významné ilustrace lze připravit na začátku hry, ostatní generovat při prvním důležitém setkání nebo s předstihem. Generování obrázku nesmí blokovat hraní: zobrazit již dostupný obraz nebo placeholder.
- Textové požadavky omezovat vhodným spojením úloh, cache a rozpočtovými pravidly; zachovat konzistenci a schvalování výstupů enginem.
- Ve volitelném cloudovém režimu měřit a zobrazovat spotřebu a umožnit limity.

## Rizika a rozhodovací test

Rizika: náročnost lokálního obrazového generování, delší odezvy slabších počítačů, různá kvalita češtiny a konzistence mezi lokálními modely, distribuční licence a velikost instalace.

**Prototypovací test:** na běžném herním PC odehrát souvislou českou scénu v hostinci s několika NPC a jejich vzpomínkami; měřit kvalitu vyprávění, konzistenci, dobu odezvy, spotřebu RAM/VRAM a chování při chybějícím obrázku. Podle výsledků vybrat konkrétní modely, UI framework a doporučené HW profily.
