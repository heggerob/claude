# Airsoft Arena – design

Spillet er på engelsk. Aldersgrensen skal være rundt 12+: ingen blod, bare BB-er, "HIT!" og en oransje dødfille.
Rekkefølgen er **2D ovenfra først**, og en 3D FPS-versjon senere som bruker de samme reglene.
Bilspillet er et eget prosjekt og blandes ikke inn her.

## Kjerneideen: dommeren

Dommeren er det som skiller spillet fra andre skytespill.

- Alle kamper har en dommer. Spillerne **leier** en dommer før kampen, og hver spiller betaler honoraret med penger fra spillet.
- En dommer har en **stjerne-rating** fra tidligere kamper. Høyere rating betyr høyere pris og (vanligvis) bedre dommer.
- Du kan også **jobbe som dommer** selv. Da får du betalt av spillerne og blir ratet etter kampen.
- Spillerne har også ratinger: **skill** (hvor gode de er) og **honor** (hvor ærlige de er).
- Senere: matchmaking kan bruke honor, og ærlige spillere havner sammen.

### Treff-regler (bestemt i prototypen)

| Situasjon | Hva skjer |
|---|---|
| En BB treffer deg | Du har 2,5 s på å rope HIT (H). Imens kan du fortsatt bevege deg og skyte. |
| Du roper HIT | Filla går opp, du går tilbake til spawn, 3 s ventetid, så er du inne igjen. Skytteren får 1 poeng. |
| Du roper ikke | Du spiller videre som "zombie". Dommeren kan oppdage det: da får skytterens lag 1 poeng + 1 straffepoeng, og du får bot og mister honor. |
| Du roper sent | Du er ute og skytteren får poenget, men uten straff. |
| Dommeren dømmer feil | Den uskyldige spilleren må ut uansett (dommerens ord er lov), men dommeren får dårligere rating. |
| Du skyter noen som har filla oppe | Overshoot, advarsel og lavere honor hvis dommeren ser det. |
| Du skyter dommeren | Bot. |
| Friendly fire | Teller som treff, men ingen poeng. |

## Beslutninger fra den gamle planen

Den første planen var laget for en 3D FPS. Dette er hva som er tatt med og hva som venter:

| Punkt | Status |
|---|---|
| Våpenklasser 00–07 (Melee, AR, SMG, Sniper/DMR, Pistol, Shotgun, Burst, LMG) | ✅ Med |
| AEG / gass / CO2 / fjær | ✅ Med (påvirker foreløpig bare stats) |
| Single / semi / burst / auto | ✅ Med |
| Stats: FPS, RPM, magasin, omladingstid, lengde | ✅ Med, pluss BB-vekt, hop-up og spredning |
| Korte koder + fiktive navn | ✅ `01-VK4 Viking K4` osv. |
| BB-kapasitet i stedet for skudd | ✅ Med |
| Treff/død-system | ✅ Bestemt, se over |
| Antall magasiner | ✅ Per våpen (`magsCarried`) |
| Økonomi | ✅ Første versjon: dommerhonorar, seier/tap, bøter |
| Bevegelse | ✅ Gå, sprinte (kan ikke skyte), huke |
| Spillmodi | ✅ Team Deathmatch, Capture the Flag, King of the Hill |
| Roller (medic, sniper, assault) | 🔜 |
| Progresjon og opplåsing | ✅ XP, 10 ranker (Recruit–General), våpen låses opp med rank, dagsbonus med streak |
| Tilbehør (attachments) | 🔜 |
| Kosmetikk og butikk | ✅ Kamo, uniform, hodeplagg, BB-farge, kasser med åpne odds |
| Flere kart, innendørs | ✅ Pallet Yard, Warehouse (innendørs), Forest |
| Treningsmodus | ✅ Treningsbane med stålblinker 10–60 m, måling av BB-fall, prøv alle våpen |
| Proximity voice og radio (kan ikke skyte mens du bruker radio) | 🔜 Krever flerspiller |
| Flerspiller / matchmaking | 🔜 FishNet eller Mirror + Steam |
| Battle Royale med ekte våpen | ❄️ Eget spill, ikke i Airsoft Arena |
| Biler | ❌ Holdes utenfor |

## Neste steg (forslag)

1. Test prototypen og juster følelsen: fart, spredning, BB-fall, hvor lett dommeren oppdager juks.
2. Capture the Flag og King of the Hill.
3. Ekte pixel-art (spillere, våpen, kart) i stedet for generert grafikk.
4. Flerspiller med Steam, der én spiller kan være dommer for andre ekte spillere.
5. Steam-side og wishlists tidlig.
