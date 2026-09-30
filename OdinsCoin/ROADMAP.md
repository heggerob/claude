# Odin's Coin – roadmap

Ett punkt om gangen. Hvert punkt kompileres mot den falske Unity-API-en (`tools/check.sh`) før det pushes.

> Midlertidig plassering: Koden ligger i mappen `OdinsCoin/` på branchen `claude/odins-coin` i `heggerob/claude`,
> fordi Claude ikke kan opprette nye repoer. Når du lager et tomt repo `odins-coin` på GitHub og gir Claude
> tilgang, flyttes alt dit.

- [x] **1. Grunnmur:** oppstart fra kode, hav med bølger (mesh som animeres), himmel, lys, tåke og tredjepersonskamera.
- [x] **2. Langskipet:** skrog laget i kode, oppdrift som følger bølgene, seil, ror og vind.
- [x] **3. Vikingen:** gå om bord mens skipet beveger seg, ta roret, heise seilet og hoppe i vannet.
- [x] **4. Odins mynt:** alteret om bord, myntkast med animasjon, innsats, velsignelser og forbannelser med varighet, og odds som vises.
- [x] **5. Øyer:** genererte øyer med strender, trær, klostre og skattekister.
- [x] **6. Kamp:** øks, sverd og skjold, og saksiske vakter med enkel AI.
- [x] **7. Bytte og gull:** bære kister om bord og levere dem i hjemmefjorden (Gunnar på brygga kjøper dem, også rett over ripa).
- [x] **8. Runer:** gravere runer i mynten (6 runer, 3 plasser), endre oddsen, lykke-meter og ravnene Hugin og Munin.
- [x] **9. Methallen:** selge bytte, oppgradere skipet og utstyret (seil, årer, skrog, brynje, øks), terningspill mot Bjørn og skrytetavle.
- [x] **10. Farer på havet:** storm (regn, lyn, tåke, vann i skroget og øsing), danske vikingskip med bueskyttere og ramming, og Jormungand.
- [x] **11. Lyd og meny:** generert lyd (25 lyder, hav/vind/regn-løkker), tittelskjerm, pausemeny, innstillinger og automatisk lagring.

## Del 2: Ny retning (se [DESIGN.md](DESIGN.md))

Sea of Thieves med vikinger og Zelda-utforsking, enkelt og artig. Gamblingen er å satse skatten om bord.

- [x] **12. Satse skatten ved alteret (`Coin/Stake.cs`):** legg en kiste på alteret og kast. Kron gir neste trinn (vanlig, sølv, gull,
  Odins skatt, dobbel verdi), mynt betyr at Odin tar den. «Alt eller ingenting» satser hele dekket. Oddsen vises
  alltid. Velsignelser og forbannelser fjernes fra mynten. Vinner du, blinker kista i gull. Taper du, blir den til en sverm av ravner som flyr til himmels.
- [x] **13. Skatten er i fare om bord (`Sea/Wreck.cs`):** synker skipet, synker kistene og kan dykkes opp igjen der det sank. Kistene ligger i vannkanten rundt en brukket mast som flyter over stedet. Vraket vises på HUD-en (avstand og retning) og på sjøkartet til siste kiste er hentet.
- [x] **14. Enkel styring (`HelmOrders` i `Ship/Longship.cs`):** W og S for seil og A og D for ror ved roret. Anker og fortøyning skjer av seg selv ved
  brygga. Færre taster og menyer.
- [ ] **15. Verdensskala** (du bestemmer): krympet verden, eller full størrelse som nå.
- [x] **16. Landemerker (`World/Landmarks.cs`, se [docs/buildings.png](docs/buildings.png)):** hvert sted får noe høyt eller lyst som synes langt unna. Byer og festninger har et 22 m vardetårn med bål på toppen, klostre et klokketårn, jarlens hall en kjempestor ask, og landingsplasser en runestein som lyser blått. Det står på det høyeste tørre stedet i nærheten, klar av hus og åkrer.
- [x] **17. Utsiktspunkter (`UI/ChartReveal.cs`):** sjøkartet starter som blankt pergament, bortsett fra hjemmefarvannet. Mens du seiler, tegnes kysten rundt skipet inn (6 km). Står du ved et landemerke og trykker E, klatrer du opp i vardetårnet, klokketårnet eller asken, eller leser runesteinens kart, og området i 25 km rundt tegnes inn. Det som er kartlagt, lagres.
- [x] **18. Runeringer (`World/RuneShrines.cs`):** rundt hvert landemerke står en ring av fem små steiner hugget med 1 til 5 hakk. Rør dem i rekkefølge etter hakkene, så våkner ringen og gir sin gave. Rører du feil stein, blir alle mørke igjen. Gavene er Vitalitetens rune (+15 helse), Utholdenhetens rune (mer pust til klatring og svømming) og Hellets rune (+2 % bedre odds ved alteret, maks 60 %). Lagres.
- [x] **19. Klatring og utholdenhet (`Player/StaminaRules.cs`):** gå mot en bratt klippe og hold W, så klatrer du opp i 1,6 m/s til pusten tar slutt. Over kanten får du et lite hopp opp. Sprint og hard svømming tar også pust. Går du tom, blir du sliten og må hente deg inn før du kan klatre eller sprinte igjen. På beina kommer pusten tilbake på noen sekunder. En grønn linje under siktet viser pusten, og den blir rød når du er sliten. Utholdenhetens rune gir 3 sekunder ekstra.
- [x] **20. Nedgravde skatter og skattekart (`World/Hoards.cs`, `World/HoardGuide.cs`):** hvert sted har en skatt gravd ned 350–800 m ute, merket med en varde og et kryss i torva. Trykk E ved varden for å grave opp en sølvkiste (150–300 gull). Omtrent hver tredje kiste du plyndrer har et kart til nærmeste skatt som ikke er gravd opp. Kartet setter et kryss på sjøkartet og viser avstand og retning på HUD-en, og en ravn sirkler over varden når du er innen 2 km. Lagres.
- [x] **20b. Grotter (`World/Caves.cs`):** ved hvert sted er det gravd en grotte inn i den bratteste bakken i nærheten, med åpningen ned mot dalen (på flatt land blir den en gravhaug med åpningen mot byen, der draugen holder til; før fikk bare 6 av 26 steder en grotte, nå får alle). Inne ligger en gullkiste (250–450 gull), og en draug (en død viking i grått) vokter inngangen. Bakken inne i grotta er gravd ut (terrengrutene rundt grotta tegnes i 1 m ruter i stedet for 25 m, så kammeret faktisk synes), og en tømt grotte forblir tom. Ryktene kan handle om grotter.
- [x] **21. Rykter i stedet for oppdragsliste (`World/Rumours.cs`):** trykk E ved en innbygger for å høre et rykte om et rikt sted som ikke er plyndret, en nedgravd skatt eller en runering som sover. Ryktet sier omtrent hvor langt og i hvilken retning («half a day's sail to the south-west»), men setter ingen markør. Nærmere steder nevnes oftere, og ingen rykter handler om stedet du står på eller det du allerede har tatt. Innbyggeren stopper og snur seg mot deg mens du snakker. Bjørns oppdrag i methallen finnes fortsatt.
- [x] **22. Første tur (`UI/FirstSteps.cs`):** en ny spiller får ett hint om gangen øverst på skjermen: gå om bord, ta roret, sett seil, finn en kiste, bær den om bord, og sats den ved alteret eller selg den. Hvert hint forsvinner når det er gjort (eller hoppes over hvis du gjør noe senere først), og etter første kiste som er satset eller solgt er hintene borte for godt. Lagres.
- [x] **23. Ravnefjær (`World/Feathers.cs`):** tre fjær fra Odins ravner er gjemt rundt hvert sted, på det høyeste punktet i et lite område ute bak husene (en haug, en rygg, en klippetopp). De svever og snurrer sakte. Går du inn i en, tar du den: 15 gull og litt av Odins gunst, og banneret viser hvor mange av alle du har funnet. Ved 10, 25 og 50 fjær, og når du har alle, gir Hugin og Munin ekstra gull (100, 300, 600 og 1500). Innbyggerne forteller av og til om en fjær i nærheten. Lagres.
- [x] **24. Mannskap på dekk (`Ship/Crew.cs`):** fire karer i hjemmevevde klær står langs dekket og følger med, og hvert nivå av Årer-oppgraderingen i methallen gir to til (opptil ti). Når du satser ved alteret, jubler de med armene i været hvis Odin smiler, og henger med hodet hvis han tar kista (som i DESIGN.md: «mannskapet heier eller stønner»). Når skipet går for årer, trekker de i takt med årene. Ellers ser de utover sjøen. De følger med når du bytter skip.
