# RPGSandbox: lokální AI prototyp 0.6 – Engine Contract renderer

Samostatná konzolová diagnostika českého vyprávění, rychlosti odpovědí a respektování kanonického stavu světa. Zatím **nejde o hru ani o skutečně dokončený simulační engine**. `engineResult` jsou předem připravené testovací výsledky, které budoucí engine předá AI. První běh vyžaduje .NET SDK 8 a pro skutečné AI odpovědi lokálně spuštěnou Ollamu s instalovaným kompatibilním modelem.

## Spuštění na Windows (PowerShell)

```powershell
cd prototype/local-ai
dotnet run -- --mock
```

Tento režim jen ověří načtení tří scénářů, vytvoření výstupního reportu a běh programu. **Netestuje AI ani kvalitu češtiny.**

Skutečný lokální test (Qwen3 má ve výchozím nastavení vypnuté přemýšlení pro srovnatelný benchmark):

```powershell
ollama pull qwen2.5:7b
dotnet run -- --model qwen2.5:7b --report report-qwen.json
```

Pokud chceš záměrně testovat režim přemýšlení u modelu, který ho podporuje, přidej `--think`. Pro oba srovnávací běhy jej **nepoužívej**. Po aktualizaci repozitáře spusť oba testy znovu a ulož je do různých reportů:

```powershell
dotnet run -- --model qwen2.5:7b --report report-qwen25-v2.json
dotnet run -- --model qwen3:8b --report report-qwen3-v2.json
```

Nově se vypíše také počet slov a správně načtené počty tokenů, pokud je Ollama poskytne.

Pro jiný podporovaný model lze použít `--model nazev-modelu`, pro jinou lokální instanci `--url http://127.0.0.1:11434`. Pro vlastní sadu testů `--scenarios cesta-k-souboru.json`.

Pokud Ollama není spuštěná, program vypíše chybu připojení a uloží neúspěšný výsledek do reportu; nikdy nevytváří cloudové spojení sám od sebe.

## Co měřit

- dobu dokončení každého scénáře a počet vstupních/výstupních tokenů dle metrik Ollamy;
- kvalitu přirozené češtiny a sílu atmosféry;
- **věrnost faktům**: model nesmí vymyslet tajné politické informace ani změnit engineResult;
- konzistentní charakter Matěje v různých situacích;
- RAM, VRAM a GPU vytížení systémovým monitorem (tento jednoduchý prototyp je sám neměří).

Opakovat scénáře s více lokálními modely na stejném PC. Výsledky automatických časových měření jsou pomocné; kvalitu dialogů je nutné vyhodnotit ručně. Neukládejte report s osobními daty či neveřejným obsahem do veřejného repozitáře.

## Další vývoj

1. Nahradit předem daný `engineResult` skutečným validátorem a testovací simulací Rabenfeldu.
2. Přidat streamované odpovědi až po oddělení validovaných účinků od narativního výstupu.
3. Přidat sqlite perzistenci, paměť NPC a Visual Engine s lokální cache portrétů.
4. Ověřit kvalitu textu a latenci dříve, než budeme generovat obrazy.


## Benchmark 0.3 – doporučené modely

Výchozí sada je nyní `scenarios-czech-v03.json` a obsahuje pět testů: atmosférický popis, přirozený dialog, znalostní hranici, paměť NPC a neúspěšné RPG vyjednávání.

Pro současný test:

```powershell
ollama pull aya-expanse:8b
ollama pull gemma3:12b

dotnet run -- --model aya-expanse:8b --report report-aya-v03.json
dotnet run -- --model gemma3:12b --report report-gemma3-v03.json
```

Pro referenci lze znovu spustit:

```powershell
dotnet run -- --model qwen2.5:7b --report report-qwen25-v03.json
dotnet run -- --model qwen3:8b --report report-qwen3-v03.json
```

Výstupy hodnotit podle [CZECH-SCORECARD.md](CZECH-SCORECARD.md). Aya Expanse je vhodná pro experiment, ale před případnou distribucí či komerčním použitím je nutné samostatně ověřit licenci konkrétního modelu. Výběr modelu pro hru zatím není schválen.


## Benchmark 0.4 – Scene Contract

Od verze 0.4 AI už nedostává pouze obecný kontext a výsledek. Engine jí předává **Scene Contract** rozdělený na:
- kanonická fakta, která musí zachovat;
- povolenou smyslovou dekoraci, kterou smí použít bez změny světa;
- zakázaná fakta, která nesmí doplnit.

Výchozí scénáře jsou v `scenarios-scene-contract-v04.json`.

Po aktualizaci repozitáře spusť stejné scénáře na jednotlivých modelech:

```powershell
git pull
cd prototype\local-ai

dotnet run -- --model qwen3:8b --report report-qwen3-v04.json
dotnet run -- --model aya-expanse:8b --report report-aya-v04.json
dotnet run -- --model gemma3:12b --report report-gemma3-v04.json
```

Při hodnocení sleduj především:
1. přirozenost češtiny;
2. zda model nepřidává zakázané skutečnosti;
3. zda umí vytvořit atmosféru i s omezeným prostorem pro improvizaci;
4. dobu odezvy.

Cílem benchmarku 0.4 není zjistit, který model nejvíc improvizuje, ale který model nejlépe **odvypráví už rozhodnutou scénu**.


## Benchmark 0.5 – validovaný Scene Plan

Verze 0.5 přidává dvoukrokový AI pipeline:

1. **Plan:** model vrátí pouze JSON s rolemi, perspektivou a ID faktů/flavoru, které chce použít.
2. **Engine validation:** program ověří speaker, listener, perspective, povinné factId, nepovolená ID a prázdné `newFacts`.
3. **Render:** pouze validovaný plán je předán druhému AI požadavku, který vytvoří finální českou prózu.

Cílem je zachytit prohazování rolí a část halucinací ještě před zobrazením textu hráči.

Po aktualizaci:

```powershell
git pull
cd prototype\local-ai
dotnet run -- --mock
```

Pro první skutečný test doporučujeme Gemma 3 12B:

```powershell
dotnet run -- --model gemma3:12b --report report-gemma3-v05.json
```

Pro porovnání:

```powershell
dotnet run -- --model qwen3:8b --report report-qwen3-v05.json
dotnet run -- --model aya-expanse:8b --report report-aya-v05.json
```

Důležité: 0.5 používá dva AI požadavky na scénu, takže celková latence bude vyšší. Měříme zvlášť čas plánování a renderu. Pokud kvalita výrazně vzroste, později lze plánovací krok nahradit levnějším/malým modelem nebo část validace převést zcela do enginu.


## Benchmark 0.6 – Engine Contract → renderer

Výsledek 0.5 ukázal, že samostatný AI planner správně drží role, ale dělá práci, kterou už bude znát samotný herní engine. Verze 0.6 proto AI planner odstraňuje.

Pipeline je nyní:

**Engine Contract → AI renderer**

Engine předává speaker, listener, perspective, kompletní kanonická fakta, přesně povolený flavor a zakázané skutečnosti. Renderer pouze vytvoří krátký český text.

Renderer používá nižší temperature `0.18`, kratší výstup a explicitní pravidlo, že při nejistotě má detail vynechat.

Spuštění:

```powershell
git pull
cd prototype\local-ai
dotnet run -- --mock
dotnet run -- --model gemma3:12b --report report-gemma3-v06.json
```

Pro srovnání lze použít i jiné modely:

```powershell
dotnet run -- --model qwen3:8b --report report-qwen3-v06.json
dotnet run -- --model aya-expanse:8b --report report-aya-v06.json
```

Hodnotíme zejména:
- nulové nové osoby/předměty/události;
- správné role a 2. osobu jednotného čísla;
- přirozenou češtinu;
- latenci, která už neobsahuje samostatný plánovací AI krok.
