# Note de passation (2026-11-16)

Pour reprendre le travail sur un autre PC, avec une nouvelle session d'agent qui n'a pas l'historique de la conversation.

## 1. Avant d'éteindre ce PC (à faire par l'auteur)

Rien n'est poussé : sans ces commandes, l'autre PC n'aura rien. État à jour : voir le §7, qui prime sur les numéros de commit de ce §1.

```bash
git -C D:/development/repo/RacingGame/CasaEngine push origin main
```

```bash
git -C D:/development/repo/RacingGame/CasaEngine push origin chantier/pbr-clearcoat-ssao-fxaa chantier/pbr-detail-texture
```

```bash
git -C D:/development/repo/RacingGame push origin remaster
```

Le moteur est à `48d720a6` sur `main` (80 commits d'avance sur `origin/main`). Le jeu est sur `remaster` (43 commits d'avance), sous-module pointé sur `48d720a6`. Les deux branches de chantier du moteur sont entièrement contenues dans `main` ; les pousser est facultatif.

Ne sont **pas** dans git et restent sur ce PC : `.codegraph/`, `.serena/`, `MGUI/`, `log.txt` (jeu), la modification ` M MGUI` (moteur), les captures de `artifacts/`, les notes de mémoire de l'agent.

## 2. Sur l'autre PC

1. Cloner ou mettre à jour le jeu, branche `remaster`, sous-module compris (`git submodule update --init` à la première installation uniquement).
2. Vérifier : `git -C CasaEngine log --oneline -1` donne `48d720a6`.
3. Recopier les scripts de référence du moteur : `ai-agent/handover/pbr-baseline/*.sh` vers `CasaEngine/CasaEngine/artifacts/pbr-baseline/` (dossier ignoré par git). Leur variable `BIN` vaut par défaut `/d/development/repo/RacingGame/...` : la redéfinir si le dépôt est ailleurs.
4. Ouvrir une session d'agent dans le dossier du jeu et lui donner cette note à lire.

Les empreintes de capture dépendent du GPU et du pilote : celles notées dans les plans valent pour ce PC. Sur l'autre, reprendre une référence avant toute comparaison (jeu : tâche T0.2 du plan ; moteur : relancer les scripts une fois et noter les valeurs).

## 3. Où en est le travail

| Chantier | État | Plan |
|---|---|---|
| Moteur : PBR, pipeline couleur, IBL | Terminé, fusionné dans `main` | `CasaEngine/ai-agent/tasks/pbr-rendering-tasks.md` |
| Moteur : clear coat, SSAO, FXAA | Terminé, vérifié, fusionné dans `main` | `CasaEngine/ai-agent/tasks/pbr-clearcoat-ssao-fxaa-tasks.md` |
| Moteur : texture de détail de `lit-pbr` (E1) | Terminé, vérifié, fusionné dans `main` | tâche E1 du plan du jeu |
| Jeu : monde de course en PBR et `LinearHdr` | Exécuté et vérifié (T0.1 à T4.2) | `ai-agent/tasks/rgce-pbr-race-world-tasks.md`, section « Passation » |

## 4. Ce qui reste

À décider ou à régler par l'auteur :

- **Tunnel presque noir** dans le monde de course : l'original l'éclairait par un ambiant propre à son matériau, abandonné par la bascule. Relever sa couleur de base ou lui donner une lumière.
- **Voiture centrale du carrousel trop blanche** : lumière à π et IBL sans tonemapping (le carrousel reste en `Legacy`). Baisser l'intensité du carrousel.
- **Réglages à l'œil** : rugosité et métal par famille (`RacingGameCasaEngine/Bootstrap/RacingGamePbrMaterialMapping.cs`), exposition et intensités (`Worlds/RaceWorldFactory.cs`).
- **Jamais contrôlé à l'œil** : une course sur chaque piste, toutes les couleurs de voiture ; dans le moteur, les touches F et O de la démo PBR, FXAA et SSAO dans l'éditeur, les quatre contrôles manuels de l'ancien plan PBR (T1.7, T4.4, T8.2, T10.1).
- **Arbitrages à valider** : P1 à P7 du plan clear coat/SSAO/FXAA ; ADR 0112 à 0116 du moteur et ADR-0018 du jeu encore en `Proposed`.

Défauts connus, non corrigés :

- Banc de capture du jeu : quelques pixels isolés scintillent d'une capture à l'autre (répétable sur le rapport et les sondes, pas à l'octet). Cause non cherchée.
- Import glTF du moteur : un matériau `OPAQUE` dont le facteur de couleur de base a un alpha inférieur à 1 est compilé en transparent (`EditorAssetImportService.cs`).
- Moteur : une sonde de réflexion référencée par identifiant d'asset alloue et journalise à chaque image (`ReflectionProbeResolver`).
- Jeu : deux commentaires décrivent encore l'ancien éclairage (`RaceWorldFactory.cs:35-38`, `LegacyTrackSceneFactory.cs:948-953`) et `CreateLegacyLighting` calcule un ambiant inutilisé.

## 5. Règles de travail à rappeler à la nouvelle session

Préférences de l'auteur, qui ne sont pas toutes écrites ailleurs :

- Réponses en français, concises. Code, commits, `docs/` et ADR en anglais ; plans en français.
- **Jamais de push** par l'agent. Jamais de commit sur `main` (moteur) ni `master` (jeu). Fusion et pointeur du sous-module : seulement sur demande de l'auteur.
- Un commit par tâche, `git add` fichier par fichier. Ne jamais indexer `.codegraph/`, `.serena/`, `MGUI/`, `log.txt`, ni ` M MGUI` dans le moteur. Ne jamais lancer `git submodule update`, `checkout` ou `reset` dans `MGUI` ou `NvgSharp`.
- **Pas de contournement côté jeu** : un défaut ou un manque du moteur se corrige dans le moteur, selon `CasaEngine/AGENTS.md` (branche dédiée, ADR, plan).
- **Direction des matériaux** : « socle commun puis PBR ». On ne reproduit pas au pixel les formules de l'original ; ses comportements accidentels se tranchent au cas par cas (liste dans `docs/racinggame-pbr-preparation.md`, décisions prises incluses).
- Référence visuelle : `RacingGameOriginal/` dans le dépôt. Toute correction visuelle se prouve avec le banc `scripts/TrackCaptureCompare` (mode d'emploi dans son README).
- Travail de plus d'un commit : écrire un plan (`ai-agent/plan-template.md`), le faire approuver, puis exécuter tâche par tâche avec une vérification indépendante par phase.

Particularités de ce PC, à revérifier sur l'autre :

- Git refusait le dépôt (« dubious ownership ») : `git -c safe.directory=*` à chaque commande.
- Pas de Python. Message de commit écrit dans un fichier puis `git commit -F`.
- `.git/index.lock` posé par moments par GitExtensions : attendre, ne pas supprimer.
- Captures en 1280×720 (jeu) ; jamais pendant un build ou des tests ; fenêtres laissées ouvertes et non réduites.
- Tests du moteur : `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj --blame-hang-timeout 5m`, jamais `--no-build` (le projet n'est dans aucune solution). 6076 tests verts.
- Build du jeu : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug`.

## 6. Empreintes de référence du moteur sur ce PC

À titre de repère seulement (voir la réserve du §2).

| Script | Empreinte |
|---|---|
| `capture-v2.sh` (`Legacy`) | material `b73dfdd2684a8615`, environment `0cf97b7e08cc0871`, static-model `312dda61e950f6b9`, static-shadow `add5db24fbd43814`, render-to-texture `d45bc1b2d6630553`, ui-overlay `307d01b87c81303d`, frozen-frame `eb25f3917cfa6a0c`, tile-map `355772772016e47f` |
| `lin-v2.sh` (`LinearHdr`) | `0ba208b099b28b67` |
| `pbrdemo.sh` | `f42805b79f7380fb` |
| `pbrdemo.sh` avec `CASAENGINE_DEMO_COLOR_PIPELINE=Legacy` | `d79a0a224c1527cc` |
| `pbrdemo.sh` avec `CASAENGINE_PBR_DEMO_CLEARCOAT=1` | `2c176114771ec932` |

Les démos skinned, particles et split-screen ne sont pas déterministes.

## 7. Mise à jour : `origin/main` du moteur fusionné (2026-11-16)

Le premier push de `main` du moteur a été refusé : `origin/main` portait 60 commits faits sur une autre machine (chantier « MGUI zéro allocation »). Ils sont maintenant fusionnés.

- Moteur : `main` local à `863627b4` (fusion), 81 commits d'avance sur `origin/main`, 0 de retard. Le push de `main` passe désormais en avance rapide.
- **ADR renumérotés** : `origin/main` avait pris le numéro 0097. Les vingt ADR de ce travail, 0097 à 0116, sont devenus **0098 à 0117**. Les numéros cités dans les sections précédentes de cette note et dans les notes de tâches des plans sont ceux d'avant : ajouter 1 (exemple : la texture de détail de `lit-pbr` est l'ADR-0117, le clear coat l'ADR-0115).
- Sous-module `MGUI` du moteur : pointeur d'`origin/main` (`d0efd9b4`), arbre propre.
- Jeu : pointeur du sous-module avancé à `863627b4` ; renvois décalés dans `docs/racinggame-pbr-preparation.md`, `docs/racinggame-3d-capture-bench-findings.md` et l'ADR-0018.
- Contrôles après fusion : moteur, builds des deux solutions, 6119 tests verts, les douze empreintes de référence inchangées ; jeu, build 0 erreur, `--smoke-frontend` et `--verify-legacy-import-profile` en succès.
- **Non fait** : vérification indépendante de la fusion, et banc de capture du jeu sur le moteur fusionné (le backend d'interface du moteur a changé). À faire en premier à la reprise.

Commandes à lancer avant d'éteindre :

```bash
git -C D:/development/repo/RacingGame/CasaEngine push origin main
```

```bash
git -C D:/development/repo/RacingGame push origin remaster
```

## 8. État sur ce PC après la reprise (2026-11-17)

Le travail PBR de l'autre PC est porté sur ce PC, dans l'architecture d'ici (code du jeu dans `RacingGame.Gameplay`, générateurs, assets cuits). Registre complet : `ai-agent/tasks/rgce-pbr-handover-tasks.md` (tranches, décisions D1 à D11, carte des 46 commits de l'autre PC, Phases 1 à 14, points ouverts O1 à O15).

Branches et commits, rien n'est poussé :

- Jeu : `chantier/rgce-pbr-handover`, 144 commits d'avance sur `origin/remaster` (`90e3d99`), qu'elle contient ; `remaster` local à `ad9f892`, non touché.
- Moteur : `main` local à `b429b79ec` (fusion d'`origin/main`, décision D10), `origin/main` à `187a420d7`. Trois branches empilées au-dessus de `main` : `chantier/static-model-tangents` (`61b4e6fae`, tangentes dans `.staticModel`, ADR-0123), `chantier/texture-asset-mipmaps` (`2b64b0591`, mipmaps par asset `.texture`, ADR-0124), `chantier/pbr-ggx-back-facing` (`8a3933b36`, correction de GGX côté dos). Le pointeur du jeu est sur `8a3933b36`.

Fait et vérifié (chaque tranche par un vérificateur indépendant, l'ensemble par S2) :

- fusions du moteur et du jeu ; original de référence qui compile sur ce PC, multijoueur LAN retiré ;
- banc de capture contre l'original, mesure de scène, sondes ; placement des objets et ombres alignés sur l'original ;
- assets : tangentes, normal maps, cube du ciel, mipmaps ; matériaux cuits en `lit-pbr` par famille ;
- monde de course : lumière linéaire HDR (mode `--color-pipeline legacy` pour le banc), lumière unique de l'original à π, ambiant par le cube du ciel à 0,05, ombres douces et teintées, tangentes de la route et des tunnels, dessous de la route, sol de la ville, couleurs de base de l'original sur les matériaux de piste.

Référence du port sur ce PC : `artifacts/pbr-handover/s2/` (non versionné) et la section « Port reference on this PC » de `docs/racinggame-3d-capture-bench-local-baseline.md`. En `linear`, luminance moyenne 104,0 pour 96,3 dans l'original ; écart moyen par canal 19,8 / 19,4 / 19,0 ; sondes de classe ombre ou lumière 27 sur 33.

Ce qui reste :

- S3, tunnel presque noir (O1) ; S4, voiture du carrousel : ici environ deux fois plus sombre qu'avant le PBR, l'éclairage du carrousel de l'autre PC n'est pas porté (O2) ; S5, défauts connus du §4.
- O13, écarts de rendu avec l'original : faces à l'ombre dos au soleil bouchées et froides depuis l'ambiant à 0,05 ; ombres du terrain trop sombres en `linear` ; pavés de la ville sombres, ombre et lumière inversées sur `Beginner-sector-20` ; rapport route sur terrain 0,655 pour 0,348.
- O14 : famille du sol de la ville ; verre et feux arrière de la voiture noirs unis ; points ouverts de l'autre PC (cube du ciel de 512 sans mipmaps, `Casino01`).
- Décisions en attente de l'auteur : O9, O10, O11 ; ADR-0027 et ADR-0029 du jeu en `Proposed` ; ADR-0123 et 0124 du moteur ; O15 : contrôle du déterminisme de la correction de GGX sur l'autre PC avant toute remontée du moteur dans `main` ou tout push ; disposition prise en mode AUTO sur la cible de pixels blancs de S1f.
- Jamais contrôlé à l'œil ni joué à la main : une course lancée du menu sur chaque piste, les couleurs de voiture, le carrousel ; les preuves d'exécution viennent des modes d'automatisation.

Correction à cette note : l'ADR-0018 du jeu est `Accepted`, et non « encore en Proposed » comme le dit le §4.

## 9. Suite du 2026-11-17 : S3, S4, S5

Faits et vérifiés après la section 8, rien n'est poussé. Détail : Phases 15 à 17 du plan `ai-agent/tasks/rgce-pbr-handover-tasks.md`.

- **S3, tunnel** : l'original exporte sur demande une vue dans le plus long tunnel de chaque piste (`--capture-track-audit --tunnel-views`, référence `artifacts/pbr-handover/s3/ref-tunnel`) ; le tunnel porte une émissive texturée qui reprend l'ambiant propre à son matériau (ADR-0030, `Proposed`). Niveau : 0,5 en gris neutre, choisi par l'auteur après la mesure de trois valeurs intermédiaires (luminance des vues de tunnel 27,0 pour 29,7 dans l'original ; ADR-0030 `Accepted`). L'émissive n'est jamais ombrée : certaines zones presque noires de l'original sont plus claires ici (O16).
- **S4, carrousel** : lumière à π, ambiant écrit à 0,05 (ADR-0031, `Proposed`). Zone centrale de l'écran : luminance 55,3 pour 61,7 avant le PBR et 42,6 avant S4. La peinture grise ressort plus claire et plus froide qu'avant le PBR, les plateaux sont bleutés, la brillance de la peinture a disparu : O2.
- **S5, défauts connus** : moteur, branche `chantier/known-defects-gltf-alpha-probe` (`092835885`), empilée sur `chantier/pbr-ggx-back-facing` : un matériau glTF `OPAQUE` est importé opaque ; une sonde de réflexion par identifiant n'alloue plus à chaque image et ne retente pas un asset en échec. Le pointeur du jeu reste sur `8a3933b36`. Jeu : les captures de référence sont répétables octet pour octet (les copies de matériau de la voiture avaient un identifiant aléatoire, dont dépendait l'ordre de dessin). Les deux commentaires périmés du §4 n'existent que dans le code de l'autre PC.
- Référence du port sur ce PC : `artifacts/pbr-handover/s3b/` (15 vues, vues de tunnel, `ref-ui`, `ref-audit`), prise après le choix du niveau du tunnel ; depuis S5 un rejeu les reproduit à 0,00 de moyenne, et deux rejeux d'un même build sont identiques octet pour octet.

Décisions en attente de l'auteur, en plus de celles de la section 8 : O16 (écarts restants dans les tunnels), O2 (carrousel, ADR-0031), O17 (remontée de la quatrième branche du moteur ; tri des objets opaques du moteur).

## 10. Fusion du 2026-11-17 : derniers changements de MGUI et du moteur

Rien n'est poussé. Détail : Phase 18 du plan.

- MGUI : `develop` avancé de `d0efd9b4` à `f6ed1fcd` (= `origin/develop`, 14 commits).
- Moteur : `origin/main` (`7e3b8ff52`, fin du chantier « MGUI zéro allocation ») fusionné dans `main`, qui est à `006a245aa` (27 commits d'avance, 0 de retard) ; tests du moteur : 6394 lancés, 0 échec. Les 8 échecs GPU connus sur ce PC ont disparu (corrigés par `2a0a3712d` d'`origin/main`).
- Branche sur laquelle le jeu pointe : `chantier/rgce-pbr-on-main-2` (`29d3d2de2`) = `main` + tangentes, mipmaps, correction de GGX ; 6407 tests, 0 échec. Elle remplace `chantier/pbr-ggx-back-facing` (`8a3933b36`) comme pointeur du jeu (commit du jeu `a3957e6`).
- `chantier/known-defects-gltf-alpha-probe` (`092835885`) n'est fusionnée nulle part.
- Jeu sur le moteur fusionné : 15 vues, vues de tunnel, dix écrans d'interface et audit identiques octet pour octet à la référence `artifacts/pbr-handover/s3b/` ; le nouveau MGUI ne change aucun pixel de ces écrans.
- Vérifié par deux vérificateurs indépendants (lancements, audit des fusions).

`main` du moteur a été poussé par l'auteur le 2026-11-17 (`origin/main` = `006a245aa`) ; les branches du chantier restent locales, donc le commit du moteur sur lequel le jeu pointe (`29d3d2de2`) n'est pas sur le dépôt distant.

Le 2026-11-17 aussi : `remaster` local du jeu avancé en avance rapide sur `61c465e`, la fusion d'`origin/remaster` (82 d'avance, 0 de retard ; la branche du chantier garde 81 commits de plus), sans push ; l'auteur abandonne le contrôle de déterminisme de GGX sur l'autre PC (O15, O19).

Puis `remaster` local a été avancé au sommet de `chantier/rgce-pbr-handover` (commit qui porte cette ligne), sur demande de l'auteur, sans push : il contient tout le portage vérifié.

Enfin `main` local du moteur a été avancé sur `29d3d2de2` (`chantier/rgce-pbr-on-main-2`), sur demande de l'auteur, sans push : 16 commits d'avance sur `origin/main`, et le pointeur du jeu est dans `main`.

Dernière étape du 2026-11-17 : `chantier/known-defects-gltf-alpha-probe` fusionnée dans `main` local du moteur (`e8d65a6fa`, 6422 tests, 0 échec), le jeu pointe dessus (`f49e83e`), banc identique à `s3b`, vérifié par deux vérificateurs indépendants (O20). État distant relevé alors : `origin/main` du moteur = `29d3d2de2` (`main` local : avance et retard `6 0`) ; `origin/remaster` du jeu = `42f1d14`.

Ces deux branches ont été poussées par l'auteur le 2026-11-17 (`origin/main` du moteur = `e8d65a6fa`, `origin/remaster` du jeu = `144912e`, relevés après `git fetch`). Ensuite : ligne du plan des défauts connus corrigée dans `CasaEngine/ai-agent/README.md` (`8a73c5329`, documentation seulement, `main` local du moteur avancé dessus), et pointeur du jeu avancé sur ce commit sans relancer le banc.

À pousser par l'auteur : ce commit de `main` du moteur, puis `remaster` du jeu.

## 11. Écarts de rendu restants (2026-11-17)

Plan : `ai-agent/tasks/rgce-render-gaps-tasks.md` (R1 à R6, chaque tranche relue avant exécution, vérification finale **CONFIRMED** en trois volets). Rien n'est poussé ; `main` du moteur, `master` et `remaster` du jeu n'ont pas été avancés par ce plan.

- Moteur, branche `chantier/draw-order-emissive-shadow` (`4e867fec3`, 4 commits sur `main` `8a73c5329`) : ordre de dessin déterministe (ADR-0125), clé `emissive_shadow_factor` de `lit-pbr` (ADR-0126), deux corrections de documentation ; 6441 tests, 0 échec. Le jeu pointe dessus (`772d830`).
- Jeu : outil `scripts/TrackPlacementExporter` supprimé ; exposition du monde de course à 0,9 (ADR-0032) ; vitres de la voiture réfléchissantes, panneaux éclairés par une émissive qui porte l'ambiant de l'original, émissive du tunnel ombrée au niveau 0,6 à la place du 0,5 choisi par l'auteur (ADR-0033) ; carrousel mesuré, rien de retenu.
- Mesure contre l'original, 15 vues en `linear` : luminance 99,05 pour 96,29 (104,0 avant) ; écart moyen par canal 17,76 (19,41 avant) ; critère 3 des sondes 27 sur 33.
- Référence du port : `artifacts/pbr-handover/s20/r4/ref/` (15 vues, tunnel, interface, audit) ; `s20/r5/ref/ui` est identique.
- Laissés à l'auteur : O4 à O8 du plan (défauts de l'ombre sur l'émissive du tunnel et replis mesurés, teinte des vitres, carrousel, ombres du terrain, écarts les plus visibles restants) ; ADR 0032 et 0033 du jeu, 0125 et 0126 du moteur à valider ; avance de `main` du moteur et de `remaster`, push.

## 12. Route et ombre du tunnel (2026-11-18)

Plans : `ai-agent/tasks/rgce-road-tunnel-shadow-tasks.md` (S1 à S3) et, dans le moteur, `CasaEngine/ai-agent/tasks/shadow-normal-offset-tasks.md` (T0 à T2). Analyse d'origine : O9 de `rgce-render-gaps-tasks.md`. Chaque tranche relue avant exécution, vérifications finales **CONFIRMED**. Rien n'est poussé ; `main` du moteur, `master` et `remaster` du jeu n'ont pas été avancés.

- Moteur, branche `chantier/shadow-normal-offset` (`8ddcb31b0`, 3 commits sur `chantier/draw-order-emissive-shadow` `4e867fec3`, elle-même 4 commits sur `main` `8a73c5329`) : le décalage `NormalBias` de la lecture de l'ombre suit la normale de sommet et plus la normale perturbée par la normal map (`LitPbr.fx`, `LitForward.fx`) ; 6445 tests, 0 échec. Le jeu pointe dessus (`11096fa`).
- Jeu : le bord déchiqueté et moucheté de l'ombre sur les parois de tunnel a disparu (moucheture de la paroi d'`Expert-tunnel` 0,863 % -> 0,352 %, 0,363 % dans l'original) ; le dessus de la route est la famille `RoadSurface` (rugosité 0,30) sans lumière d'environnement (texture d'occlusion noire `NoEnvironmentLight`), ADR-0034 `Proposed` (`4132d0f`).
- Mesure contre l'original, 15 vues en `linear` : écart moyen par canal 15,73 (17,76 avant) ; route éclairée 83,8 pour 84,0 dans l'original (101,8 avant) ; boîte du reflet de `Beginner-sector-50` 189,3 pour 209,9 (110,5 avant) ; sondes 20 sur 34, 0 sur 4, 27 sur 33, 3 sur 3.
- Émissive du tunnel sur le moteur corrigé (mesure de R4) : B, en place, médiane 29,479 dans la fenêtre, écart 14,998 ; C 31,860 et 15,663 ; A 28,311 hors fenêtre et 16,248. Planches `artifacts/pbr-handover/s22/judge/2-*-tunnel-ABC-*.jpg`.
- Référence du port : `artifacts/pbr-handover/s22/s2/ref/` (15 vues et tunnel dans les deux modes, interface, audit). Scripts de mesure figés : `s22/scripts/speckle.py`, `measure_s2.py`.
- Laissés à l'auteur : O2 à O6 du plan (réglage B ou C du tunnel, deux phrases de l'ADR-0034 et l'ADR-0018, pénombre en paliers du filtre d'ombre, reflet encore sous l'original, validation de l'ADR-0034, avance de `main` et de `remaster`, push, moteur d'abord).
