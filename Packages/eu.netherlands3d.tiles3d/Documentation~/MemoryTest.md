# Memory Test (GLB)

Deze debugtoolset bevat `MemoryTestGlbLoader`, `MemoryTestGlbLoaderUI` en `MemoryStatsGraph`. Samen vormen ze een herbruikbare klassenset die GLB’s batch-gewijs kan laden én ontladen terwijl het geheugengebruik stabiel wordt gehouden. De loader is los te hergebruiken in de toekomstige TileKit zodat je dezelfde laadlogica buiten deze testscene kunt inzetten.

## API-sleutel instellen

- **Via URL**: voeg `?apikey=<jouw_key>` toe aan de launch-URL; de loader leest dit automatisch in.  
- **In de editor**: gebruik het tekstveld in de MemoryTest UI; de waarde wordt direct aan de loader doorgegeven.

## Stress-test gebruiken

- De DevScene bevat de benodigde componenten en is bereikbaar via `Shift + Escape`; dit werkt alleen in development builds.  
- Start of stop de test met de knoppen in de UI; tijdens een run laadt de loader batches GLB’s, ontlaadt na de ingestelde timers en herhaalt de cyclus.  
- Houd de grafiek in de UI in de gaten: doel is dat allocated/reserved geheugen na elke cyclus terugkeert naar een stabiel niveau.

## Testdata-sync

De loader verwacht `TestData/3dtiles_transforms.csv`. Het editorscript `TestDataResourceSync` (met `[InitializeOnLoad]` en build-hooks) regelt dit automatisch:

1. Zodra Unity de editor-assemblies laadt, triggert `[InitializeOnLoad]` een sync: `Packages/eu.netherlands3d.tiles3d/TestData` wordt gekopieerd naar `Runtime/Resources/TestData`, zodat de CSV altijd klaarstaat tijdens debuggen en development builds.  
2. Tijdens `OnPreprocessBuild` wordt gekeken of je een release build start; dan verwijdert het script de Resources-kopie vóór het bouwen en zet die na afloop (`OnPostprocessBuild`) weer terug.

Zo belandt de testdata nooit in productieplayers, maar hoef je er in de editor niets voor te doen.
