# World Generator v0.1

Tento prototyp je oddělený od narativního vertical slice. Jeho úkolem není chápat volný text ani generovat prózu, ale vytvořit **svět před prvním tahem hráče**.

## Co v0.1 generuje

Z jednoho deterministic seed vzniká snapshot obsahující:

- regiony a jejich klima, terén, zdroje a populaci;
- sídla a dopravní vazby;
- frakce a základní mocenské zájmy;
- rodiny;
- persistentní postavy;
- podniky a jejich vlastníky/pracovníky;
- vztahy mezi postavami;
- strukturovanou historii;
- konkrétní startovní lokální scénu;
- kontextové akce odvozené ze startovní scény.

v0.1 používá ještě jednoduchá pravidla a šablony. Cílem je ověřit **pipeline a úroveň detailu**, ne vytvořit definitivní generátor.

## Spuštění

Z kořene repozitáře:

```powershell
cd prototype\worldgen
dotnet run
```

Vlastní seed:

```powershell
dotnet run -- --seed 12345
```

Vlastní výstup:

```powershell
dotnet run -- --seed 12345 --out moje-hra.json
```

Stejný seed musí vytvořit stejný svět.

## Důležitá zásada

Startovní scéna se negeneruje z prázdna. Vychází z již vytvořeného regionu a jeho dopravních/geografických vazeb.

Budoucí pipeline:

```text
WorldConfig + Seed
        ↓
geografie a regiony
        ↓
sídla a dopravní síť
        ↓
populace a rodiny
        ↓
postavy, profese, podniky
        ↓
frakce a politika
        ↓
vztahy
        ↓
historická simulace
        ↓
současný stav světa
        ↓
výběr startovní oblasti
        ↓
lokální scéna v plném detailu
        ↓
kontextové možnosti hráče
```

## Co budeme ověřovat dál

- jak velkou část populace vytvářet persistentně při worldgenu;
- jak generovat rodokmeny bez nekonzistencí;
- jak svázat profese s ekonomikou a podniky;
- jak nechat historii skutečně měnit současný stav místo pouhého seznamu událostí;
- jak generovat lokální scénu z geografického podkladu;
- jak odvodit nabídnutelné akce čistě z přítomných entit a jejich affordances.
