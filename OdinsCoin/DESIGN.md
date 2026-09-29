# Odin's Coin – design

Et 3D vikingspill i Unity med Sea of Thieves-stemning. Du seiler et langskip på åpent hav, raider øyer og klostre,
og kjemper mot saksere, andre skip og sjømonstre. Spillet er på engelsk og i en stilisert low-poly-stil.

## Kjernen: Odins mynt

Om bord på langskipet står et lite alter med **Odins mynt**. Myntkastet er gamblingen i spillet, og det gir flaks eller ulykke.

- **Kaste mynten:** Før en kamp, en storm eller et raid kan du kaste mynten. Kron (Odins øye) gir en **velsignelse**. Mynt (ormen) gir en **forbannelse**.
- **Innsats:** Du kan satse gull eller bytte. Jo mer du satser, jo sterkere blir velsignelsen, og jo verre blir forbannelsen.
- **Velsignelser (eksempler):**
  - Tors vrede: økt skade i nærkamp.
  - Njords bris: medvind og mer fart.
  - Frøyas gave: mer gull fra kister.
  - Heimdals øye: fiender vises gjennom tåka.
- **Forbannelser (eksempler):**
  - Lokes lure: våpenet sklir innimellom.
  - Rans garn: skipet lekker.
  - Hels kulde: mindre helse til neste daggry.
- **Runer:** Du graverer inntil tre runer inn i myntkanten ved alteret, og betaler med gull. Ansuz gir bedre odds, Algiz korter ned forbannelser, Fehu øker utbetalingen, Thurisaz forlenger velsignelser og Raidho fyller Odins gunst raskere. Hagalaz gir dårligere odds, men større gevinst. Skjermen viser alltid oddsen og hvor mye du i snitt får tilbake per gull. Mellom hvert kast må mynten hvile i 10 sekunder.
- **Lykke-meter:** Odins gunst fylles når du vinner kast, dreper saksere og selger kister. Når den er full, kan du sende en ravn fra alteret. Hugin sirkler over de tre nærmeste skattene. Munin setter seg på alteret, og da blir neste kast Odins øye.
- **Ærlighet:** Oddsen står alltid tydelig på skjermen. Ingen ekte penger, bare gull fra spillet.

## Spill-loopen

1. Seil ut fra hjemmefjorden.
2. Finn en øy, et kloster eller et handelsskip. Kast mynten før du går i land.
3. Raid: slåss mot vaktene, finn kister og bær byttet om bord.
4. Kom deg hjem: stormer, fiendeskip og Jormungand kan stoppe deg.
5. I methallen: selg byttet, oppgrader skipet, kjøp runer, og spill terninger mot andre vikinger.

## Retning

- Tredjepersonskamera. Du går fritt om bord mens skipet seiler, som i Sea of Thieves.
- Skipet styres med seil (heis og fir), ror og årer, og vinden bestemmer farten.
- Nærkamp med øks, sverd, spyd og skjold.
- Stilisert grafikk laget i kode (low-poly meshes), så prototypen trenger ingen modeller.
- Singleplayer først, co-op-mannskap senere.

## Adskilt fra de andre spillene

Dette er ikke bilspillet eller Airsoft Arena. Det har egen kode og egen mappe, og senere eget repo.
