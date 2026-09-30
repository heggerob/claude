# Odin's Coin: den ekte verdenen og de nye skipene

Mål: Norge og landene rundt i full størrelse (1:1), bygget fra ekte høydedata, med ekte vikingtidssteder som
baser. Nye skip som er mye større og kulere enn vanlige vikingskip, litt i piratstil uten å se ut som
pirater. Utseendet er tegnet og stilisert som resten av spillet, men fysikken på vannet er ekte: styring
med ror, seil i vinden og årer.

## Plan

- [x] **1. Kartdata:** last ned ekte høyde- og dybdedata (AWS Terrain Tiles, Terrarium-formatet, som har
  både land og havbunn) for hele området fra Island til Østersjøen og fra Den engelske kanal til Nordkapp.
  Sy dem sammen, projiser til spillets flate verden (1 enhet = 1 meter) og lagre som en komprimert
  høydekart-fil i spillet. Verktøyet ligger i `tools/world/`.
  - Ferdig: `tools/world/build_map.py` henter 357 fliser (zoom 7) og lager et rutenett på 1 km over
    4 150 × 2 400 km, med land og havdybde i meter (`Resources/World/north.bytes`, 7,6 MB). Innsjøer over
    havnivå (Vänern, Ladoga) er land foreløpig, og fjordene er grove på 1 km (finere kyst kommer i punkt 3).
- [x] **2. Verdenskart i spillet (`WorldMap`):** høyde og dybde for et hvilket som helst punkt, omregning
  mellom breddegrad/lengdegrad og spillkoordinater, og et oversiktskart (`docs/worldmap.png`).
- [ ] **3. Landskap som strømmes inn:** terreng lastes i ruter rundt spilleren og fjernes bak, med ekstra
  detalj lagt på i kode (kyststein, skjær, fjell). Flytende origo, så presisjonen holder over tusenvis av
  kilometer.
- [ ] **4. Ekte steder og baser:** vikingtidens byer og handelssteder på riktig sted (Kaupang, Nidaros,
  Bjørgvin, Avaldsnes, Borg i Lofoten, Hedeby, Ribe, Roskilde, Birka, Sigtuna, Uppsala, Visby, Lindisfarne,
  Jorvik, Dublin, Kirkwall, Reykjavík, Lundenwic, Novgorod med flere), med havn, brygger og hus.
- [ ] **5. Hytter og hus:** langhus, naust, stabbur, gammer, vakttårn, palisader og brygger, tegnet i samme
  stil som figurene.
- [ ] **6. Nye skip:** egne, oppdiktede skipsklasser som er større enn langskip, med flere master, høy
  akterkastell, dragehoder og tunge årerekker. Hvert skip er en design (lengde, bredde, dypgang, vekt, master,
  seil, årer, ror).
  - Ferdig (`Ship/ShipDesign.cs`): fire klasser.
    - **Skerrycutter** (17 m): grunn og rask gjennom skjærgården.
    - **Wavewolf** (30 m): slank raider med latinseil akter, som går høyere mot vinden.
    - **Stormbreaker** (44 m): tremastet krigsgalei med akterkastell og 30 årer per side.
    - **Krakenhall** (62 m, over 1 000 tonn): firemastet flaggskip.
  - Gjenstår: modellene (utseendet).
- [ ] **7. Ekte skipsfysikk:** oppdrift fra skrogets form, motstand i vannet, sideveis grep fra kjølen,
  seil som gir kraft etter vinkelen mot den tilsynelatende vinden, ror som bare virker når skipet har fart,
  og årer som tar tak i vannet i takt.
  - Ferdig (`Ship/ShipPhysics.cs`), testet med tall:
    - **Motstand:** friksjon etter ITTC-57 og bølgemotstand, som stiger bratt forbi skrogfarten (rundt 13 knop for 30 m).
    - **Årer:** Wavewolf ror i rundt 4 knop. Ror én side og skåt den andre, så snur hun på stedet.
    - **Ror:** gjør ingenting når skipet står stille.
    - **Seil:** rundt 10 knop på slør i 16 knops vind, med 3° avdrift. Ingen seil drar rett mot vinden, og
      latinseil går høyere enn råseil.
    - **Krenging:** krigsgaleien krenger noen grader i sterk sidevind.
    - **Svinging:** små skip svinger mye raskere enn store.
  - Gjenstår: koble fysikken til skipene i spillet (oppdrift på bølgene per celle i skroget).
- [ ] **8. Seiling i praksis:** kryssing mot vinden, rev av seil i storm, ankring, fortøying ved brygga.

## Merk

- I full størrelse tar det lang tid å seile: Bergen til Trondheim er rundt 500 km, altså rundt 10 til 15
  timer i ekte fart. Skalaen er derfor en innstilling (`WorldMap.Scale`), som står på 1 (full størrelse).
- Høydedata: AWS Terrain Tiles (Mapzen), som er gratis og bygger på blant annet SRTM, GMTED og ETOPO1.
