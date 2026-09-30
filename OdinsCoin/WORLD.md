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
- [x] **3. Landskap som strømmes inn:** terreng lastes i ruter rundt spilleren og fjernes bak, med ekstra
  detalj lagt på i kode (kyststein, skjær, fjell). Flytende origo, så presisjonen holder over tusenvis av
  kilometer.
  - Ferdig (`WorldTerrain`, `TerrainDetail`, `WorldOrigin`).
    - Ruter på 1 km lastes rundt skipet, over et grovt lag på 128 km ut mot horisonten.
    - Bakken får farge etter hva den er: sand, gress, fjell eller snø.
  - Kystlaget (`tools/world/build_detail.py`, `Resources/World/coast.bytes`, 21,6 MB):
    - 1 975 ruter på 25 km med 200 m oppløsning langs alle kyster, bygget fra zoom-11-fliser.
    - Fjorder og sund er med, og vannet på 0 m regnes som sjø.
    - Mälaren er havbukt, slik den var i vikingtiden.
    - Se [docs/coast-bergen.png](docs/coast-bergen.png), [docs/coast-lofoten.png](docs/coast-lofoten.png) og
      [docs/coast-oslofjord.png](docs/coast-oslofjord.png).
  - Ikke koblet inn i selve spillet ennå (se punkt 4).
- [x] **4. Ekte steder og baser:** vikingtidens byer og handelssteder på riktig sted (Kaupang, Nidaros,
  Bjørgvin, Avaldsnes, Borg i Lofoten, Hedeby, Ribe, Roskilde, Birka, Sigtuna, Uppsala, Visby, Lindisfarne,
  Jorvik, Dublin, Kirkwall, Reykjavík, Lundenwic, Novgorod med flere), med havn, brygger og hus.
  - Ferdig (`World/Places.cs`): 26 steder på ekte koordinater (byer, haller, klostre, Jomsborg). Alle har en
    havn som er minst 2,5 m dyp, funnet på det ekte kartet.
  - Ferdig (`World/Settlements.cs`, `World/RealWorld.cs`): hvert sted får en brygge ut til havna og husene
    sine på tørt, flatt land, vendt mot sjøen. Stedene bygges når skipet kommer innen 25 km, og tas ned bak
    deg. Spillet starter nå i den ekte verdenen: hjemmeøya ligger i åpent vann 3 km utenfor Kaupang.
    Terrenget har kollidere, så skipet kan gå på grunn og du kan gå i land.
  - Det flytende origoet flytter hele spillverdenen, kameraet og bølgene sammen, så havet ikke hopper.
  - HUD-en viser nærmeste havn, med avstand og kurs.
  - Ikke testet i selve Unity ennå.
- [x] **5. Hytter og hus:** langhus, naust, stabbur, gammer, vakttårn, palisader og brygger, tegnet i samme
  stil som figurene.
  - Ferdig (`World/Buildings.cs`, se [docs/buildings.png](docs/buildings.png)), i ekte størrelser:
    - **Langhus:** buede vegger og torvtak.
    - **Jarlens storhall:** 48 m, med spontak, kryssede dragehoder på gavlene og forgylte dørstolper.
    - **Naust:** steinvegger og åpen mot sjøen.
    - **Stabbur:** på stolper med steinheller.
    - **Vakttårn:** med vardekurv.
    - **Palisade:** med spisse stokker.
    - **Brygge:** på påler.
    - **Kirke:** liten, i stein, med klokkegavl, til klostrene.
  - Plassert ved de ekte stedene (se punkt 4).
- [x] **6. Nye skip:** egne, oppdiktede skipsklasser som er større enn langskip, med flere master, høy
  akterkastell, dragehoder og tunge årerekker. Hvert skip er en design (lengde, bredde, dypgang, vekt, master,
  seil, årer, ror).
  - Ferdig (`Ship/ShipDesign.cs`): fire klasser.
    - **Skerrycutter** (17 m): grunn og rask gjennom skjærgården.
    - **Wavewolf** (30 m): slank raider med latinseil akter, som går høyere mot vinden.
    - **Stormbreaker** (44 m): tremastet krigsgalei med akterkastell og 30 årer per side.
    - **Krakenhall** (62 m, over 1 000 tonn): firemastet flaggskip.
  - Modellene (`Ship/ShipModel.cs`, se [docs/ships.png](docs/ships.png)): klinkbygd, tjæret skrog med stigende
    ripe og gullstriper, dragehode med horn og tenner i baugen, halekrøll akter, rød-svart stripete råseil med stor
    rune, svarte latinseil, akterkastell med lykter på de store, skjoldrader langs ripa, årerekker og et hengslet ror.
    Flaggskipet har en hall på dekket. Målene kommer fra designet, så utseende og fysikk stemmer overens.
- [x] **7. Ekte skipsfysikk:** oppdrift fra skrogets form, motstand i vannet, sideveis grep fra kjølen,
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
  - Koblet inn i spillet: du starter nå med en **Wavewolf** (30 m, 94 tonn) i stedet for det gamle langskipet.
    - Hun flyter på bølgene med oppdrift per celle i bunnen, og vannet, vinden, seilene, årene og roret
      virker på henne etter de ekte tallene.
    - Mannskapet svinger roret over på rundt 2 sekunder og setter eller tar ned seilene på rundt 8 sekunder.
    - Rår brasses rundt mot vinden, så langt riggen tillater, og seilduken samles opp mot råa når seilene tas ned.
    - Hun har dekk du kan gå på, rekker, ror, alteret til Odins mynt, og kan gå på grunn på den ekte havbunnen.
    - Ror du mens du legger roret over, drar yttersiden hardere.
  - De danske raiderne seiler nå Skerrycutters (17 m, 21 tonn) med svart-rødt seil og den samme ekte fysikken.
    Wavewolf er fire ganger så tung, så en ramming fra deg rister dem mye mer enn deres rister deg.
- [x] **8. Seiling i praksis:** kryssing mot vinden, rev av seil i storm, ankring, fortøying ved brygga.
  - Ferdig (`Ship/Seamanship.cs`):
    - **Vann over ripa:** vannet renner inn der ripa er under bølgeflaten, enten skipet krenger i vinden eller
      baugen graver seg inn i en sjø (overløp: 1,7 · lengde · dybde^1,5). Ligger ripa 30 cm under, er Wavewolf
      full på rundt ett minutt.
    - **Storm og rev:** stormen blåser nå full storm (41 knop). Med fulle seil krenger Wavewolf 15°, revet
      til en firedel bare 4°. Rev med Q.
    - **Anker (G ved roret):** holder der det er under 40 m dypt. Tauet ligger slakt til det strekkes, og drar
      ankeret hvis draget blir større enn det holder: ankeret holder skipet uten seil i 28 knop, men ikke med
      fulle seil i full storm.
    - **Fortøying (G ved brygga):** fortøyninger fra baug og hekk til pullertene på brygga. Reisen starter
      fortøyd hjemme, og hver brygge ved de ekte stedene har pullerter.
    - HUD-en varsler når du tar inn vann eller ankeret drar.
    - **Kryssing:** fysikken regner ut hvor høyt hvert skip går mot vinden i vinden som blåser nå (Wavewolf 51°,
      den råriggede Skerrycutter 55° i 16 knop). HUD-en viser seilingen: slør, halvvind, bidevind, og «in irons»
      med de to kursene du må krysse på for å komme opp mot vinden.

## Merk

- I full størrelse tar det lang tid å seile: Bergen til Trondheim er rundt 500 km, altså rundt 10 til 15
  timer i ekte fart. Skalaen er derfor en innstilling (`WorldMap.Scale`), som står på 1 (full størrelse).
- Høydedata: AWS Terrain Tiles (Mapzen), som er gratis og bygger på blant annet SRTM, GMTED og ETOPO1.
