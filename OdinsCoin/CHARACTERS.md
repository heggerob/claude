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
- [ ] **8. Speideren:** pelslue, grønt skjerf og grønn tunika, bue og pilkogger.
- [ ] **8b. Finpuss, runde for runde:** Punktene over er bare første versjoner. Når alle seks finnes, går jeg tilbake til hver figur og sammenligner med konseptbildet igjen og igjen, til de er så like som mulig. Kjente forskjeller så langt:
  - **Raider:** Pelsen er for jevn og lys. Den venstre armen med øksa synes for lite. Ermene og de mørke underlagene i skjørtet trenger mer form, og støvlene bør være slankere med spissere tær.
  - **Jarl:** Kronen trenger flere detaljer (runemønster og gullkanter på hornene). Pelskragen må være mer lurvete og mindre hvit. Kappen trenger folder og tydeligere filler. Remmer og ringer mangler på beltet. Sverdet og skjoldet må ha riktig størrelse og mønster.
  - **Navigatør:** Skjørtet er for kort og for smalt, og mangler det blå underskjørtet med pelskant. Kappen foran er for flat og trenger folder. Beina er for lange. Kartet trenger revne kanter og bedre tegning, og den ene hånda skal holde kanten av kartet.
  - **Spydvakt:** Skjoldet bør være litt ovalt og vippe mer mot oss. Knutemønsteret skal være en ekte flettet knute, og vimpelen større. Lærjakken trenger mer form, med splitt foran og mørke bukser, og kappen må være mer fillete.
  - **Raider (etter at LookRotation ble rettet):** Øksehodet må snus så bladet vender mot oss igjen.
  - **Seer:** Geviret må bli tykkere og mer forgreinet, og skal hvile på en tverrstang med mange runeskiver. Flettene trenger perler. Kappen trenger flere lag med fjær, lyse flekker og mønster. Staven må være mer knudrete, og runesteinen skal holdes av røtter. Hun mangler også flere kjeder og beinsmykker.
  - **Alle:** Stoffet trenger folder og tykkelse, pelsen må få strå og ikke klumper, og tegnede teksturer og skravur (punkt 10) er det som mangler mest mot det malte bildet.
- [ ] **9. Bevegelse i plaggene:** kapper, skjørt, fletter, pels og vimpler svinger og henger etter når figuren går, snur og hugger (enkel fysikk med fjærer).
- [ ] **10. Tegneserie-look på alt:** tusj-kanter på skipet, øyene, trærne, husene og havet, skravur med blyant eller tusj i skyggene, og papirkorn over hele bildet.
- [ ] **10b. Tegnede teksturer:** stoff, pels, lær, tre og mønsterkanter lages i kode med penselstrøk og streker, slik at alt ser tegnet ut som på bildet.
- [ ] **11. I spillet:** velg figur i menyen, bruk figurene som spiller og NPC-er, og la animasjonene passe pinne-lemmene (pust, gange, sving).
- [ ] **12. Skins:** fargesett og varianter for hver figur, kjøpt for gull i methallen, med en figur som snurrer rundt i butikken.
- [ ] **13. Detaljer:** runer og mønstre på stoff, nagler og ringer, kapper som blafrer i vinden, og flere ansiktsuttrykk.
- [ ] **14. Flere skins:** nye sett (vinter, draugr, gull-jarl, havfolk) og sjeldne skins fra kister.
