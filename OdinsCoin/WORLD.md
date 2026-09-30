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
  - Riggen på skipene fra 25 m og oppover har fått mer av de store havseilernes stil (uten å bli piratskip):
    - toppmaster over råa med lange, kløftede vimpler i husets farge og en gullknapp;
    - vanter i en vifte fra ripa til mastetoppen, med ratliner å klatre i, og en utkikkstønne på stormasta;
    - et baugspryd med forstag til formasta og vaterstag ned til stevnen.
    - på Stormbreaker og Krakenhall et forkastell med tinner å skyte mellom, og gullbånd som akterkastellet;
    - rader med firkantede åreporter langs skroget, med røde lokk slått opp.
  - Seilplanen er den samme, så fysikken er uendret.
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
    - **Grunnstøting:** et forsiktig puff mot en sandbanke gjør ingenting. Treffer du grunnen i fart, blir bordgangen
      slått inn, med flere hull jo fortere du går. I 10 knop får skipet tre hull. Hullene må tettes, og vannet må
      øses ut.
    - **Kryssing:** fysikken regner ut hvor høyt hvert skip går mot vinden i vinden som blåser nå (Wavewolf 51°,
      den råriggede Skerrycutter 55° i 16 knop). HUD-en viser seilingen: slør, halvvind, bidevind, og «in irons»
      med de to kursene du må krysse på for å komme opp mot vinden.
- [x] **9. Skipsbyggeren:** kjøp større skip i mjødhallen (fanen «Shipwright»).
  - Skerrycutter koster 400 gull, Stormbreaker 2 500 og Krakenhall 6 000. Alle starter med en Wavewolf.
  - Skipene du har kjøpt er dine, og du bytter mellom dem ved brygga. Lasten og Odins alter flyttes over.
  - Flåten og skipet du seiler lagres.
  - Hvert skip har sin egen plass ved brygga, flytende og klar av stranda.
  - Du kan klatre om bord fra brygga eller fra vannet når du står ved siden av skipet.
- [x] **10. Liv ved de ekte stedene:**
  - **Plyndring:** klostre (4 kister, 3 vakter), haller (2 kister, 4 vakter), festninger (3 kister, 7 vakter) og
    landingsplasser har kister å ta og vakter rundt hovedbygningen. Vaktene stiller opp når skipet er innen 3 km.
  - Når alle kistene er båret bort, er stedet plyndret resten av reisen, og Odins gunst stiger.
  - **Markedsbyer:** har en handelsmann på brygga som kjøper kister, båret til ham eller rett fra skipet.
    Hedeby betaler best (1,3x), så Birka, Jorvik, Dyflin og Visby.
  - HUD-en viser hva det nærmeste stedet er: marked, kloster, jarlehall, festning, eller plyndret.
  - Vakter, stormen, sjøormen og piler følger med når origo flyttes.
  - Husene er solide, og brygga har et dekk du kan gå ut på til skipet.
  - Kister og vakter står alltid på tørt land og utenfor husene. Det er testet for alle stedene.
- [x] **11. Lange overfarter:** i full størrelse er Kaupang–Hedeby 500 km, 34 timer i 8 knop.
  - Ferdig (`Core/TimeWarp.cs`): T ved roret gir 1×, 2×, 4×, 8× eller 16× tid på en rolig overfart. Tiden går
    tilbake til normal hvis raidere kommer innen 600 m, sjøormen dukker opp, det blir storm, skipet går på grunn,
    eller du forlater skipet. HUD-en viser «TIME ×8».
  - Ferdig (`UI/SeaChart.cs`, se [docs/chart.png](docs/chart.png)): sjøkartet (M) er hele Norden tegnet på
    pergament fra det ekte kartet. Havet er blått etter dybden, landet oker etter høyden, fjellene har skravur og
    kystene er blekket. Kartet viser stedene (marked ◆, kloster ✚, andre ●, plyndrede er gråe) og skipet ditt med
    kursen, og har en målestokk. Rull for å zoome inn 2×–16× rundt skipet.
  - Ferdig (`Ship/Passage.cs`): etter 16× gir T en lang overfart. Skipet seiler 240 ganger raskere på den samme
    fysikken, men flatt og i steg på et kvarter sekund, og holder kursen selv. A/D svinger kursen. Kaupang–Hedeby
    tar da rundt 9 minutter. Overfarten stopper ved grunt vann foran baugen, og av alt som stopper rask tid.
    Den går ikke fra anker eller fortøyning. En test seiler en time på slør: kursen holdes innen 1°, og hun gjør
    10,5 knop.
- [x] **12. Lagret posisjon:** lagringen husker hvor skipet ligger ute i den ekte verdenen, med kursen, og
  «Continue» setter deg tilbake der med mannskapet om bord (hvis det fortsatt er åpent vann). Ligger skipet
  hjemme, starter du ved brygga som før.
- [x] **13. Havner du faktisk kommer inn til:** en test viste at 12 av 26 havner lå i innestengt vann, i dammer eller
  innsjøer uten forbindelse til havet.
  - Havnene finnes nå med et flomsøk fra åpent hav (`Places.FloodHarbour`), som bare godtar vann et skip kan seile
    inn fra sjøen. De bakes til `Resources/World/harbours.txt` med `tools/world/bake_harbours.sh`.
  - Elver og sund som er for smale for kartets 200 m-rutenett er gravd inn som seilløp (`World/Channels.cs`):
    Themsen til Lundenwic, Humber og Ouse til Jorvik, Schlei til Hedeby, Roskildefjorden, Byfjorden ved Tønsberg,
    og vannveien fra Sigtuna til Mälaren. Jorvik og Lundenwic har nå havn ved selve byen i stedet for 40–50 km unna.
  - Den oppdiktede detaljen dempes ved vannlinjen, så grunne kyster ikke blir til falske øyer og innsjøer.
  - En test seiler fra hver havn (og fra hjemmeøya) ut til åpent hav på et rutenett på 50 m.
  - Bilder av stedene ovenfra med husene, brygga, havna, kistene og vaktene: [Kaupang](docs/place-kaupang.png),
    [Lindisfarne](docs/place-lindisfarne.png), [Hedeby](docs/place-hedeby.png).
- [x] **14. Døgnet og den ekte sola (`Core/SkyClock.cs`):**
  - Klokka går med skipets egen tid, så rask tid og lange overfarter får døgnene til å gå.
  - Sola står der den faktisk står for breddegraden der skipet er og årets dag. Sesongen starter i slutten av mai.
  - Midt på sommeren går sola rundt hele natta i Lofoten. Ved Kaupang står den 54° høyt ved middag og går bare litt
    under horisonten om natta, så natta blir lys og blå. Om vinteren står den ikke opp i Lofoten.
  - Himmel, tåke og lys følger sola: blågrått om dagen, varm glød ved horisonten, blå skumring og mørkeblå natt.
    Stormen mørkner dette ytterligere.
  - HUD-en viser dag og klokkeslett, og tiden lagres.
- [x] **15. Handelsskip (`Sea/Merchant.cs`):** opptil tre handelsskip seiler rundt deg ute på det ekte havet.
  - Det er Skerrycutters i handelsfarger, på vei til en av de nærmeste markedsbyene, og de bruker den samme fysikken.
  - De styrer mot havna, ror når vinden er for langt forut, og svinger unna grunt vann.
  - Legger du deg langs siden, stryker de seilet uten kamp, og kistene på dekk er dine.
  - De forsvinner når de når havna, eller når du er mer enn 15 km unna.
- [x] **16. Oppdrag fra Bjørn (`Hall/Commissions.cs`):** mjødhallen har en ny fane, «Commissions».
  - Bjørn tilbyr tre raid på ekte steder som ikke er plyndret ennå: det nærmeste, og så steder lenger unna.
  - Bonusen øker med avstanden hjemmefra og med antall vakter. Borre, like ved, gir 230 gull; Reykjavík gir 1 120.
  - Du har ett oppdrag om gangen. HUD-en viser avstand og kurs dit, og bonusen utbetales idet stedet er plyndret.
  - Oppdraget og de plyndrede stedene lagres.
- [x] **17. Le for vinden (`World/WindShelter.cs`):** landet tar vinden. I le av en øy eller en fjellside dør
  brisen ut, omtrent tolv ganger landets høyde nedover, som i fjordene. I Bergen havn faller østavinden fra fjellene
  til 15 %, mens vestavinden fra havet blåser fullt. Skipet merker det, og HUD-en viser «in the lee of the land».
- [x] **18. Strømmer (`World/Currents.cs`):** vannet beveger seg.
  - De store tidevannsstrømmene går hardt og snur med tidevannet hver 12,42 time: Saltstraumen opptil rundt 10 knop,
    Moskstraumen utenfor Lofoten, Pentland Firth og Corryvreckan.
  - Den norske kyststrømmen setter nordover langs kysten.
  - Skroget arbeider gjennom vannet og seilene får vinden over grunnen, så en motstrøm kan holde deg igjen og en
    medstrøm kan bære deg. Strømmen tar også skipet med på lange overfarter.
  - HUD-en viser strømmen og retningen den setter når den er merkbar.
- [x] **19. Sjøen følger vinden:** en svak bris krusser bare havet, frisk vind bygger opp sjø, og i le av landet
  (inne i en fjord eller bak en øy) ligger sjøen mye roligere. Stormen kommer på toppen. Bølgehøyden styrer også
  hvor mye vann som skylles inn over ripa.

- [x] **20. Verden i øyehøyde (se [docs/scene-kaupang-fp.png](docs/scene-kaupang-fp.png)):** siden spillet nå spilles i førsteperson, viser forhåndsvisningen også havna slik du ser den når du står på brygga.
  - Lavt land er gress. Sand er det bare nærmest vannkanten, der det før var sand opp til 2,5 m over havet.
  - [x] Landskapet rundt deg (`World/Scenery.cs`): furu og bjørk i skogholt der støyen sier skog, busker, gresstuer, steiner og kampesteiner, og drivved på strendene. Samme sted har alltid de samme trærne. Ingenting vokser på tomtene i byene eller rundt hjemmehavna, eller under tidevannslinja. Stammer og kampesteiner er faste, men du går gjennom gress og busker. Det bygges i ruter på 64 m, der alt med samme farge slås sammen til én mesh, og holdes på rundt 350 m rundt deg.
  - [x] Folk i byene (`World/Townsfolk.cs`): bønder, fiskere og handelsfolk i hjemmevevde klær, uten våpen. De rusler fra dørstokk til dørstokk og går aldri gjennom et hus. De blir stående en stund og ser seg rundt, og de går unna hvis du går rett bort til dem. En by har 4–12 innbyggere, en gård eller et kloster 2–5. Har du plyndret stedet, har folket flyktet.
  - [x] Fra roret (se [docs/scene-helm.png](docs/scene-helm.png)): rormannen står et steg til styrbord for midtlinja, så mesanmasten ikke står rett foran øynene når du styrer i førsteperson.
  - [x] Åkrer med gjerde (`World/Fields.cs`): bak eller ved siden av langhusene, hallene og stabburene ligger en inngjerdet åker med furer av vendt jord og korn i rader. Kornet er modent bygg (gull) eller grønne spirer. Den ligger bare på tørt, nokså flatt land, klar av alle hus og andre åkrer. Gjerdet har stolper og to rekker med rekkverk som følger bakken, og en grind mot huset. Du går inn i gjerdet, men kommer inn gjennom grinda. Ingen trær vokser i åkrene. En by har inntil tre åkrer, en gård eller et kloster inntil to.

## Merk

- I full størrelse tar det lang tid å seile: Bergen til Trondheim er rundt 500 km, altså rundt 10 til 15
  timer i ekte fart. Skalaen er derfor en innstilling (`WorldMap.Scale`), som står på 1 (full størrelse).
- Høydedata: AWS Terrain Tiles (Mapzen), som er gratis og bygger på blant annet SRTM, GMTED og ETOPO1.
