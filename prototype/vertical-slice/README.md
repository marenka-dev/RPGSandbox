# RPGSandbox – Rabenfeld vertical slice v0.1

První souvislá hratelná konzolová smyčka. Už nejde o izolovaný AI benchmark.

## Co prototyp umí

- lokální kanonický stav světa v SQLite;
- herní čas postupující pouze podle akcí;
- tři lokace: pokoj U Tří korun, společenská místnost hostince a rabenfeldské náměstí;
- Matěje s jednoduchou perzistentní pamětí;
- Havla a kanonickou cenu obilí 4 groše za míru;
- jednoduchou deterministickou interpretaci českého vstupu;
- Engine Contract pro každou přijatou akci;
- lokální Gemma 3 12B přes Ollamu pouze jako renderer prózy;
- fallback bez AI při nedostupné Ollamě;
- historii přijatých akcí v tabulce `world_events`.

Engine mění stav světa **před** narací. AI nikdy nezapisuje do SQLite a nemůže měnit kanonický výsledek.

## Spuštění

Z kořene repozitáře:

```powershell
cd prototype\vertical-slice
dotnet restore
dotnet run -- --model gemma3:12b
```

Databáze se vytvoří jako `rabenfeld-v01.db` v aktuálním adresáři.

Pro test bez AI:

```powershell
dotnet run -- --no-ai
```

Pro nový začátek napiš ve hře `reset`.

## Příklady vstupu

```text
podívám se z okna
sejdu dolů za Matějem
poprosím Matěje o snídani a zeptám se, jestli je dnes rušno
řeknu Matějovi, že dnes půjdu na trh a potom na radnici
rozloučím se s Matějem
půjdu na náměstí
prohlédnu si trh
zkusím Havla přesvědčit, aby mi prodal obilí za polovinu
vrátím se do hostince
```

Příkazy `stav`, `pomoc`, `reset`, `konec` jsou systémové.

## Záměr v0.1

Intent parser je úmyslně jednoduchý a deterministický. Další krok bude oddělit interpretaci libovolného vstupu od samotné simulace tak, aby i případná AI interpretace vracela pouze strukturovaný návrh akce, který engine validuje. Renderer zůstává oddělený a nikdy není autoritou nad stavem světa.
