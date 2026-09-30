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
  alltid. Velsignelser og forbannelser fjernes fra mynten.
- [x] **13. Skatten er i fare om bord (`Sea/Wreck.cs`):** synker skipet, synker kistene og kan dykkes opp igjen der det sank. Kistene ligger i vannkanten rundt en brukket mast som flyter over stedet. Vraket vises på HUD-en (avstand og retning) og på sjøkartet til siste kiste er hentet.
- [x] **14. Enkel styring (`HelmOrders` i `Ship/Longship.cs`):** W og S for seil og A og D for ror ved roret. Anker og fortøyning skjer av seg selv ved
  brygga. Færre taster og menyer.
- [ ] **15. Verdensskala** (du bestemmer): krympet verden, eller full størrelse som nå.
- [x] **16. Landemerker (`World/Landmarks.cs`, se [docs/buildings.png](docs/buildings.png)):** hvert sted får noe høyt eller lyst som synes langt unna. Byer og festninger har et 22 m vardetårn med bål på toppen, klostre et klokketårn, jarlens hall en kjempestor ask, og landingsplasser en runestein som lyser blått. Det står på det høyeste tørre stedet i nærheten, klar av hus og åkrer.
- [x] **17. Utsiktspunkter (`UI/ChartReveal.cs`):** sjøkartet starter som blankt pergament, bortsett fra hjemmefarvannet. Mens du seiler, tegnes kysten rundt skipet inn (6 km). Står du ved et landemerke og trykker E, klatrer du opp i vardetårnet, klokketårnet eller asken, eller leser runesteinens kart, og området i 25 km rundt tegnes inn. Det som er kartlagt, lagres.
- [x] **18. Runeringer (`World/RuneShrines.cs`):** rundt hvert landemerke står en ring av fem små steiner hugget med 1 til 5 hakk. Rør dem i rekkefølge etter hakkene, så våkner ringen og gir sin gave. Rører du feil stein, blir alle mørke igjen. Gavene er Vitalitetens rune (+15 helse), Utholdenhetens rune (mer pust til klatring og svømming) og Hellets rune (+2 % bedre odds ved alteret, maks 60 %). Lagres.
- [ ] **19. Klatring og utholdenhet.**
- [ ] **20. Hemmeligheter:** grotter, skattekart og en ravn som leder deg.
- [ ] **21. Tips i stedet for oppdragsliste:** Bjørn og byfolket forteller rykter om hvor skatten er.
