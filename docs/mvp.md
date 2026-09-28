# MVP

## Cíl

První verze nemá simulovat celý kontinent.

Má ověřit, zda základní princip hry funguje:

> Je živý AI sandbox stále konzistentní a zábavný i po mnoha hodinách hraní?

## Doporučený rozsah

### Svět

- jeden region,
- jedno hlavní město,
- několik okolních lokací,
- základní lokální historie.

### NPC

- přibližně 20–40 významnějších NPC,
- vztahy,
- zaměstnání,
- základní osobnost,
- cíle,
- dlouhodobá paměť,
- uložený portrét.

### Hráč

- vytvoření postavy,
- základní statistiky,
- peníze,
- inventář,
- reputace,
- vztahy.

### Herní čas

- den / noc,
- postup času při akcích,
- základní změny světa v čase.

### AI

- volný textový vstup hráče,
- AI vypravěč,
- dialogy NPC,
- reakce na hráčovy akce,
- přístup pouze k relevantnímu kontextu.

### Persistovaný svět

- databáze,
- save/load,
- event log,
- základní snapshoty,
- shrnutí dlouhodobé paměti.

### Grafika

- jednoduché pixel-art UI,
- portréty hlavních NPC,
- několik obrázků lokací,
- ikony základních předmětů.

## Co zatím nedělat

Do MVP zatím nezařazovat:

- celý kontinent se stovkami měst,
- detailní simulaci milionů obyvatel,
- rozsáhlý bojový systém,
- komplexní animace,
- plnou ekonomiku všech regionů,
- generování nového obrázku po každém tahu,
- detailní crafting,
- multiplayer.

## Test úspěchu MVP

MVP je úspěšné, pokud:

1. svět drží konzistenci,
2. NPC si pamatují podstatné události,
3. vztahy se dlouhodobě vyvíjejí,
4. hráč může dělat neočekávané věci,
5. AI respektuje fakta světa,
6. uložené vizuální identity zůstávají konzistentní,
7. po 5–10 hodinách hraní svět stále působí živě,
8. hra vytváří zajímavé situace bez předem napsané hlavní dějové linie.

## První prototyp

Je možné začít ještě menším vertikálním prototypem:

- jedno město,
- hostinec,
- tržiště,
- několik dalších lokací,
- 10–15 NPC,
- jednoduché vztahy,
- základní ekonomika,
- několik simulovaných lokálních problémů,
- AI vypravěč.

Takový prototyp má především ověřit paměť a konzistenci.
