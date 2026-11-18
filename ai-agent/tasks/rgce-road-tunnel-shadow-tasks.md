# Plan agent IA — Route et ombre du tunnel

Suite de `rgce-render-gaps-tasks.md` (point O9, analyse du 2026-11-18). L'auteur a demandé ce plan le 2026-11-18 (« Oui, écris le plan ») après avoir vu les planches `artifacts/pbr-handover/s21/judge/`. Approuvé par l'auteur le 2026-11-18 (« Oui, en autonomie ») : exécution en mode autonome, sans push ni avance de `main`, `master` ou `remaster`.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

1. Le jeu prend la correction du moteur qui rend net le bord des ombres sur les surfaces à normal map (plan du moteur `CasaEngine/ai-agent/tasks/shadow-normal-offset-tasks.md`), et ses références sont refaites.
2. La route retrouve le reflet du soleil de l'original et son niveau de gris au soleil, par son matériau : rugosité plus basse et lumière d'environnement retirée de la route.

Ce que ce plan ne livre pas est dans « Hors périmètre ».

## État vérifié (2026-11-18, jeu `f5f5578` sur `chantier/rgce-pbr-handover`, moteur `4e867fec3` sur `chantier/draw-order-emissive-shadow`)

Faits de l'analyse (détail et chiffres : O9 de `rgce-render-gaps-tasks.md` ; sorties et scripts : `artifacts/pbr-handover/s21/`) :

- Route de l'original (`RacingGameOriginal/Core/Content/Shaders/NormalMapping.fx`, `PS_SpecularRoad20` ; `Graphics/Material.cs:30-37`) : texture × (40/255 + N·L × 210/255) + N·L × reflet de Phong de puissance 24, blanc, masqué par l'alpha de `Road.tga`, qui vaut 255 partout ; pas de reflet du ciel ; l'ombre multiplie le tout.
- Route du port : `RacingGameCasaEngine/Content/Materials/Track/Road.material`, `lit-pbr`, base 210, métal 0, rugosité 0,85 (famille `Road` de `RacingGame.Gameplay/Import/RacingGamePbrMaterialMapping.cs:16`, partagée avec le dessous de route, les colonnes et le tunnel ; recette `RaceMaterialRecipes.ForRoad`, `RaceMaterialRecipes.cs:162`). Le cuiseur (`scripts/RgceAssetCooker/Cooker.cs`, `CreateMaterialAsset`) n'écrit ni `occlusion_texture` ni `occlusion_strength`.
- Clés du moteur déjà là (`lit-pbr`) : `occlusion_texture` (canal rouge) et `occlusion_strength` (défaut 1) ; l'occlusion vaut 1 + force × (rouge − 1) et multiplie la lumière d'environnement seule, diffuse et spéculaire (`LitPbr.fx:449-450`), pas la lumière directe.
- Mesure, 423 points de piste projetés dans les 12 vues hors vues d'ensemble (`s21/scripts/measure_road.py` ; classes fixées sur l'original seul : 223 points « éclairés », 73 « assombris », 11 « reflet ») : route éclairée, luminance médiane 84,0 dans l'original, 101,8 dans le port ; boîte du reflet de `Beginner-sector-50` (x 450-660, y 560-700), luminance moyenne 209,9 pour 110,5 ; écart moyen par canal des 15 vues (E) 17,759.
- Essai mesuré (copie du build, rugosité 0,35, texture d'occlusion noire, force 1) : route éclairée 84,3 ; boîte du reflet 185,8 ; E 15,789 ; route dans les tunnels (boîte x 520-760, y 600-700) 7,0 / 11,9 / 15,8 pour 8,8 / 12,5 / 15,4 avant et 11,7 / 21,8 / 31,1 dans l'original.
- Ombre du tunnel : défaut du moteur (décalage `NormalBias` le long de la normale perturbée par la normal map), prouvé par essai (`NormalBias` 0 : bord net ; 0,3 : aggravé). Réglages du monde de course : `RaceWorldFactory.cs:84-91` (`NormalBias` 0,1).
- Référence du port : `artifacts/pbr-handover/s20/r4/ref/` (15 vues et tunnel dans les deux modes, interface, audit). Le dossier de build copié hors dépôt `D:/development/rgce-bench-r7/build` reproduit cette référence octet pour octet (15 vues et tunnel en `linear`).
- Dernier ADR du jeu : 0033.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Cible, mesure et méthode : celles de `rgce-render-gaps-tasks.md` (D1 à D3, D6) : original mesuré contre `s1c/ref` et `s3/ref-tunnel`, variante `nopost` ; règle écrite avant la mesure et appliquée mécaniquement ; candidats de matériaux sur une copie du dossier de build hors du dépôt ; le mode `legacy` est mesuré et noté, avec le seul critère 3 des sondes (au moins 27 sur 33). Sorties de ce plan : `artifacts/pbr-handover/s22/`. |
| D2 | L'ombre du tunnel se corrige dans le moteur, pas par un réglage du jeu : `NormalBias` reste à 0,1 (pas de contournement dans le jeu d'un défaut du moteur). |
| D3 | La lumière d'environnement est retirée de la route par les clés existantes `occlusion_texture` et `occlusion_strength` du matériau, avec une petite texture noire ; aucune évolution du moteur. Approuvé par l'auteur le 2026-11-18 sur la présentation de l'essai. C'est un choix de données de matériau : l'original ne donne à la route aucun reflet du ciel. |
| D4 | Seul le dessus de la route change (`Material.Track.Road`). Le dessous, les colonnes et le tunnel gardent la famille `Road` (0 ; 0,85). |
| D5 | L'assombrissement de la route que l'original montre près de la voiture sur 73 des 423 points (sans objet projetant, mécanisme non établi) n'est pas imité. |
| D6 | Le choix entre les réglages B et C de l'émissive du tunnel (O4 de `rgce-render-gaps-tasks.md`) reste à l'auteur ; S1 lui donne une planche et des chiffres refaits avec l'ombre corrigée. L'état B reste en place. |
| D7 | Le matériau de la route est une décision de réglage : ADR-0034 `Proposed`, avec une ligne d'état sur l'ADR qui porte la valeur de la famille `Road` (à retrouver par l'exécutant, `rg "0.85" docs/decisions`), sans réécrire d'ADR existante. |
| D8 | Le pointeur du sous-module avance sur `chantier/shadow-normal-offset` du moteur. `main` du moteur, `master` et `remaster` du jeu ne sont pas avancés par ce plan, et rien n'est poussé. |

## Règles d'exécution pour l'agent

- Branche `chantier/rgce-pbr-handover` (suite du chantier). `git add` fichier par fichier ; jamais `.serena/`, `.codegraph/`, `log.txt` ; aucune commande git dans `CasaEngine/MGUI` ni `CasaEngine/NvgSharp` ; jamais de `git submodule update`, `checkout`, `switch`, `reset`. Commandes de commit commençant par `cd D:/...`, message par fichier et `git commit -F`.
- Un lancement du jeu à la fois, jamais pendant un build ; rebuild avant tout lancement après un changement de code ou une cuisson.
- Builds à 0 erreur ; cuiseur `--verify` (0 écart après cuisson) et `--check` ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès, journaux sans `[Warning]` ni `[Error]`.
- Un commit par tâche, ce plan mis à jour dans le même commit ; messages, code, docs et ADR en anglais ; plan en français ; fins de ligne des fichiers existants conservées (pas de `sed -i`).
- Chiffres du doc de banc et de l'ADR produits par script et relus par un second script.
- Chaque tranche est relue avant exécution (`plan-verifier`) et l'ensemble est vérifié après (`verifier` frais, S3).
- Retour arrière de toute tranche : `git revert` de son commit (le contenu cuit revient avec lui) ; les sorties sous `artifacts/pbr-handover/s22/` sont gardées.

## Légende des statuts

⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Tâches

### ⏳ S1 — Jeu sur le moteur corrigé, références refaites

- Prérequis : plan du moteur `shadow-normal-offset-tasks.md` terminé, T2 **CONFIRMED**. Si ce plan-là est bloqué, S1 passe en ⚠️ et S2 part de l'état actuel (référence `s20/r4/ref`).
- Changement : le pointeur du sous-module `CasaEngine` avance sur la tête de `chantier/shadow-normal-offset` ; aucun autre fichier du jeu, aucun contenu.
- État du moteur construit, contrôlé avant le build et noté : le `HEAD` de `CasaEngine` est la tête de `chantier/shadow-normal-offset` (empreinte notée) ; cette tête contient le commit `fix(rendering): offset the shadow lookup along the geometric normal` (`git merge-base --is-ancestor`) ; `git -C CasaEngine status --short` ne montre rien hors de `MGUI` et `NvgSharp`. Après le build, le shader `LitPbr` compilé du dossier de build diffère de celui de la copie d'avant la correction (`D:/development/rgce-bench-r7/build`), preuve que le build a pris le shader corrigé. Après les rejeux, le pointeur committé est cette même empreinte.
- Validation :
  - build à 0 erreur ; cuiseur `--verify` 0 écart et `--check` 0 manquant ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès ;
  - rejeux : 15 vues et vues de tunnel dans les deux modes, dix écrans d'interface, 30 captures d'audit, chacun deux fois, identiques entre les deux rejeux ; références sous `s22/s1/ref/` ;
  - écrans d'interface sans monde 3D, identiques octet pour octet à `s20/r4/ref/ui` : `ui-splash`, `ui-main-menu`, `ui-highscores`, `ui-options`, `ui-help`, `ui-track-selection` ;
  - vues 3D, bornées : les 15 vues en `linear`, les 30 captures d'audit, et les quatre écrans d'interface qui montrent un monde 3D (`ui-car-selection`, dont le carrousel a ses propres ombres avec un `NormalBias` de 0,05 ; `ui-pause`, `ui-race-finished`, `ui-race-hud`). Pour chacune, contre `s20/r4/ref`, la part des pixels qui changent de plus de 8 niveaux sur un canal est au plus de 3 % (l'essai à `NormalBias` 0 donnait au plus 1,9 % sur les 15 vues). Les vues de tunnel n'ont pas cette borne : c'est là que le défaut se voit (l'essai y changeait 3,2 % des pixels d'Expert) ;
  - E en `linear` au plus 17,759 + 0,05 ; critère 3 des sondes au moins 27 sur 33 dans les deux modes ;
  - les trois vues bornées les plus changées sont regardées avec un masque des pixels changés : les changements sont sur des bords d'ombre, la note le dit vue par vue ;
  - preuve que la correction agit, sur la vue `Expert-tunnel` en `linear` : l'image diffère de `s20/r4/ref/tunnel-linear` ; la mesure de moucheture de la paroi droite est au plus de 0,60 %. La mesure est celle du script `artifacts/pbr-handover/s22/scripts/speckle.py`, écrit et figé le 2026-11-18 avant la correction (empreinte SHA-256 commençant par `6dbe28b573083f62`), qui fait foi : boîte x 640-1280, y 0-420 ; luminance 0,299 R + 0,587 G + 0,114 B tronquée sur 8 bits ; médiane 5 × 5 de PIL (`ImageFilter.MedianFilter(5)`) sur la boîte recadrée, avec son traitement du bord ; part des pixels qui s'écartent de la médiane de plus de 12 niveaux. Avant la mesure d'après correction, `speckle.py --calibrate` doit redonner, à 0,0005 près, les quatre valeurs d'étalonnage et elles sont notées : 0,863 % pour `s21/base-tunnel/Expert-tunnel.png` (état actuel), 0,348 % pour `s21/shadow-nb0-tunnel/Expert-tunnel.png` (essai à `NormalBias` 0), 2,923 % pour `s21/shadow-nb0.3-tunnel/Expert-tunnel.png` (essai à 0,3), 0,363 % pour `s3/ref-tunnel/Expert-tunnel-nopost.png` (original). Le recadrage de cette paroi est regardé à côté de ceux de `s21/base-tunnel` et de `s21/shadow-nb0-tunnel`, et la note dit si le bord déchiqueté et les mouchetures de la paroi et de la plaque de lampe ont disparu ;
  - tunnel : planche original / avant / après pour les trois tunnels ; la mesure de R4 (`s20/r4/scripts/tunnelmeas.py` : médiane de la bande haute, fenêtre [28,882 ; 31,882], écart des trois vues) refaite pour l'état B en place et, sur une copie du dossier de build, pour C (facteur 0,5, niveau 0,6) et A (facteur 0, niveau 0,5) ; chiffres et planche A / B / C notés pour l'auteur (D6). Si la médiane de B sort de la fenêtre, c'est noté et laissé à l'auteur.
- Arrêts, sans commit : état du moteur non conforme (tête, commit de T1 absent, arbre du sous-module modifié, shader compilé inchangé) ; rejeux différents entre eux ; une vue bornée au-delà de 3 % ; un changement ailleurs que sur un bord d'ombre ; critère 3 sous 27 ; un des six écrans sans monde 3D qui change ; étalonnage de `speckle.py` non reproduit, ou script modifié (empreinte différente) ; `Expert-tunnel` identique à la référence ou moucheture au-dessus de 0,60 % (la correction n'agit pas).
- Retour arrière : `git revert` du commit (le pointeur revient à `4e867fec3`).
- Commit : `chore(racing-casa): take the engine fix of the shadow normal offset`

### ⏳ S2 — Matériau de la route (ADR-0034)

- Prérequis : S1 fait (ou ⚠️ avec la référence `s20/r4/ref`, voir S1). L'état de départ de S2, noté « base », est la référence en place à ce moment ; ses chiffres sont remesurés avant les candidats.
- Mesure (D1), en `linear`, sur une copie fraîche du dossier de build, sans code temporaire.
  - Script : `artifacts/pbr-handover/s22/scripts/measure_s2.py`, écrit et figé le 2026-11-18 avant toute mesure (empreinte SHA-256 commençant par `3497b166e10de3ca`). Il fait foi : il calcule toutes les grandeurs ci-dessous, applique l'éligibilité et le choix, et écrit `measure_s2.json`. Ses classes de points viennent de l'original seul et ne dépendent d'aucun candidat. Contrôle fait le 2026-11-18 sur les captures de l'essai (`s22/check/`, copie de `s21/base` et de `s21/road-r0.35-b210-o1`) : il redonne base 101,8 / 110,5 / 17,759 / 8,8 / 12,5 / 15,4 et essai 84,3 / 185,8 / 15,789 / 7,0 / 11,9 / 15,8, saturation 0,000 % dans les deux cas.
  - Captures : sous `s22/s2/`, un dossier `<nom>/` (15 vues) et un dossier `<nom>-tunnel/` (3 vues de tunnel) par état ; `base` pour l'état de départ ; `r<rugosité>-o<force>` pour un candidat (par exemple `r0.35-o1`). Lancement : `python artifacts/pbr-handover/s22/scripts/measure_s2.py base r0.30-o0.5 …` (tous les candidats en une fois).
  - Candidats, sur `Track/Road.material` de la copie : rugosité 0,30 ; 0,35 ; 0,40 ; 0,45, croisée avec `occlusion_strength` 0,5 ; 0,75 ; 1 sur une texture d'occlusion noire, soit 12 candidats, plus la base. La base de couleur reste 210.
  - Grandeurs (luminance = 0,299 R + 0,587 G + 0,114 B sur les canaux en flottant ; définitions exactes dans l'en-tête du script) : `Lr`, luminance médiane des 223 points « éclairés » (original : 84,0) ; `G`, luminance moyenne de la boîte du reflet de `Beginner-sector-50`, x 450-660, y 560-700, bornes hautes exclues (original : 209,9) ; `E`, écart moyen par canal des 15 vues ; `Tb`, `Ta`, `Te`, luminance moyenne de la boîte de route x 520-760, y 600-700, bornes hautes exclues, de `Beginner-tunnel`, `Advanced-tunnel`, `Expert-tunnel` ; `Sat`, part, sur l'ensemble des pixels des 15 vues, de ceux dont un canal vaut 255.
  - Éligibilité : |`Lr` − `Lr` de l'original| ≤ 6 ; `G` ≥ 160 ; `Tb`, `Ta` et `Te` chacun au moins égal à celui de la base moins 3 ; `Sat` ≤ `Sat` de la base + 0,5 point ; `E` < `E` de la base.
  - Choix : parmi les candidats éligibles, le plus petit `E` ; à moins de 0,02 du plus petit, la rugosité la plus haute, puis la force d'occlusion la plus basse. Aucun éligible : rien n'est appliqué, la tranche se clôt avec ses chiffres et l'écart reste ouvert.
- Application, si un candidat est retenu :
  - `RacingGame.Gameplay/Import/RacingGamePbrMaterialMapping.cs` : nouvelle famille `RoadSurface` (métal 0, rugosité retenue) pour le seul dessus de la route (D4) ;
  - `RacingGame.Gameplay/Import/RaceMaterialRecipe.cs` : texture d'occlusion et force d'occlusion de la recette, documentées ; `RaceMaterialRecipes.ForRoad` les renseigne, valeurs en constantes nommées, commentaire renvoyant à l'ADR-0034 ;
  - nouvelle texture source noire `<stem>.png` sous `RacingGameCasaEngine/Content/Textures/` (nom et taille au choix de l'exécutant, notés) ;
  - `scripts/RgceAssetCooker` : la texture entre dans le plan de cuisson (`CookPlan.cs`) ; le cuiseur écrit `occlusion_texture` et `occlusion_strength` seulement quand la recette donne une texture d'occlusion ; le vérificateur (`Verifier.cs`) contrôle les deux clés ; `README.md` du cuiseur ;
  - catalogue : le cuiseur n'inscrit que `Texture.<stem>` (l'asset `.texture`) ; l'entrée `Image.<stem>` du `.png`, dont le `.texture` cuit porte l'identifiant, est écrite par `python scripts/generate_rgce_content_assets.py`, lancé après l'ajout du `.png` et avant la cuisson ;
  - `RacingGameCasaEngine/Bootstrap/LegacyImportProfileVerifier.cs` : touché seulement si une valeur qu'il épingle change ;
  - recuisson ; ADR-0034 `Proposed` et ligne d'état (D7) ; section du doc de banc ; ce plan.
- Validation après application :
  - cuiseur `--verify` avant recuisson : écarts sur le seul matériau de la route et sur la nouvelle texture (preuve que le contrôle mord) ; après : 0, et sa sortie ne porte pas la note « no Image.<stem> png entry » ; cuiseur `--check` 0 ; `python scripts/generate_rgce_content_assets.py --check` : sortie 0 ; `AssetInfos.json` a exactement deux entrées de plus qu'avant, `Image.<stem>` (png) et `Texture.<stem>` (texture) ; `git status` du contenu limité à `Road.material`, au `.png`, au `.texture` et à `AssetInfos.json` ; les 98 autres matériaux cuits (les 99 fichiers `.material` de `Content/Materials` moins `Track/Road.material`, compte contrôlé sur la liste des fichiers) identiques octet pour octet ;
  - rejeux `linear` des 15 vues et du tunnel, deux fois, identiques entre eux ; contre les captures du candidat retenu : identiques, ou au plus 1 niveau d'écart sur au plus 0,1 % des pixels par vue (la texture cuite peut différer de celle de la copie), noté ;
  - contrôle que le candidat appliqué reste éligible, dans un dossier à part pour ne rien écraser : `s22/s2/applied/` reçoit une copie de `s22/s2/base/` et de `s22/s2/base-tunnel/`, et les rejeux d'après application sous le nom du candidat retenu (`s22/s2/applied/<nom>/` et `s22/s2/applied/<nom>-tunnel/`) ; commande `python artifacts/pbr-handover/s22/scripts/measure_s2.py --root s22/s2/applied base <nom>` ; sa sortie doit donner ce candidat éligible, et elle écrit son propre `s22/s2/applied/measure_s2.json`. Les dossiers `s22/s2/<nom>/`, `s22/s2/<nom>-tunnel/` et le fichier `s22/s2/measure_s2.json` (la base et les 12 candidats) ne sont jamais réécrits ;
  - `legacy` rejoué et mesuré, noté ; critère 3 des sondes au moins 27 sur 33 dans les deux modes ; les quatre critères notés dans les deux modes ;
  - les six écrans d'interface sans monde 3D (liste de S1) identiques à la référence ; les quatre autres écrans et les 30 captures d'audit rejoués deux fois, identiques entre eux, changements nommés ;
  - `--smoke-frontend` et `--verify-legacy-import-profile` en succès ; références sous `s22/s2/ref/` ; planche original / base / retenu des vues `Beginner-start`, `Beginner-sector-50`, `Advanced-sector-20`, `Expert-sector-50`, `Expert-sector-80` et des trois tunnels.
- Arrêts, sans commit, l'état de base étant gardé : script de mesure modifié (empreinte différente) ; écart du cuiseur hors de la route et de la nouvelle texture ; un des contrôles de catalogue en échec (note « no Image », `--check` du générateur, nombre d'entrées) ; rejeux différents entre eux ou au-delà de la tolérance contre le candidat ; candidat qui n'est plus éligible une fois appliqué ; critère 3 sous 27 ; un des six écrans sans monde 3D qui change.
- Retour arrière : `git revert` du commit.
- Commit : `fix(racing-casa): give the road its sun sheen and take the sky light off it`

### ⏳ S3 — Vérification indépendante et clôture

- Prérequis : S1 clos (✅, ou ⚠️ parce que le plan du moteur est bloqué) et S2 clos (✅ avec un candidat appliqué, ou ✅ sans rien d'appliqué faute de candidat éligible). Si S1 est ⚠️ et que S2 n'a rien appliqué, le jeu n'a pas changé : S3 se réduit à la clôture, sans vérification.
- Vérification fraîche, en trois volets comme en R6. La référence de chaque contrôle dépend de l'issue des tranches :
  - lancements : builds, cuiseur, `--smoke-frontend`, `--verify-legacy-import-profile` ; rejeux identiques à la dernière référence produite (`s22/s2/ref` si S2 a appliqué un candidat ; sinon `s22/s1/ref` si S1 est ✅) ; si S1 est ✅, les contrôles de S1 contre `s20/r4/ref` refaits ; si S2 a mesuré, la table des candidats recalculée à partir des captures gardées et la règle réappliquée (elle doit donner le choix noté, ou « aucun éligible ») ;
  - documents : notes du plan égales aux tableaux de mesure ; si S2 a appliqué un candidat, chiffres de l'ADR-0034 et du doc de banc égaux à ces tableaux, et aucune ADR existante réécrite ; si S2 n'a rien appliqué, absence d'ADR-0034 et de changement de contenu contrôlée ;
  - images : planche du tunnel de S1 si S1 est ✅ (bord d'ombre) ; planche de la route de S2 si un candidat est appliqué (reflet, gris de la route) ; tout défaut nouveau.
- Constats traités selon leur priorité (P0 et P1 : correction dans le périmètre ou arrêt ; P2 introduit : bloquant ; le reste noté).
- Clôture : `ai-agent/HANDOVER.md` (nouvelle section), ligne de ce plan dans `ai-agent/README.md`, points ouverts.
- Commit : `docs(ai-agent): record the verification of the road and tunnel shadow`

## Hors périmètre

- L'assombrissement de la route près de la voiture dans l'original (D5) et les ombres portées manquantes sur la route.
- La route trop sombre dans les tunnels et les ombres du terrain (ambiant, O6 de `rgce-render-gaps-tasks.md`).
- Le choix B ou C de l'émissive du tunnel (D6), la teinte des vitres, les autres écarts d'O7.
- La couleur de base de la route (les essais à 170 et 150 font moins bien), une texture de rugosité tirée de la texture de la route.
- Toute évolution du moteur autre que la correction de son plan ; l'avance de `main`, de `master` et de `remaster` ; tout push.

## Points ouverts

- O1 (auteur, à l'approbation) : mode d'exécution, autonome ou avec question à chaque décision ; base de la branche du moteur (O1 du plan du moteur).
- O2 (auteur, après S1) : réglage de l'émissive du tunnel, B ou C, sur la planche refaite (D6).
