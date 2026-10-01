# RPGSandbox: lokální AI prototyp 01

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
