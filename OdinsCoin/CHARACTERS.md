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
- [ ] **5. Navigatøren:** blått hodetørkle med rune, lys flette, blå kappe med mønsterkant, kart i hånda, rullebeholder i beltet.
- [ ] **6. Spydvakten:** hjelm, pelskrage, rødt skjerf, langt spyd med rød vimpel, rundt rødt skjold med mønster.
- [ ] **7. Den gamle seeren:** gevir-hodepynt med runesteiner, lange hvite fletter, lang fillete kjortel, stav med lysende runestein.
- [ ] **8. Speideren:** pelslue, grønt skjerf og grønn tunika, bue og pilkogger.
- [ ] **9. Bevegelse i plaggene:** kapper, skjørt, fletter, pels og vimpler svinger og henger etter når figuren går, snur og hugger (enkel fysikk med fjærer).
- [ ] **10. Tegneserie-look på alt:** tusj-kanter på skipet, øyene, trærne, husene og havet, skravur med blyant eller tusj i skyggene, og papirkorn over hele bildet.
- [ ] **10b. Tegnede teksturer:** stoff, pels, lær, tre og mønsterkanter lages i kode med penselstrøk og streker, slik at alt ser tegnet ut som på bildet.
- [ ] **11. I spillet:** velg figur i menyen, bruk figurene som spiller og NPC-er, og la animasjonene passe pinne-lemmene (pust, gange, sving).
- [ ] **12. Skins:** fargesett og varianter for hver figur, kjøpt for gull i methallen, med en figur som snurrer rundt i butikken.
- [ ] **13. Detaljer:** runer og mønstre på stoff, nagler og ringer, kapper som blafrer i vinden, og flere ansiktsuttrykk.
- [ ] **14. Flere skins:** nye sett (vinter, draugr, gull-jarl, havfolk) og sjeldne skins fra kister.
