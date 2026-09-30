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
  - Runde 4 (ferdig): større hoder og flatt lys på ansiktene, og pelsen på Navigatørens skulder synes over kappa.
  - Runde 5 (ferdig):
    - **Speider:** skjørt og panel til kneet, og buen skrått bak kroppen.
    - **Spydvakt:** vimpelen henger ned fra spydet.
    - **Raider:** flettene svinger som i bildet (`SwungBraids`).
    - **Jarl:** fyldigere pelskrage og skjoldet vendt mot oss.
    - **Seer:** fillete fjær stikker ut fra skuldrene.
    - **Navigatør:** videre skjørt og høyere pannebånd.
  - Runde 6 (ferdig):
    - **Alle:** varmere, kremgul pels med mørke tupper.
    - **Jarl:** gullruner på kåpepanelet.
    - **Navigatør:** lys vevd kant langs hele kappa.
    - **Speider:** buen holdes lavt og går ned bak beina.
    - **Raider:** mørkt stål, høyere støvler og bredere stilling.
    - **Spydvakt:** vimpelen sitter høyere på spydet.
    - **Seer:** hvitt hår rammer inn ansiktet i hetta, og smykkene er større.
  - Runde 7 (ferdig):
    - **Alle:** kortere skjørt, så pinnebeina synes. Figurene står litt på skrå, pelsen holder seg lys, og hvert våpen har en egen hvilestilling. Rundbildet (`docs/turnaround.png`) viser hver figur fra fire kanter.
    - **Jarl:** avrundet kronehjelm lavt over pannen, med fire buede horn med gullkant.
    - **Raider:** bærer øksa over skulderen i spillet.
    - **Spydvakt:** høyere spyd der vimpelen går klar av hjelmen. Spyd og stav bæres oppreist.
    - **Seer:** lange snorer med runeskiver ved ansiktet og nedover stola, og en stav med gevirgreiner og en innfattet stein.
    - **Speider:** en vifte av piler over skulderen.
    - **Navigatør:** lengre og fyldigere kappe bak.
  - Til runde 8:
    - [x] Sverd og bue har egne bærestillinger: sverdet henger med spissen ned og den flate siden ut, og buen holdes lavt og på skrå. Se `docs/heroes.png`.
    - [x] Hanskene er store, med knoker, tommel og en utsvingt mansjett med lys søm, som i bildet.
  - Runde 8 (ferdig):
    - **Alle:** spyd, stav og sverd går ikke lenger gjennom bakken når de bæres i spillet. Hånden griper lenger ned på skaftet, og en test sjekker høyder fra 1,4 til 2 m.
    - **Jarl:** store kuplede gullspenner med knutemønster foran pelsen og flettene.
    - **Raider:** lysere, slitt stål på økseblad og en blank egg.
    - **Navigatør:** kortere skjørt og kappe, så beina synes. Skriftrullene stikker ut ved hofta, og bandanaknuten med haler sitter på siden av hodet. Pelskragen er lys saueskinn.
    - **Spydvakt:** større skjold med mørk jernkant og nagler.
    - **Seer:** kortere stav, så runesteinen sitter ved ansiktet.
    - **Speider:** bred grønn kappekrage over begge skuldrene, med pelsen oppå.
  - Runde 9 (ferdig):
    - **Jarl:** kronen har gullribber med nagler over kuppelen og fire tydelige horn. Det runde skjoldet er laget av rødbeisede planker.
    - **Raider:** øksebladet er en halvmåne med buet egg og krokete skjegg. Store punger henger lavt på hoftene.
    - **Navigatør:** brede, lyse vevde kanter med mørkt sikksakkmønster på kappa.
    - **Spydvakt:** brede, lyse lærkanter ned forsiden og rundt kanten av vesten. Knuteskjoldet har bredere bånd og rustbrun jernkant.
    - **Seer:** fjærkappa er åpen foran. Beltet har en runemedaljong, og ved hofta henger en bunt med hodeskalle og runeanheng.
    - **Speider:** lys pelslue med mørkt tøybånd, halsen synes over kragen, og en kort flette går over i bølgete lokker.
  - Runde 10 (ferdig):
    - **Alle:** øynene er piller (to runde prikker som henger sammen), huden er fersken med sju hudfarger å velge mellom, og figurene står rakt med brystet fram og haka opp.
    - **Jarl:** høyere, svart hjelmkuppel med tynne gullribber og stor gullkam, bredere kappe, et koksgrått frontpanel med gullkant, og store åpne gullringer på et bredt brunt belte.
    - **Raider:** den ledige armen henger rett ned langs siden.
    - **Navigatør:** kartet holdes oppe i brysthøyde, og saueskinnskragen er mindre, så kappa synes.
    - **Spydvakt:** lengre og bredere spydblad.
    - **Seer:** ansiktet er fritt, med én lang snor med store runeskiver på hver side. Flettene når ned til hofta, og kjortelen er bred nederst.
    - **Speider:** korte støvler, så beina synes.
  - Runde 11 (ferdig):
    - **Jarl:** pelskragen er bred og kort og ligger ute over skuldrene.
    - **Raider:** skjørtet og det røde forkleet henger ned til støvlene, og pelskragen er bredere og kortere.
    - **Navigatør:** kartet er mindre, så pelskragen og fletta synes.
    - **Spydvakt:** knuten på skjoldet er malt med brede, lyse bånd med mørk kant, og pelskragen er bredere.
    - **Seer:** slankere og høyere, kjortelen faller rett ned, og fargen er skifergrå.
    - **Speider:** den grønne kappa er bredere over skuldrene, og pilene har smale, spisse fjær.
  - Runde 12 (ferdig):
    - **Alle:** et lite stykke tynn hals synes mellom hodet og kragen.
    - **Jarl:** remmene over brystet ligger utenpå frakken og møtes i én gullring.
    - **Raider:** kortere flette som henger bak skulderen, og den ledige armen holdes litt ut fra kroppen.
    - **Navigatør:** runen på bandanaen sitter på stoffet (den lå gjemt), tykkere flette, og korte bandanaender som stikker ut bak knuten.
    - **Spydvakt:** det lange skjerfet krysser brystet på skrå, og skjoldbulen er større.
    - **Seer:** geviret sprer seg bredt ut til sidene med takker som står opp, og runeanhengene henger fra gevirgreinene (ingen tverrstang).
    - **Speider:** pilkoggerremmen går over kappa, pelsen over høyre skulder er større, og kragen sitter lavere.
  - Runde 13 (ferdig):
    - **Alle:** bredere og lysere kryssremmer på støvlene.
    - **Jarl:** kappa slutter ved støvlene, og sverdet har en skarpere spiss.
    - **Raider:** mørkere øksblad med en lys, slipt egg.
    - **Navigatør:** mørkt underskjørt med lys pelskant, lengre skjørt, og kappa henger bare på siden (ikke blått mellom beina).
    - **Spydvakt:** en større, lysere lærpung på hofta.
    - **Seer:** mer krokete stav med knuter, og brune fjærremser innimellom de grå.
    - **Speider:** kappa slutter over beltet, så spennen synes.
  - Runde 14 (ferdig):
    - **Raider:** smalere pelskappe som henger lenger ned over overarmene, et smalere og kortere skjørt, så de svarte pinnebeina synes mellom skjørtet og støvlene, og et forkle som slutter ved skjørtekanten.
    - **Jarl:** en tykk, lys pelsboa rullet over skuldrene rett under haka, åpen foran for spennene, og kortere fletter som slutter ved brystet.
  - Runde 15 (ferdig):
    - **Navigatør:** saueskinnet er bredere og henger lenger ned over begge skuldrene, som en kappe.
    - **Spydvakt:** pelskragen er kortere og mer brun, med mørke tupper.
  - Runde 16 (ferdig):
    - **Speider:** pelsstykket ligger på den ene skulderen over den grønne kappa, i stedet for å henge ned til livet.
    - **Seer:** sammenlignet uten store avvik denne runden.
  - Runde 17 (ferdig):
    - **Seer:** fjærkappa vider seg ut mot bakken i fillete lag, så silhuetten blir en bred trekant som i bildet, i stedet for en rett søyle.
  - Runde 18 (ferdig):
    - **Spydvakt:** pelskragen er litt kortere igjen. Den henger fortsatt ned over overarmene, trolig fra pelsen på armene. Det tas i en senere runde.
  - Runde 19 (ferdig):
    - **Navigatør:** tynnere flette, og bandanaen sitter høyere, så mer av det runde ansiktet synes under kanten, som i bildet.
  - Runde 20 (ferdig):
    - **Jarl:** de innpakkede flettene slutter ved brystet, som i bildet.
- [x] **9. Bevegelse i plaggene:** (se [docs/motion.png](docs/motion.png)) kapper, skjørt, fletter, pels og vimpler svinger og henger etter når figuren går, snur og hugger (enkel fysikk med fjærer). Kapper og fletter bøyer seg i stedet for å svinge som en stiv planke: toppen blir liggende på skuldrene (eller ved hodet), og jo lenger ned, jo mer blafrer de (`ClothBend`). Skjørt, frakker og kjortler følger beina: alt på kroppen under hofta svinger med låret på sin side, så et langt steg dytter stoffet fram i stedet for at kneet går gjennom det (`SkirtFlex`).
- [x] **10. Tegneserie-look på alt:** tusj-kanter på skipet, øyene, trærne, husene og havet, skravur med blyant eller tusj i skyggene, og papirkorn over hele bildet.
  - [x] Skravur i skyggene: shaderen `OdinsCoin/InkToon` (i `Resources/Shaders`) gir myk tegnet belysning med kalde skygger, enkle blyantstreker på skyggesiden og kryss-skravur i de mørkeste delene. Ansiktene får ingen skravur, så de holder seg rene. Den brukes på alt som ikke er blankt (havet beholder Lit). `InkStyle.cs` har samme matte, og forhåndsvisningen bruker den.
  - [x] Tusj-kanter på verdenen: shaderen `OdinsCoin/InkOutline` skyver et skall ut et fast antall skjermpiksler (tynnere på avstand), og `InkOutliner` gir alt massivt i InkToon en strek, også ting som dukker opp senere. Skroget og øyene får strek fra yttersiden, seil og bannere får ingen, og heltene har sine egne streker.
  - [x] Papirkorn: papirteksturen ligger fast på skjermen over alt i InkToon (og i forhåndsvisningen).
  - [x] Skygger som faller på ting med InkToon: shaderen tar imot solens skygger både i URP, der den bruker URPs eget shaderbibliotek, og i den innebygde pipelinen. En skygge gjør tonen mørk, så den får blyantskravur som resten. Ikke testet i Unity ennå.
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
- [x] **13. Detaljer:** runer og mønstre på stoff, nagler og ringer, kapper som blafrer i vinden, og flere ansiktsuttrykk.
  - [x] Kapper, bannere og fletter tar vinden (`ClothWind`): spranget regnes mot lufta, så vind bakfra blåser kappa fram. Et blafr med vindkast vokser med vindstyrken, er forskjellig for hver ting og holder seg innenfor fjærens grenser.
  - [x] Runer og mønstre på stoff: `CharacterKit.RuneBand` setter en rad med runer langs en kant. Den er brukt på Jarlens gullkant og mellom sikksakkene på Navigatørens kappe. Nagler og ringer fantes fra før (Jarlens remmer og belte, Spydvaktens vest og Raiderens hjelm).
  - [x] Ansiktsuttrykk (`Face`): strekøynene blunker hvert 2.–5. sekund. De blir til ^ ^ når du vinner på mynten eller selger en kiste (Gunnar smiler også), og til > < når noen blir truffet eller mynten viser slangen. Se de to siste figurene i `docs/heroes.png`.
- [x] **14. Flere skins:** nye sett (vinter, draugr, gull-jarl, havfolk) og sjeldne skins fra kister. (se [docs/skins.png](docs/skins.png))
  - Fire sett kler alle seks antrekkene. Winterborn (600 gull) og Sea-Folk (800 gull) kjøpes i methallen. Draugr (med gråblek hud og grønne runer) og Gold Jarl er sjeldne og kan ikke kjøpes.
  - Hver kiste du selger hjemme, har 10 % sjanse for et sjeldent skin du ikke har ennå. Sjansen står i methallen. Du får aldri samme skin to ganger, og når du har alle, kommer det ikke flere.
- [x] **15. Ansikt, hud og holdning:** øynene er to runde prikker over hverandre som henger sammen (en høy pille), som i bildet. Huden er fersken som i bildet, og du kan velge mellom sju hudfarger i heltemenyen. Figurene står rakt med brystet fram og haka litt opp.
- [x] **16. Levende bevegelse:** alt skal være mykt og uten hopp mellom stillinger.
  - [x] Fotsteg og svinger (`Player/Locomotion.cs`): farten bygges opp og dør ut gradvis. Hvert steg planlegger hvor neste fot lander og kan bare snu kroppen et visst antall grader (mye når du står, lite i full sprint), så en sving går i en bue over flere steg. En skarp vending bremser først, snur deretter på stedet med små steg og setter så av gårde. I lufta beholder du farten og kan bare styre litt.
  - [x] Gange og sprint (`Characters/HeroAnimator.cs`, se [docs/motion.png](docs/motion.png)): beina følger fotstegene. Armene pendler og bøyer seg ved albuen, kroppen hopper litt og lener seg fram når den setter av. Den heller inn i svingene, og hodet ser dit du skal før kroppen snur.
  - [x] Hopp: i lufta går knærne opp og armene ut, og beina strekker seg mot bakken når figuren faller. Landingen svikter og bøyer kroppen fram, dypere jo hardere fallet er, før den retter seg opp. Kapper, fletter og skjørt kjenner mindre tyngde og luft som presser dem opp i fallet, så de flagrer opp. Ved landingen slås de ned igjen. (Ingen krøking før satsen, for den ville gitt forsinkelse på hopp-knappen.)
  - [x] Våpen (`Characters/HeroAttacks.cs`, se [docs/attacks.png](docs/attacks.png)): tohånds hogg ovenfra med øks, skrått hugg med sverd, stikk med spyd der grepet sklir ned på skaftet, spenning og skudd med bue, slag med stav holdt i enden, og et knyttneveslag uten våpen. Hvert slag går gjennom noen nøkkelstillinger (klar, oppladning, slag, gjennomføring, tilbake) langs myke kurver. Det glir inn fra det figuren gjorde og ut igjen, og kroppen vrir, lener og tar et steg inn i slaget. Treffet kommer når våpenet faktisk treffer, og våpenet peker alltid dit slaget skal.
  - [x] Alle ledd går gjennom dempede fjærer, så overganger aldri hopper. En test kjører gange, sprint, sving, hopp, landing og stopp og sjekker at ingen ledd rykker brått fra ett bilde til det neste.
  - [x] NPC-er bruker samme system. Saksiske vakter går og snur i steg, snur seg mot deg før de hogger, og sverdet går gjennom samme slag, med oppladningen strukket ut som en tydelig advarsel. Bjørn, Gunnar og de danske bueskytterne puster, flytter vekten, kikker seg rundt og snur seg i steg (`HeroIdle`). Bueskytterne spenner og skyter med buen når pilene går.
  - [x] Forhåndsvisning: [docs/footsteps.png](docs/footsteps.png) viser fotsporene ovenfra i en sving når figuren går og når den sprinter, en U-sving i sprint og en snuing fra stillestående. [docs/motion.png](docs/motion.png) viser gange, sprint, hopp og landing, og [docs/attacks.png](docs/attacks.png) viser slagene.
  - [x] Knær og ekte gange: beina har kneledd, og alt fra kneet og ned (legg, støvler, buksebein) bøyer seg med det. Hofte og kne følger målte gangkurver for mennesker. For gange er de etter Winters normaldata, med 60 % av skrittet på bakken. For løping har foten 38 % bakketid og bøyer kneet til over 100° i svingfasen. Gange glir over i løping med farten. Kroppen hviler på det lengste beinet, så den bølger av seg selv og synker ned i en landing. I hoppet strekkes beina i satsen, trekkes opp på toppen og strekkes mot bakken i fallet, og knærne svikter ved landing.
  - [x] Føttene står stille på bakken: lårets sving skaleres slik at foten som står nede blir der mens kroppen går over den, både i gange og løp. En test måler at foten sklir lite. Spillets gange (4,2 m/s) er egentlig et raskt jogg, så løpesteget kommer tidlig inn.
  - [x] Sats før hoppet: et kort kne-bøy på en tiendedels sekund, med armene svingt bakover, før figuren spretter opp.
  - [x] Rolig gange: hold Ctrl for å gå sakte med ekte gangsteg. Vanlig fart er et raskt jogg, og Shift gir sprint. Løpesteget er kortet litt ned, så bakbeinet ikke strekker seg lenger bak enn en ekte løper.
  - [x] Ankler: foten under ankelen bøyer seg for seg selv. Den ruller fra hæl til tå etter målte ankelkurver, strekker tærne i avsparket og i lufta, og bøyer dem opp for å ta imot landingen.
  - [x] Flere slag per våpen (kombinasjoner): trykker du angrep igjen mens et slag pågår, går det rett over i neste. Øks: hogg ovenfra, skrått hugg og bredt feie-slag. Sverd: hugg, bakhåndshugg og utfall. Spyd: stikk, overhåndsstikk og feiing med skaftet. Stav: slag ovenfra, feiing og støt. Bue: full spenning og kjapt skudd. Uten våpen: jab, kryss og uppercut. Det siste slaget i rekken slår hardest.
  - [x] Buestrengen trekkes: når buen spennes, tar hånda tak i strengen, og midten av den følger hånda tilbake til kinnet, så strengen står i en V fra hver ende av buen. Ved skuddet slipper den og står rett igjen (`BowDraw`, `BowString`). Mens buen er spent, ligger en pil på strengen og peker gjennom grepet, og den er borte når den er skutt. Angrepsarket viser nå det mest opptrukne øyeblikket før hvert slag.
