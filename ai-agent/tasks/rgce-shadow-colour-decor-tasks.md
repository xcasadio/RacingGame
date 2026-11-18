# Plan agent IA — Couleur d'ombre, pénombre et matériaux du décor

Suite de `rgce-road-tunnel-shadow-tasks.md` et des points O6 et O7 de `rgce-render-gaps-tasks.md`. Demande de l'auteur du 2026-11-18 : « Corriges et continue en mode AUTO », puis choix des quatre sujets à la question de cadrage (reflet de la route ; ambiant des tunnels et du terrain ; matériaux du décor ; pénombre de l'ombre, qui autorise l'évolution du moteur correspondante). Mode autonome : chaque tranche est relue avant exécution et l'ensemble est vérifié après ; aucun push ; `main` du moteur et `remaster` du jeu ne sont avancés qu'à la demande de l'auteur.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Rapprocher encore le rendu du port de celui du jeu original, mesuré par le banc de captures, sur quatre écarts :

1. la pénombre en paliers au bord des ombres (réglage `FilterTaps` du moteur, plan `CasaEngine/ai-agent/tasks/shadow-filter-taps-tasks.md`) ;
2. les zones à l'ombre trop sombres : terrain, pavés, route des tunnels (couleur d'ombre du monde de course) ;
3. le feuillage des palmiers trop clair ;
4. les vitres : bandes de fenêtres des immeubles éteintes, vitres de la voiture bleues au lieu de grises.

Le reflet de la route est traité et clos par mesure (voir « État vérifié »).

## État vérifié (2026-11-18, jeu `5a243b3` = `remaster`, moteur `8ddcb31b0` = `main`)

Faits relevés en lecture seule par cinq enquêteurs, relus un à un (`artifacts/pbr-handover/s23/facts.txt`), et essais mesurés sur une copie du dossier de build hors du dépôt (`artifacts/pbr-handover/s23/`). Référence du port : `artifacts/pbr-handover/s22/s2/ref/`.

- **Reflet de la route, clos.** Rugosités 0,25 ; 0,20 ; 0,15 ; 0,10 avec l'occlusion, mesurées avec le script figé de l'ADR-0034 (`s22/scripts/measure_s2.py`, sorties `s23/road/`) : `E` 15,846 ; 16,090 ; 16,399 ; 16,700 pour 15,732 à 0,30, et la boîte du reflet baisse (185,7 ; 173,2 ; 151,4 ; 123,9 pour 189,3). Aucun candidat éligible : 0,30 reste. Rien à changer.
- **Couleur d'ombre.** Le moteur décode `ShadowColor` en lumière linéaire (`ForwardLightBinder.cs:237`, `PrepareColor`, `ColorSpace.Decode` = puissance 2,2 : 0,25 devient environ 0,047) et en multiplie le pixel, émissive exceptée, avant la courbe de tons (`Lighting.fxh:278-288`, `LitPbr.fx:564-571`). L'original multiplie l'image finale par (0,25 ; 0,26 ; 0,27) (`ShadowMap.fx:240`, `ShadowMapBlur.cs`). Sous la courbe ACES du monde de course, le rapport ombre sur lumière du terrain est de 0,152 et 0,139 dans le port pour 0,284 et 0,267 dans l'original. Essai (`s23/tint/`, build temporaire lisant `RGCE_BENCH_SHADOW_COLOR`, retiré) : à 0,38 les rapports sont 0,284 et 0,246, l'écart des trois tunnels à l'original passe de 15,343 à 13,023, la route des tunnels de 7,0 / 11,6 / 15,4 à 11,5 / 20,2 / 27,9 (original 11,7 / 21,8 / 31,1), `E` de 15,732 à 15,314, et la médiane des parois reste dans la fenêtre de l'auteur (30,983).
- **Feuillage.** Quatre matériaux de feuilles, désignés par leur texture de base : `PalmLeave.tga` pour `Models/AlphaPalm/orig 09 - Defaulta.material`, `Models/AlphaPalm2/orig 09 - Defaulta.material` et `Models/AlphaPalm3/orig 09 - Defaulta.material` (famille `Vegetation`, rugosité 0,95) ; `PalmLeave2.tga` pour `Models/AlphaPalmSmall/orig 09 - Default.material` (famille `Default`, rugosité 0,8, la texture n'étant pas dans `VegetationTextures`). Tous sont en base 255, `AlphaTest`, seuil alpha 0,35 (`presentation.AlphaCutoff`, `RaceMaterialRecipes.cs:107` ; l'original teste à 0,5). Les troncs (`Palm.tga`, `Palm2_stamm.tga`) et `Models/Hotel01/plants.material` (`plants.tga`) ne sont pas des feuilles de palmier. Sur quatre boîtes, les pixels à dominante verte valent en moyenne (57 ; 79 ; 30) à (56 ; 81 ; 25) dans l'original et (96 ; 123 ; 72) à (64 ; 91 ; 45) dans le port, et ils sont 1,2 à 3,2 fois plus nombreux. Essai (`s23/trial/t0.38-palm170`, base 170 sur les trois matériaux `Defaulta` seulement, `AlphaPalmSmall` non modifié) : l'écart moyen de couleur des feuilles passe de 26,99 à 14,19 et `E` de 15,314 à 15,231. La cause de l'écart n'est pas établie ; la base est un calage mesuré.
- **Vitres des immeubles.** Trois matériaux : `Models/Hotel01/39.material` (base 255), `Models/Hotel02/fenster.material` (base 94 ; 162 ; 231), `Models/Casino01/39.material` (base 64), tous métal 1, rugosité 0,05, famille `BuildingGlass`. L'original les dessine avec `reflet du ciel × fresnel × 1,5 + ambiant + diffus + spéculaire` (`NormalMapping.fx:642-654`), le reflet n'étant pas teinté par le diffus et son vecteur ayant ses axes Y et Z échangés ; une façade y montre des bandes blanches (`Beginner-sector-20`) et une autre un mur noir (sonde `building-away-1`). Dans le port ces vitres sont à l'ombre de leur propre façade, donc teintées par la couleur d'ombre. Essai (`s23/trial/t0.38-win-palm`, vitres sans ombre reçue) : les bandes s'allument, en bleu (boîte 41,0 pour 52,1 dans l'original, 30,5 avant).
- **Vitres de la voiture.** `Models/Car/Standard 19.material` : métal 1, rugosité 0,05, base 224 (ADR-0033). Boîte de la lunette arrière de `Beginner-start` (x 608-626, y 388-391) : (115,1 ; 176,3 ; 196,8) pour (136,9 ; 137,6 ; 137,9) dans l'original. L'original reflète le même fichier de ciel avec un vecteur aux axes échangés et mélange le verre à 0,66 ; le port est un miroir opaque du ciel.
- **Pénombre.** Filtre d'ombre du moteur : 12 lectures sur un disque fixe ; essai à 32 et 64 lectures (`s23/taps/`, `s23/look/taps-expert-wall.png`) : à 64 la pénombre de la paroi d'`Expert-tunnel` est un dégradé lisse ; hors tunnels, moins de 0,2 % des pixels changent.
- Scripts de mesure figés le 2026-11-18, avant toute mesure de ce plan, sous `artifacts/pbr-handover/s23/scripts/` (début de l'empreinte SHA-256) : `measure_tint.py` `537d522d7d767c01`, `measure_leaves.py` `35ebecfb2d96756d`, `measure_windows.py` `04c7620d9f017b9a`, `measure_glass.py` `edba6cc454607ff8`. Chacun porte dans son en-tête la définition exacte de ses grandeurs et de sa règle, et fait foi. `speckle.py` (`s22/scripts/`, `6dbe28b573083f62`) reste figé.
- Dernier ADR du jeu : 0034.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Cible, mesure et méthode : celles des plans précédents. Original mesuré contre `s1c/ref` et `s3/ref-tunnel`, variante `nopost` ; règle écrite avant la mesure, dans un script figé, appliquée mécaniquement ; réglages de code mesurés avec un build temporaire lisant une variable d'environnement, jamais committé, code remis à l'identique (`git status` propre contrôlé) ; candidats de matériaux sur une copie du dossier de build hors du dépôt ; le mode `legacy` est mesuré et noté, avec le seul critère 3 des sondes (au moins 27 sur 33). Sorties de ce plan : `artifacts/pbr-handover/s24/`. |
| D2 | Un réglage qui ne passe pas sa règle n'est pas appliqué : la tranche se clôt avec ses chiffres et l'écart reste ouvert. |
| D3 | Pas de contournement dans le jeu d'un défaut du moteur, et aucune évolution du moteur hors de celle autorisée (`FilterTaps`). |
| D4 | La couleur d'ombre garde sa teinte : ses trois canaux restent dans le rapport 25 : 26 : 27 de l'original ; seul son niveau est mesuré. Elle ne change que dans le monde de course ; le carrousel garde ses réglages (l'auteur a validé son état le 2026-11-18). |
| D5 | L'émissive du tunnel reste au réglage B validé par l'auteur le 2026-11-18 ; la règle de la couleur d'ombre garde la médiane des parois dans la fenêtre de l'auteur. |
| D6 | Les vitres de la voiture font partie du choix de l'auteur (option « Matériaux du décor » de la question de cadrage, dont le texte citait « vitres de la voiture bleues au lieu de grises »). Elles changent aussi dans le carrousel, où la voiture a le même matériau : l'auteur l'a accepté le 2026-11-18 (« Oui, carrousel compris »). La tranche G5 lui donne une planche avant / après du carrousel. |
| D7 | Chaque réglage retenu est une décision : ADR `Proposed` du jeu, numérotées dans l'ordre des tranches à partir de 0035 ; une tranche qui ne retient rien n'écrit pas d'ADR. Aucune ADR existante n'est réécrite ; une ligne d'état renvoie à la nouvelle. |
| D8 | Le pointeur du sous-module avance sur `chantier/shadow-filter-taps` du moteur. Rien n'est poussé. |

## Règles d'exécution pour l'agent

- Branche `chantier/rgce-pbr-handover` (suite du chantier). `git add` fichier par fichier ; jamais `.serena/`, `.codegraph/`, `log.txt` ; aucune commande git dans `CasaEngine/MGUI` ni `CasaEngine/NvgSharp` ; jamais de `git submodule update`, `checkout`, `switch`, `reset`. Commandes de commit commençant par `cd D:/...`, message par fichier et `git commit -F`.
- Un lancement du jeu à la fois, jamais pendant un build ; rebuild avant tout lancement après un changement de code ou une cuisson. Aucun build du jeu pendant qu'un exécutant modifie le moteur.
- Builds à 0 erreur ; cuiseur `--verify` (0 écart après cuisson) et `--check` ; `generate_rgce_content_assets.py --check` ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès, journaux sans `[Warning]` ni `[Error]`.
- Un commit par tâche, ce plan mis à jour dans le même commit ; messages, code, docs et ADR en anglais ; plan en français ; fins de ligne des fichiers existants conservées (pas de `sed -i`).
- Chiffres du doc de banc et des ADR produits par script et relus par un second script.
- Chaque tranche est relue avant exécution (`plan-verifier`) et l'ensemble est vérifié après (`verifier` frais, G6).
- Retour arrière de toute tranche : `git revert` de son commit ; les sorties sous `artifacts/pbr-handover/s24/` sont gardées.

### Validation commune aux tranches G2 à G5, après application

Notée « validation commune » dans les tranches. La « référence précédente » est la référence de la dernière tranche qui a changé le jeu (`s24/g1/ref` après G1, etc.).

- build à 0 erreur ; cuiseur `--verify` 0 écart et `--check` 0 ; `generate_rgce_content_assets.py --check` sortie 0 ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès ;
- rejeux deux fois, identiques entre eux : 15 vues et 3 vues de tunnel en `linear` et en `legacy`, 10 écrans d'interface, 30 captures d'audit ; références sous `s24/<tranche>/ref/` ;
- les 15 vues en `linear` identiques octet pour octet aux captures du candidat retenu ; pour une tranche de matériaux, au plus 1 niveau d'écart sur au plus 0,1 % des pixels par vue est admis et noté ;
- le script de la tranche relancé dans un dossier à part (`s24/<tranche>/applied/`, copie de la base et rejeux sous le nom du candidat retenu) : le candidat reste retenu ; les captures des candidats et le fichier de résultats de la mesure ne sont jamais réécrits ;
- critère 3 des sondes au moins 27 sur 33 dans les deux modes ; les quatre critères et `E` notés dans les deux modes ;
- les six écrans d'interface sans monde 3D (`ui-splash`, `ui-main-menu`, `ui-highscores`, `ui-options`, `ui-help`, `ui-track-selection`) identiques à la référence précédente ; les changements des quatre autres écrans et des captures d'audit nommés ;
- une planche original / avant / après des vues où le changement se voit, regardée, décrite dans la note.

Arrêts communs, sans commit, l'état précédent étant gardé : script de mesure modifié (empreinte différente) ; build temporaire non remis à l'identique ; écart du cuiseur hors des fichiers de la tranche ; rejeux différents entre eux ou différents du candidat retenu au-delà de la tolérance ; candidat qui n'est plus retenu une fois appliqué ; critère 3 sous 27 ; un des six écrans sans monde 3D qui change.

## Légende des statuts

⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Tâches

### ✅ G1 — Moteur à `FilterTaps`, 64 lectures dans le monde de course (ADR-0035)

- Prérequis : plan du moteur `shadow-filter-taps-tasks.md` terminé, T2 **CONFIRMED**. Si ce plan-là est bloqué, G1 passe en ⚠️ et G2 part de la référence `s22/s2/ref`.
- Changement : le pointeur du sous-module avance sur la tête de `chantier/shadow-filter-taps` ; `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` règle `Shadows.FilterTaps` à 64 par une constante nommée, commentée. Aucun contenu. Le carrousel n'est pas touché.
- État du moteur construit, contrôlé avant le build et noté : `HEAD` de `CasaEngine` = tête de `chantier/shadow-filter-taps` (empreinte notée), qui contient le commit `feat(rendering): make the tap count of the shadow filter a setting` ; `git -C CasaEngine status --short` vide hors `MGUI` et `NvgSharp` ; après le build, `Content/Shaders/LitPbr.xnb` du dossier de build diffère de celui d'avant (`5451c75c2b3fcac9…`). Le pointeur committé est cette même empreinte.
- Validation :
  - build à 0 erreur ; cuiseur `--verify` 0 écart, `--check` 0 ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès ;
  - rejeux deux fois, identiques entre eux (15 vues et tunnel dans les deux modes, 10 écrans, 30 captures d'audit) ; références `s24/g1/ref/` ;
  - contre `s22/s2/ref` : les six écrans sans monde 3D et `ui-car-selection` identiques octet pour octet ; pour chacune des 15 vues en `linear`, des 30 captures d'audit et des écrans `ui-pause`, `ui-race-finished`, `ui-race-hud`, au plus 3 % des pixels changent de plus de 8 niveaux (l'essai donnait moins de 0,2 % hors tunnels) ; les vues de tunnel n'ont pas cette borne ;
  - `E` en `linear` au plus celui de `s22/s2/ref` (15,732) + 0,05 ; critère 3 au moins 27 sur 33 dans les deux modes ;
  - preuve que le réglage agit : `Expert-tunnel` en `linear` diffère de `s22/s2/ref/tunnel-linear-a` ; `speckle.py --calibrate` réussit et la moucheture d'`Expert-tunnel` reste au plus de 0,60 % ; le recadrage de la paroi droite (x 860-1280, y 0-300) est regardé à côté de celui de la référence et de l'essai `s23/taps/t64-tunnel` : la note dit si l'éventail de bandes a laissé place à un dégradé ;
  - les trois vues bornées les plus changées regardées avec un masque : les changements sont sur des bords d'ombre ;
  - mesure de R4 du tunnel (`s20/r4/scripts/tunnelmeas.py`) notée : la médiane des parois reste dans la fenêtre [28,882 ; 31,882], sinon c'est noté pour G2, qui a ce critère dans sa règle.
- ADR-0035 `Proposed` (64 lectures dans le monde de course, avec le renvoi à l'ADR-0127 du moteur) ; section du doc de banc ; ce plan.
- Arrêts, sans commit : état du moteur non conforme ; rejeux différents entre eux ; une vue bornée au-delà de 3 % ; un changement ailleurs que sur un bord d'ombre ; critère 3 sous 27 ; un des sept écrans cités qui change ; `Expert-tunnel` identique à la référence, ou moucheture au-dessus de 0,60 %.
- Retour arrière : `git revert` du commit.
- Commit : `feat(racing-casa): soften the shadow edges of the race world with 64 filter taps`

> Validation (2026-11-18), sorties dans `artifacts/pbr-handover/s24/g1/` (`slice_check.txt`, `ref/`, `masks/`), scripts `s22/scripts/runref.sh`, `s24/scripts/slice_check.py`, `s22/scripts/speckle.py`.
> - Prérequis : plan du moteur terminé, T2 **CONFIRMED** (6469 tests, 0 échec).
> - État du moteur construit : `HEAD` de `CasaEngine` = tête de `chantier/shadow-filter-taps` = `46acdf6fd`, qui contient `1eacf5764` (`feat(rendering): make the tap count of the shadow filter a setting`) ; `git -C CasaEngine status --short` vide ; `LitPbr.xnb` du dossier de build : `c5648d76509173c8…` pour `5451c75c2b3fcac9…` avant. Le pointeur committé est `46acdf6fd`.
> - Build à 0 erreur ; cuiseur `--verify` 0 écart, `--check` 0 ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès ; aucun `[Warning]` ni `[Error]`.
> - Rejeux deux fois, identiques entre eux (15 vues et tunnel dans les deux modes, 10 écrans, 30 captures d'audit) ; références `s24/g1/ref/`.
> - Contre `s22/s2/ref` : les six écrans sans monde 3D et `ui-car-selection` identiques octet pour octet ; vues bornées : au plus 1,231 % des pixels changés de plus de 8 niveaux (`audit-Beginner-sector-20` ; puis `audit-Expert-sector-20` 1,106 %, `audit-Beginner-helper-palms` 1,080 %), `ui-race-finished` 0,592 %, `ui-race-hud` 0,444 %, `ui-pause` 0,004 % ; borne 3 %. Masque regardé (`audit-Beginner-helper-palms`) : les pixels changés suivent le bord des ombres du palmier, des panneaux et des glissières.
> - `E` : `linear` 15,734 (15,732 ; borne 15,782) ; `legacy` 20,245 (20,249). Sondes : `linear` 20/34, 0/4, 27/33, 3/3 ; `legacy` 4/34, 2/4, 27/33, 3/3 ; inchangées.
> - Preuve que le réglage agit : `Expert-tunnel` en `linear` diffère de la référence (2,171 %) ; `speckle.py` (empreinte `6dbe28b573083f62`) redonne son étalonnage, moucheture 0,304 % (seuil 0,60 %). Les trois vues de tunnel sont identiques octet pour octet à l'essai `s23/taps/t64-tunnel`. Recadrage regardé (`s24/judge/1-penombre-paroi-expert.png`, et `s23/look/taps-expert-wall.png`) : l'éventail de bandes a laissé place à un dégradé lisse, proche du flou de l'original.
> - Tunnel, mesure de R4 : médiane des parois 30,053, dans la fenêtre ; écart 15,155.
> - ADR-0035 `Proposed` ; section « Update after the shadow filter taps » du doc de banc. Le coût en temps d'image n'est pas mesuré.

### ✅ G2 — Couleur d'ombre du monde de course (ADR-0036)

- Prérequis : G1 clos (✅ ou ⚠️). Base : la référence en place.
- Mesure (D1, D4), en `linear`, avec un build temporaire lisant `RGCE_BENCH_SHADOW_COLOR` (modèle : `s23/scripts/tempcode.py`), copié hors du dépôt puis retiré du code avant toute mesure. Script : `s23/scripts/measure_tint.py`. Captures sous `s24/g2/` : `base/` et `base-tunnel/` ; pour chaque candidat `t<niveau>/` et `t<niveau>-tunnel/`, la couleur d'ombre valant (niveau ; niveau × 0,26 / 0,25 ; niveau × 0,27 / 0,25). Candidats : 0,30 à 0,46 par pas de 0,02, soit 9 candidats, la variable d'environnement recevant exactement ces chaînes : `t0.30` = `0.30,0.312,0.324` ; `t0.32` = `0.32,0.3328,0.3456` ; `t0.34` = `0.34,0.3536,0.3672` ; `t0.36` = `0.36,0.3744,0.3888` ; `t0.38` = `0.38,0.3952,0.4104` ; `t0.40` = `0.40,0.416,0.432` ; `t0.42` = `0.42,0.4368,0.4536` ; `t0.44` = `0.44,0.4576,0.4752` ; `t0.46` = `0.46,0.4784,0.4968`. Lancement : `python artifacts/pbr-handover/s23/scripts/measure_tint.py base t0.30 … t0.46`. La base capturée sur la copie doit être identique octet pour octet à la référence en place.
- Règle (dans l'en-tête du script) : éligible si la médiane des parois des tunnels est dans [28,882 ; 31,882], le critère 3 au moins à 27 et `E` au plus celui de la base ; retenu : la plus petite somme `S` des écarts des deux rapports ombre sur lumière du terrain à ceux de l'original, et, à moins de 0,01, le plus petit écart des tunnels. Si la base gagne ou si rien n'est éligible, rien n'est appliqué.
- Application : `RaceWorldFactory.cs`, constante `ShadowColor` écrite avec les trois mêmes littéraux décimaux que la chaîne du candidat retenu, en littéraux `float` et non par un calcul (par exemple `new(0.38f, 0.3952f, 0.4104f)` pour `t0.38`), pour que les images soient identiques à celles de la mesure ; la note cite la chaîne du candidat et la ligne committée ; commentaire qui dit pourquoi elle n'est plus la valeur de l'original (le moteur l'applique en lumière linéaire avant la courbe de tons) et renvoie à l'ADR-0036 ; ADR-0036 `Proposed`, avec une ligne d'état sur l'ADR ou le document qui fixait (0,25 ; 0,26 ; 0,27), s'il y en a un (`rg -n "0\\.26" docs`) ; doc de banc ; ce plan.
- Validation : validation commune (dossier `s24/g2/applied/` : copie de `base/` et `base-tunnel/`, rejeux sous `t<niveau>/` et `t<niveau>-tunnel/` ; commande `measure_tint.py --root s24/g2/applied base t<niveau>` ; réussi si la ligne `choice:` nomme ce candidat) ; en plus, la mesure de R4 du tunnel (`tunnelmeas.py`) et les boîtes de route des tunnels notées avant et après.
- Arrêts : arrêts communs.
- Commit : `fix(racing-casa): set the shadow colour of the race world against the original`

> Validation (2026-11-18), sorties dans `artifacts/pbr-handover/s24/g2/` (`measure_tint.txt`, `measure_tint.json`, `strings.txt`, `slice_check.txt`, `applied/`, `ref/`).
> - Mesure : `measure_tint.py` inchangé (empreinte `537d522d7d767c01` contrôlée avant et après). Build temporaire lisant `RGCE_BENCH_SHADOW_COLOR` copié hors du dépôt, code remis à l'identique (`git status` propre) et rebuild. Base capturée sur la copie, identique octet pour octet à `s24/g1/ref` (15 vues et tunnel). Neuf candidats capturés avec les chaînes du plan (`strings.txt`).
> - Règle : les neuf candidats et la base sont éligibles ; plus petite somme `S` : 0,021 à 0,38 et 0,025 à 0,40, à moins de 0,01 l'un de l'autre ; écart des tunnels 12,844 à 0,38 pour 13,016 à 0,40. **Retenu : 0,38**, chaîne du candidat `0.38,0.3952,0.4104`. Base : rapports du terrain 0,152 et 0,139 (original 0,284 et 0,267), `E` 15,734, médiane des parois 30,053, écart des tunnels 15,155.
> - Application : `RaceWorldFactory.cs`, `private static readonly Vector3 ShadowColor = new(0.38f, 0.3952f, 0.4104f);`, mêmes littéraux que la chaîne du candidat ; commentaire renvoyant à l'ADR-0036.
> - Build à 0 erreur ; cuiseur `--verify` 0 écart, `--check` 0 ; générateur `--check` 0 ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès ; aucun `[Warning]` ni `[Error]`.
> - Rejeux deux fois, identiques entre eux ; références `s24/g2/ref/`. Les 15 vues et les 3 vues de tunnel en `linear` sont identiques octet pour octet aux captures de `t0.38`.
> - Candidat toujours retenu : `measure_tint.py --root s24/g2/applied base t0.38` : rapports 0,284 et 0,246, `S` 0,021, `E` 15,312, médiane 30,885, écart des tunnels 12,844, `choice: t0.38` ; `s24/g2/measure_tint.json` non réécrit.
> - Sondes : `linear` 20/34, 2/4, 27/33, 3/3 (critère 2 : 0/4 avant) ; `legacy` 4/34, 0/4, 27/33, 3/3 (critère 2 : 2/4 avant ; dans ce mode la couleur n'est pas décodée, 0,38 y est plus clair que l'ombre de l'original). `E` : `linear` 15,734 -> 15,312 ; `legacy` 20,245 -> 20,009.
> - Tunnel, mesure de R4 : médiane des parois 30,053 -> 30,885 (fenêtre tenue), écart 15,155 -> 12,844 ; route des tunnels 7,0 / 11,6 / 15,4 -> 11,5 / 20,2 / 27,9 (original 11,7 / 21,8 / 31,1).
> - Interface et audit : les six écrans sans monde 3D et `ui-car-selection` identiques à `s24/g1/ref` ; `ui-pause` 0,009 %, `ui-race-hud` 1,064 %, `ui-race-finished` 1,406 % ; 20 des 30 captures d'audit changent (jusqu'à 39,8 %, `audit-Beginner-sector-20`), 10 sont identiques ; vue la plus changée : `Beginner-sector-20`, 24,6 %.
> - Documents : ADR-0036 `Proposed` ; aucune ADR ne fixait la couleur d'ombre (`rg "0\.26" docs` ne trouve que des sections historiques du doc de référence locale, laissées telles quelles) ; section « Update after the shadow colour » du doc de banc ; table écrite et relue par `s24/scripts/gen_docs_g2.py`.
> - Regardé sur `s24/judge/2-couleur-ombre-avant-apres.jpg` : les ombres du terrain et des pavés sont moins noires, plus proches de l'original ; la route et les parois des tunnels sont plus claires, proches de l'original sur `Advanced-tunnel` et `Expert-tunnel` ; la face de colline à gauche de `Beginner-start` reste nettement plus sombre que dans l'original (elle est tournée à l'opposé du soleil : c'est l'ambiant, pas l'ombre).

### ✅ G3 — Feuillage des palmiers (ADR-0037)

- Prérequis : G2 clos. Base : la référence en place.
- Mesure (D1), en `linear`, sur une copie fraîche du dossier de build. Script : `s23/scripts/measure_leaves.py`. Matériaux visés : les quatre matériaux de feuilles de « État vérifié », et eux seuls (`AlphaPalm`, `AlphaPalm2`, `AlphaPalm3` : `orig 09 - Defaulta.material` ; `AlphaPalmSmall` : `orig 09 - Default.material`) ; chaque candidat modifie les quatre ; les troncs et `Hotel01/plants.material` ne sont pas touchés. Captures sous `s24/g3/` : `base/` et `b<gris>-c<seuil>/`. Candidats : gris de la couleur de base 255, 220, 190, 170, 150, 130, croisé avec le seuil alpha 0,35 et 0,5, soit 11 candidats hors de la base (255 ; 0,35). Lancement : `python artifacts/pbr-handover/s23/scripts/measure_leaves.py base b255-c0.5 b220-c0.35 …`.
- Règle (dans l'en-tête du script) : éligible si chaque boîte garde au moins 100 pixels de feuille, le critère 3 au moins à 27 et `E` au plus celui de la base ; retenu : le plus petit écart moyen `D` de couleur des feuilles à l'original sur les quatre boîtes, et, à moins de 1,0, le seuil 0,5 (celui de l'original) avant 0,35, puis le gris le plus haut.
- Application, si un candidat est retenu : `RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` (`ForScenery` : niveau de base et seuil alpha des matériaux dont la texture de base est `PalmLeave.tga` ou `PalmLeave2.tga`, désignés par ces deux noms de texture et non par la famille, en constantes nommées, commentaire renvoyant à l'ADR-0037 et disant que la cause de l'écart n'est pas établie ; leur famille et leur rugosité ne changent pas) ; la recette désigne ainsi les quatre matériaux visés, et seulement eux ; recuisson ; ADR-0037 `Proposed` ; doc de banc ; ce plan. Le cuiseur et son vérificateur ne changent que si une clé nouvelle est nécessaire (elle ne l'est pas : `base_color` et `alpha_cutoff` existent).
- Validation : validation commune ; en plus, `git status` du contenu limité aux quatre matériaux visés ; les 95 autres matériaux cuits identiques octet pour octet (99 fichiers `.material` moins les quatre, compte contrôlé sur la liste des fichiers).
- Arrêts : arrêts communs ; un matériau recuit hors des quatre visés, ou un des quatre non recuit.
- Commit : `fix(racing-casa): bring the palm foliage toward the original`

> Validation (2026-11-18), sorties dans `artifacts/pbr-handover/s24/g3/` (`measure_leaves.txt`, `measure_leaves.json`, `cook-verify-before.txt`, `slice_check.txt`, `applied/`, `ref/`), script des candidats `s24/scripts/leafcand.py`.
> - Mesure : `measure_leaves.py` inchangé (empreinte `35ebecfb2d96756d` contrôlée avant et après). Base capturée sur une copie fraîche du build, identique octet pour octet à `s24/g2/ref` (15 vues). Onze candidats capturés, chacun modifiant les quatre matériaux de feuilles et eux seuls.
> - Règle : les onze candidats et la base sont éligibles. Plus petit écart `D` : 13,88 (gris 170, seuil 0,5) ; à moins de 1,0 : 170 / 0,35 (14,19), 150 / 0,5 (14,39), 150 / 0,35 (14,79). **Retenu : gris 170, seuil 0,5** (seuil de l'original d'abord, puis le gris le plus haut). Base : `D` 26,99, `E` 15,312.
> - Application : `RaceMaterialRecipes.ForScenery`, constantes `PalmLeafBaseLevel` 170 et `PalmLeafAlphaCutoff` 0,5 pour les matériaux dont la texture de base est `PalmLeave.tga` ou `PalmLeave2.tga` ; famille et rugosité inchangées ; cuiseur et vérificateur non touchés.
> - Cuiseur : `--verify` avant recuisson, 8 écarts, tous sur les quatre matériaux visés (`base_color` et `alpha_cutoff` de chacun) ; après recuisson 0 écart, `--check` 0, générateur `--check` 0 ; `git status` du contenu : les quatre matériaux visés et rien d'autre (99 fichiers `.material`, 95 inchangés).
> - Build à 0 erreur ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès ; aucun `[Warning]` ni `[Error]`.
> - Rejeux deux fois, identiques entre eux ; références `s24/g3/ref/`. Les 15 vues en `linear` sont identiques octet pour octet aux captures de `b170-c0.5`.
> - Candidat toujours retenu : `measure_leaves.py --root s24/g3/applied base b170-c0.5` : `D` 13,88, `E` 15,199, `choice: b170-c0.5` ; `s24/g3/measure_leaves.json` non réécrit.
> - `E` : `linear` 15,312 -> 15,199 ; `legacy` 20,009 -> 19,959. Sondes inchangées : `linear` 20/34, 2/4, 27/33, 3/3 ; `legacy` 4/34, 0/4, 27/33, 3/3.
> - Interface et audit : les six écrans sans monde 3D et `ui-car-selection` identiques à `s24/g2/ref` ; `ui-pause` 0,080 %, `ui-race-finished` 0,196 %, `ui-race-hud` 0,393 % ; 22 des 30 captures d'audit changent (jusqu'à 25,9 %, `audit-Expert-helper-palms`), 8 sont identiques ; vue la plus changée : `Expert-sector-20`, 5,2 %.
> - Documents : ADR-0037 `Proposed`, ligne d'état sur l'ADR-0028 ; section « Update after the palm foliage » du doc de banc ; table écrite et relue par `s24/scripts/gen_docs_g3.py`.
> - Regardé sur `s24/judge/3-feuillage-avant-apres.jpg` : les feuilles sont plus sombres et moins vives sur les trois vues, proches de l'original sur `Expert-sector-20` et `Expert-start` ; le grand palmier de `Beginner-sector-20` reste plus clair et nettement plus large que dans l'original, où ses palmes sont fines et sombres (surface de feuilles 2,64 fois celle de l'original) : écart ouvert.

### ⏳ G4 — Vitres des immeubles (ADR-0038)

- Prérequis : G3 clos. Base : la référence en place.
- Mesure (D1), en `linear`, sur une copie fraîche du dossier de build. Script : `s23/scripts/measure_windows.py`. Matériaux et clés : `hotel01` = `Models/Hotel01/39.material`, `hotel02` = `Models/Hotel02/fenster.material`, `casino01` = `Models/Casino01/39.material`. Captures sous `s24/g4/`, 15 vues chacune :
  - `base/` ;
  - `mark-<clé>/` : le matériau seul modifié en marqueur (`base_color` noir, `emissive_color` (10 ; 0 ; 10), `receive_shadows` faux), pour délimiter ses pixels ;
  - `<clé>-s0-blegacy/`, `<clé>-s1-b224/`, `<clé>-s0-b224/` : le matériau seul modifié, `receive_shadows` faux (`s0`) ou vrai (`s1`), couleur de base d'origine (`blegacy`) ou gris 224 (`b224`).
  Lancement : `python artifacts/pbr-handover/s23/scripts/measure_windows.py hotel01 hotel02 casino01`.
- Règle (dans l'en-tête du script), par matériau : pas de changement si son masque a moins de 500 pixels ; éligible si `E` au plus celui de la base et si les critères 1, 3 et 4 des sondes ne baissent pas ; retenu : le plus petit écart moyen `M` à l'original sur les pixels du matériau, et, à moins de 1,0, l'état le plus proche de l'état de départ.
- État combiné : les états retenus des trois matériaux sont appliqués ensemble sur la copie et capturés (`s24/g4/combined/`). Contrôle par le même script figé, dans un dossier à part : `s24/g4/gate/` reçoit une copie de `base/`, des trois `mark-<clé>/` et des neuf dossiers de candidats de `s24/g4/`, puis, pour chaque matériau qui a un état retenu, le dossier de cet état est remplacé par une copie de `combined/` ; commande `python artifacts/pbr-handover/s23/scripts/measure_windows.py --root s24/g4/gate hotel01 hotel02 casino01`, sortie gardée. Réussi si, pour chaque matériau qui a un état retenu, la ligne de cet état porte `eligible` (`E` au plus celui de la base, critères 1, 3 et 4 non abaissés) et si sa ligne `choice for <clé>:` nomme le même état ; sinon rien n'est appliqué.
- Application, si au moins un matériau change : `RacingGame.Gameplay/Import/RaceMaterialRecipe.cs` (réception de l'ombre de la recette, vraie par défaut) et `RaceMaterialRecipes.cs` (les matériaux retenus, désignés par leur modèle et leur nom, valeurs en constantes nommées, commentaire renvoyant à l'ADR-0038) ; `scripts/RgceAssetCooker` (le cuiseur écrit `receive_shadows` depuis la recette, le vérificateur le contrôle au lieu d'exiger vrai, README) ; recuisson ; ADR-0038 `Proposed`, avec une ligne d'état sur l'ADR-0033 si la couleur de base d'un verre d'immeuble change (elle y est dite inchangée) ; doc de banc ; ce plan.
- Validation : validation commune, les 15 vues étant comparées à `s24/g4/combined/`, et le contrôle « candidat toujours retenu » se faisant ainsi : `s24/g4/applied/` est construit comme `s24/g4/gate/`, les rejeux d'après application remplaçant le dossier de l'état retenu de chaque matériau qui en a un ; commande `measure_windows.py --root s24/g4/applied hotel01 hotel02 casino01` ; réussi si, pour chaque clé, la ligne `choice for <clé>:` nomme le même état que dans la mesure (« none » compris), la valeur de `M` imprimée n'entrant pas dans la comparaison, et si la ligne de chaque état retenu porte toujours `eligible` ; en plus, `git status` du contenu limité aux matériaux retenus ; les autres matériaux cuits identiques octet pour octet ; la sonde `building-away-1` (mur du casino, noir dans l'original) notée avant et après.
- Arrêts : arrêts communs ; état combiné non conforme.
- Commit : `fix(racing-casa): light the window glass of the buildings as the original`

### ⏳ G5 — Vitres de la voiture (ADR-0039)

- Prérequis : G4 clos. Base : la référence en place.
- Mesure (D1), en `linear`, sur une copie fraîche du dossier de build. Script : `s23/scripts/measure_glass.py`. Matériau : `Models/Car/Standard 19.material`, clé `base_color`. Captures sous `s24/g5/` : `g0/` (état de départ, base 224 ; 224 ; 224), puis `g1/` à `g4/` au plus, chacune avec la couleur de base que le script imprime après la précédente (ligne `next:`). Lancement : `python artifacts/pbr-handover/s23/scripts/measure_glass.py g0:224,224,224 [g1:<r>,<g>,<b> …]`, relancé après chaque capture avec la liste complète.
- Règle (dans l'en-tête du script) : l'itération s'arrête dès que le plus grand écart de canal de la boîte de la lunette arrière est d'au plus 6, ou après `g4` ; éligible si le critère 3 est au moins à 27 et `E` au plus celui de `g0` + 0,02 ; retenu : le plus petit écart moyen de la boîte ; rien n'est appliqué si `g0` gagne.
- Application, si un candidat est retenu : `RaceMaterialRecipes.cs` (`LegacyCarGlassBaseColor`, commentaire mis à jour : la couleur n'est plus un gris, elle compense la couleur du ciel reflété ; renvoi à l'ADR-0039) ; `RacingGameCasaEngine/Bootstrap/LegacyImportProfileVerifier.cs` seulement si une valeur qu'il épingle change ; recuisson ; ADR-0039 `Proposed`, ligne d'état sur l'ADR-0033 (sa décision D1 fixait 224) ; doc de banc ; ce plan.
- Validation : validation commune, le contrôle « candidat toujours retenu » se faisant ainsi : `s24/g5/applied/` reçoit une copie de tous les états mesurés, de `g0/` au dernier `g<n>/`, chacun avec son dossier `compare/`, le dossier du candidat retenu `g<K>/` étant remplacé par les rejeux d'après application ; commande `python artifacts/pbr-handover/s23/scripts/measure_glass.py --root s24/g5/applied g0:224,224,224 … g<n>:<r>,<g>,<b>`, avec la même liste complète d'arguments que la dernière mesure ; réussi si la ligne `choice:` nomme le même `g<K>` ; en plus, `git status` du contenu limité à `Models/Car/Standard 19.material` ; une planche avant / après de `ui-car-selection` pour l'auteur (D6), et la boîte du pare-brise du carrousel notée avant et après.
- Arrêts : arrêts communs.
- Commit : `fix(racing-casa): neutralise the colour of the car glass`

### ⏳ G6 — Vérification indépendante et clôture

- Prérequis : G1 à G5 clos (chacun ✅ avec un changement, ✅ sans rien d'appliqué, ou ⚠️). Si aucune tranche n'a changé le jeu, G6 se réduit à la clôture.
- Vérification fraîche en trois volets, sur l'état final :
  - lancements : builds, cuiseur, générateur, `--smoke-frontend`, `--verify-legacy-import-profile` ; un rejeu de chaque sorte identique à la dernière référence produite ; empreintes des scripts figés inchangées ; chaque script de mesure relancé sur une copie des captures gardées redonne la table et le choix notés ; état du moteur conforme à G1 si G1 est ✅ ; aucune lecture de `RGCE_BENCH_` dans le code committé ;
  - documents : chiffres des ADR écrites, du doc de banc et des notes du plan égaux aux sorties gardées ; chaque règle réappliquée à la main ; aucune ADR existante réécrite ; pour une tranche sans changement, absence d'ADR et de changement de contenu contrôlée ;
  - images : les planches de chaque tranche qui a changé le jeu regardées, vue par vue pour les 15 vues et les 3 tunnels entre `s22/s2/ref` et l'état final ; tout défaut nouveau, et ce qui est devenu plus mauvais qu'avant.
- Constats traités selon leur priorité (P0 et P1 : correction dans le périmètre ou arrêt ; P2 introduit : bloquant ; le reste noté).
- Clôture : `ai-agent/HANDOVER.md` (nouvelle section), ligne de ce plan dans `ai-agent/README.md`, points ouverts.
- Commit : `docs(ai-agent): record the verification of the shadow colour and decor slices`

## Hors périmètre

- L'ombre en perspective de l'original, ses ombres portées sur la route près de la voiture, le flou d'écran de l'ombre ; toute évolution du moteur autre que `FilterTaps`.
- Le carrousel (sauf l'effet des vitres de la voiture, D6) ; l'émissive du tunnel (D5) ; la route (close).
- La couleur de la voiture de course, les plafonds de tunnel, le halo du soleil, les autres écarts d'O7 de `rgce-render-gaps-tasks.md`.
- L'avance de `main` et de `remaster`, tout push.

## Points ouverts

- O1 (auteur, après le plan) : validation des ADR écrites ; avance de `main` du moteur et de `remaster` du jeu ; push, moteur d'abord.
