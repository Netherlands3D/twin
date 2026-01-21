# Memory Test (GLB, B3DM)

De DevScene is bedoeld om GLB- en B3DM-bestanden geheel los van de reguliere tilehandler te laden en ontladen. Daardoor kun je geheugen- en performancegedrag in isolatie meten en het effect van een dataset op de browserheap analyseren.

`GlbLoader.cs` download en plaatst de GLB en B3DM bestanden. Deze class kan hergebruikt worden in andere projecten, waarbij je de zekerheid hebt dat het geheugengedrag is getest.

## Testdata capture maken 

Om de stress-test te kunnen uitvoeren, moet je eerst testdata vastleggen door tiles te capturen tijdens een normale sessie:

1. Schakel in de editor via het menu **Netherlands3D → GLB URL + Position Capture → Start Capturing** de recorder in.  
2. Open een dataset (bijvoorbeeld via de gewone tilesetviewer) en navigeer rond; voor elk geladen bestand wordt een regel geschreven naar `Packages/eu.netherlands3d.tiles3d/TestData/capture{n}.csv`.  
3. Stop de sessie via hetzelfde menu. Je kunt zoveel captures maken als nodig; de Memory Test UI toont alle gevonden CSV's automatisch in de dropdown.

Elke regel in de CSV bevat URL, positie, rotatie en schaal, bijvoorbeeld:

```
https://www.3drotterdam.nl/.../13865.b3dm,-136.5234,-43.21904,455.0938,-1.479171E-08,0.9999689,-2.115634E-08,-0.007889595,1,1,1
```

## Stress-test uitvoeren

De stress-test laadt en ontlaadt herhaaldelijk tiles om het geheugengedrag te analyseren.

### Test opstarten

1. Voer de test uit in een **browser (development build)**; de Memory Test scene heeft een onscreen grafiek waarmee je het geheugengebruik kunt volgen.  
2. Open de DevScene in de build met **Shift + Escape** (alleen beschikbaar in development builds).  
3. Kies een capture-CSV in de dropdown en start de test.  
4. De loader shufflet de volgorde zodat elke run dezelfde tiles in een andere volgorde laadt.

### Download-instellingen

De **Max Parallel Downloads** instelling (1-6) bepaalt hoeveel tiles tegelijk worden gedownload:

- **1 (sequentieel)**: Ideaal voor gedetailleerde geheugenanalyse per tile. Elke tile wordt volledig geladen voordat de volgende start.
- **6 (parallel)**: Simuleert het echte productiegedrag (browserlimiet). Tot 6 tiles worden tegelijk gedownload, wat overeenkomt met de reguliere tilehandler.

**Tip**: Start met sequentieel (1) om memory leaks makkelijker te identificeren. Gebruik parallel (6) om te testen hoe de applicatie zich gedraagt onder productieomstandigheden.

### Geheugenanalyse

- Houd de memorygrafiek in de UI in de gaten tijdens het draaien van de test.  
- In een browser krimpt de heap niet automatisch; het doel is om te zien hoe disposed geheugen wordt hergebruikt terwijl de test blijft doorlopen.  
- Door herhaaldelijk laden en ontladen kun je memory leaks identificeren en controleren of het geheugen correct wordt vrijgegeven.

## Google API-sleutel

Voor het testen van Google 3D Tiles (Reality Mesh) is een API-sleutel nodig:

- **Via URL-parameter**: start de player met `?apikey=<jouw_key>`; de loader leest deze parameter en vult de requests automatisch aan.  
- **In de editor**: gebruik het veld *Google API key* in de UI; wijzigingen worden direct toegepast.

## Testdata-sync (automatisch)

De loader zoekt CSV's in `Packages/eu.netherlands3d.tiles3d/TestData` en in `Runtime/Resources/TestData`. Het editorscript `TestDataResourceSync` houdt deze mappen gesynchroniseerd en voorkomt dat testdata in productiebuilds terechtkomt:

1. **In de editor**: `[InitializeOnLoad]` kopieert automatisch `Packages/eu.netherlands3d.tiles3d/TestData` naar `Runtime/Resources/TestData` zodra Unity de editor-assemblies herlaadt.  
2. **Bij het builden**: tijdens `OnPreprocessBuild` wordt de Resources-map verwijderd voor release builds; `OnPostprocessBuild` zet de testdata daarna weer terug.

Dit voorkomt dat grote testbestanden in productiebuilds terechtkomen (kleinere build-size), terwijl je in de editor alle testdata direct beschikbaar hebt zonder handmatig te kopiëren.
