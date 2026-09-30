# Odin's Coin – figurene

Mål: figurer i ekte 3D som er **nesten identiske** med konseptbildet [docs/reference/characters-concept.png](docs/reference/characters-concept.png).
Kjennetegnet er **ansiktet med strek-øynene og de tynne kroppene**. Plaggene skal være realistiske og bevege seg med figuren.
Hele spillet, både figurene og verdenen, skal se **tegnet** ut: tusj-kanter, blyant- og tusj-streker og tegnede teksturer, som en tegneserie.

Figurene bygges i lag, så ingenting må gjøres på nytt senere:
1. **Kropp** (`Body.cs`): Spilleren velger høyde, bredde og kjønn. Ansiktet med strek-øynene og pinne-lemmene er alltid med.
2. **Hår** (`HairStyle`): velges for seg.
3. **Antrekk** (`Outfits.cs`, `Garments.cs`): klærne og evnene til Jarlen, Raideren og de andre. Alle plaggene bygges ut fra målene til kroppen, så de passer på alle størrelser.
4. **Farger** (`Palette.cs`): Hvert antrekk har standardfarger, men alle kan endres. Plaggene bruker bare fargeplassene.
5. **Våpen** (`Weapons`): et tillegg som bygges for seg og holdes i hånda. Det er ikke en del av figuren.

Så langt: [docs/heroes.png](docs/heroes.png). Sammenligning med konseptbildet: [docs/compare/](docs/compare/)
- et stort rundt hode med to svarte strek-øyne og ingen munn;
- tynne svarte pinne-armer og -bein, med hansker og store støvler;
- tjukke klær med pels, fillete kapper og kanter, fletter med perler, lær og messing;
- en svart blekk-kant rundt alt.

Alt bygges i kode (ingen modellfiler trengs). `tools/preview/render.sh` tegner et bilde av alle figurene på rad, som på konseptbildet, og eksporterer en `.obj`-fil for hver figur.

## Plan (ett punkt om gangen)

- [x] **1. Stilen:** rundt hode med strek-øyne, pinne-armer og -bein, hansker, støvler med pels og remmer, blekk-kant, og et bilde med figurene på rad på lys bakgrunn.
- [x] **2. Stoff og pels:** fillete kanter, kapper som henger, pelskrager, fletter med perler, belter med ringer og vesker, og gullkanter.
- [x] **3. Jarlen:** svart og gull hornkrone, rød kappe med pels, stort sverd og skjold, røde fletter.
- [x] **4. Raideren:** hjelm med nesevern, røde fletter, pels på skuldrene, tohånds øks over skulderen, rødt banner foran.
- [x] **5. Navigatøren:** blått hodetørkle med rune, lys flette, blå kappe med mønsterkant, kart i hånda, rullebeholder i beltet.
- [x] **6. Spydvakten:** hjelm, pelskrage, rødt skjerf, langt spyd med rød vimpel, rundt rødt skjold med mønster.
- [x] **7. Den gamle seeren:** gevir-hodepynt med runesteiner, lange hvite fletter, lang fillete kjortel, stav med lysende runestein.
- [x] **8. Speideren:** pelslue, grønt skjerf og grønn tunika, bue og pilkogger.
- [ ] **8b. Finpuss, runde for runde:** Punktene over er bare første versjoner. Jeg går tilbake til hver figur og sammenligner med konseptbildet igjen og igjen, til de er så like som mulig.
  - Runde 1 (ferdig): pelsen er bygd om til lurvete skulderkapper, kappene er fillete med folder, Raiderens økse og støvler er rettet, og Navigatørens skjørt, Spydvaktens triquetra-skjold, Seerens gevir og Speiderens hetteskjerf er på plass.
  - Runde 2 (ferdig):
    - **Raider:** skjeggøks på størrelse med ansiktet og pels som åpner seg i en V foran. Pelsen har fått `frontWidth`.
    - **Jarl:** pelskragen ligger som en boa over skuldrene, kronen har seks takker, og kappekanten er fillete strimler i ulik lengde.
    - **Navigatør:** skjørtet går til kneet, og det blå underskjørtet synes. Støvlene er slanke og spisse, rullene på hofta er store, og kappen svinger ut.
    - **Spydvakt:** bredt spydblad og vimpel med merke på begge sider. Spydet står på bakken, skjoldet er større, og skjerfet ligger på skrå.
    - **Seer:** hetta har spiss og hengende fjær, skuldrene er smalere, og runebrikkene er store og henger i lange snorer.
    - **Speider:** bare armer med armbeskyttere, grønn skulderkappe med spiss foran, pels på én skulder og koggeret på riktig side.
  - Runde 3 (ferdig):
    - **Alle:** overkroppene er smalere enn hodet, som i bildet.
    - **Jarl:** remmer i kryss med ring, en krone som sitter som en hjelm, koksgrå kåpe med bred gullkant, og et smalere sverd med blodrenne og runer.
    - **Raider:** større lommer på hoftene.
    - **Navigatør:** et større, fillete kart med kystlinje, øy og seilrute.
    - **Spydvakt:** splitt foran i lærvesten, mørke bukser og knehøye støvler.
    - **Seer:** blekere filler i fjærkappa.
    - **Speider:** lua sitter på skrå med mørkt stoff synlig, flettet svinger over høyre skulder, og løse lokker henger på samme side.
  - Til runde 4:
    - Ansiktene er litt mindre enn i bildet. Hodet kan bli større, eller hodeplaggene kan sitte høyere.
    - Hendene som holder våpen bør synes bedre.
    - Pelsen på Navigatørens skulder er for lite synlig.
    - Buen til Speideren skal henge bak, ikke holdes foran.
- [x] **9. Bevegelse i plaggene:** (se [docs/motion.png](docs/motion.png)) kapper, skjørt, fletter, pels og vimpler svinger og henger etter når figuren går, snur og hugger (enkel fysikk med fjærer).
- [ ] **10. Tegneserie-look på alt:** tusj-kanter på skipet, øyene, trærne, husene og havet, skravur med blyant eller tusj i skyggene, og papirkorn over hele bildet.
  - [x] Skravur i skyggene: shaderen `OdinsCoin/InkToon` (i `Resources/Shaders`) gir myk tegnet belysning med kalde skygger, enkle blyantstreker på skyggesiden og kryss-skravur i de mørkeste delene. Ansiktene får ingen skravur, så de holder seg rene. Den brukes på alt som ikke er blankt (havet beholder Lit). `InkStyle.cs` har samme matte, og forhåndsvisningen bruker den.
  - [x] Tusj-kanter på verdenen: shaderen `OdinsCoin/InkOutline` skyver et skall ut et fast antall skjermpiksler (tynnere på avstand), og `InkOutliner` gir alt massivt i InkToon en strek, også ting som dukker opp senere. Skroget og øyene får strek fra yttersiden, seil og bannere får ingen, og heltene har sine egne streker.
  - [x] Papirkorn: papirteksturen ligger fast på skjermen over alt i InkToon (og i forhåndsvisningen).
  - [ ] Skygger som faller på ting med InkToon (i dag kaster de skygge, men viser ikke skygger fra andre).
- [x] **10b. Tegnede teksturer:** (`DrawnTextures.cs`, se `docs/textures.png`; hver del får type fra palettfargen) stoff, pels, lær, tre og mønsterkanter lages i kode med penselstrøk og streker, slik at alt ser tegnet ut som på bildet.
- [x] **11. I spillet:** velg figur i menyen, bruk figurene som spiller og NPC-er, og la animasjonene passe pinne-lemmene (pust, gange, sving).
  - [x] Velg helt: tittelmenyen har «Your hero». Der velger du antrekk (med evnene vist), kjønn, høyde, bygning, våpen og hva du har i den andre handa. Spilleren bygges om med én gang, og valget lagres for seg (`HeroChoice`), så det overlever en ny seilas.
  - [x] Spilleren er en storybook-helt (`HeroBuilder`). Blokkering løfter skjoldarmen med albue i stedet for å flytte skjoldet.
  - [x] Saksere, danske raidere, Bjørn og Gunnar er helter (`NpcHeroes`): antrekk med egne farger og kropp fra et frø, så ingen er like. De er med nederst i `docs/heroes.png`.
  - [x] Animasjoner for pinnelemmer (`HeroPose`): albuene bøyer seg når armene svinger i gange, knyttes stramt i opptrekket til et hugg og strekkes når slaget treffer. Albuene bøyes også når figuren bærer kister og øser vann, og alle puster sakte når de står.
- [x] **12. Skins:** fargesett og varianter for hver figur, kjøpt for gull i methallen, med en figur som snurrer rundt i butikken. (se [docs/skins.png](docs/skins.png))
  - Hvert antrekk har sine klassiske farger gratis og to skins å kjøpe (250–700 gull). Til sammen er det 12 skins, og de endrer bare fargene, ikke evnene.
  - Methallen har fått fanen «Colours»: du kan prøve et skin på den snurrende figuren ved døra, kjøpe det og ta det på. Heltemenyen lar deg bytte mellom skinsene du eier.
  - Det du har kjøpt, lagres for seg (`SkinLocker`) og blir med til nye seilaser.
- [ ] **13. Detaljer:** runer og mønstre på stoff, nagler og ringer, kapper som blafrer i vinden, og flere ansiktsuttrykk.
  - [x] Kapper, bannere og fletter tar vinden (`ClothWind`): spranget regnes mot lufta, så vind bakfra blåser kappa fram. Et blafr med vindkast vokser med vindstyrken, er forskjellig for hver ting og holder seg innenfor fjærens grenser.
  - [ ] Runer og mønstre på stoff, flere nagler og ringer.
  - [x] Ansiktsuttrykk (`Face`): strekøynene blunker hvert 2.–5. sekund. De blir til ^ ^ når du vinner på mynten eller selger en kiste (Gunnar smiler også), og til > < når noen blir truffet eller mynten viser slangen. Se de to siste figurene i `docs/heroes.png`.
- [ ] **14. Flere skins:** nye sett (vinter, draugr, gull-jarl, havfolk) og sjeldne skins fra kister.
