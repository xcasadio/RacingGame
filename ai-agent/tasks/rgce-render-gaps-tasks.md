# Plan agent IA — Écarts de rendu restants du portage PBR

Suite de `rgce-pbr-handover-tasks.md` (points ouverts O2, O10, O13, O14, O16, O17). Demande de l'auteur du 2026-11-17 : « fait toutes les tâches suivantes de manière autonome ». Mode AUTO : chaque tranche est relue avant exécution et vérifiée après par un vérificateur frais ; aucun push, aucune fusion dans `main`, `master` ou `remaster`.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Rapprocher le rendu du port de celui du jeu original (D11 du plan de reprise), mesuré par le banc de captures, sur les écarts restants : équilibre de lumière du monde de course, matériaux noirs (panneaux, vitres de la voiture), tunnels, carrousel. Supprimer l'outil `scripts/TrackPlacementExporter`. Brancher le jeu sur les deux évolutions du moteur autorisées (ordre de dessin déterministe, émissive ombrée).

## Réponses de l'auteur (2026-11-17)

| Réf | Réponse |
|---|---|
| A1 | O10 : « Mon but est de créer un projet pour l'éditeur CasaEngine. S'il est utile pour ça garde-le, sinon tu le supprimes. » |
| A2 | O11 (vecteur droite du maillage de route) : laisser. |
| A3 | O9 (cisaillement des supports) : laisser. |
| A4 | Évolutions du moteur autorisées : émissive ombrée, tri déterministe. Les corrections de défauts avérés du moteur restent permises. Aucune autre évolution du moteur. |
| A5 | Le contrôle sur l'autre PC est abandonné. |

## État vérifié (2026-11-17, jeu `db8a36c`, moteur `8a73c5329`)

Relevé par six enquêteurs en lecture seule ; rien n'a été lancé, donc toute grandeur ci-dessous marquée « prédite » est à mesurer. L'exécutant revérifie les lignes citées avant de modifier.

- **Outil d'export** : trois fichiers suivis (`Program.cs`, `RuntimeSceneComparison.cs`, `TrackPlacementExporter.csproj`), hors de `RacingGame.slnx` ; il lit `RacingGame/Content`, écrit dans `artifacts/track-placement/` (ignoré par git) ; aucun code, cuiseur, fichier du projet d'éditeur ni test ne lit ses sorties ni ne le nomme. Il consomme l'export `--export-track-runtime-scene` du jeu (`TrackRuntimeSceneExporter`), qui reste : `scripts/compare_race_scene_exports.py` s'en sert. Références actives : `rgce-editor-project-tasks.md:1662` et `:1740` (tranche S6 à venir), O10 ici (`rgce-pbr-handover-tasks.md`), O2 de `rgce-3d-geometry-fidelity-tasks.md:107`. Conclusion : inutile au projet d'éditeur, donc supprimé (A1).
- **Courbe de ton** : `RaceWorldFactory.ConfigureLinearColorPipeline` prend `AcesApprox` à l'exposition 1. Cette courbe (ajustement de Narkowicz sans le facteur 0,6, documentée ainsi dans le moteur : pas un défaut) a un pied de pente 0,214 et des tons moyens de pente 1,47 environ : les tons moyens montent, les ombres sont écrasées (la teinte d'ombre 0,25 donne un rapport d'affichage d'environ 0,14 au lieu de 0,26). Prédit sur les images `s3b/linear-a` : Reinhard à l'exposition 1,3 donne une luminance moyenne de 96,8 (original 96,3) et des rapports d'ombre du terrain d'environ 0,31 et 0,28 (original 0,284 et 0,267) ; ACES à 0,9 corrige la moyenne mais pas les rapports.
- **Ambiant** : en `LinearHdr`, le moteur décode la couleur d'ambiant (`ResolvedEnvironmentSettings.cs:106-111`) : 0,05 devient 0,0014. Le commentaire de `RaceWorldFactory.cs:48-52` et l'ADR-0031 parlent de « 5 % » : c'est faux en `linear` (vrai en `legacy`). Les faces dos au soleil ne reçoivent presque que le reflet du ciel, d'où leur teinte froide. L'original donne à chaque matériau un ambiant gris neutre (terrain A/D = 0,376 ; défaut 0,19 ; rail 0,40 ; ville 0,16). Piste : `AmbientSource.Flat` avec une couleur d'environ 0,28 à 0,31 (prédit, à mesurer).
- **Reflet du ciel** : le terme spéculaire d'environnement de `lit-pbr` n'est pas réduit par la couleur d'ambiant ; seul `SpecularIntensity` du monde le règle (1 aujourd'hui). L'original n'a aucun reflet d'environnement sur la route, le terrain, la ville ni le tunnel ; il en a sur la voiture et le verre. C'est la cause probable de la route blanc bleuté, du sol de ville et du dessous de route bleutés, du plafond de tunnel bleuté. Un facteur par matériau serait une évolution du moteur non autorisée (A4) : seul le réglage du monde est disponible, et il assombrit aussi les reflets de la voiture.
- **Référence de mesure** : la moyenne de 112,3 de O13 date d'avant `558a5d9` ; la référence actuelle `s3b/linear-a` a une moyenne de 104,0 (excès de 7,7), critère 1 à 16 sur 34, critère 2 à 0 sur 4 (`s3b/compare-linear-nopost.txt`).
- **Pavés de la ville de `Beginner-sector-20`** : ni le maillage, ni la normal map, ni les drapeaux d'ombre ne sont en cause. L'original projette ses ombres depuis une lumière ponctuelle virtuelle en perspective, de couverture étroite et atténuée sur les bords (`ShadowMapShader.cs:235-266`, `ShadowMap.fx:224-232`) ; le port a une ombre directionnelle. Reproduire cela demanderait une évolution du moteur non autorisée : écart classé non atteignable.
- **Panneaux à chevrons noirs** (`SignCurveLeft/Right`, tunnel de Beginner) : l'original les éclaire par l'ambiant du matériau, `texture × (0,3137 + relief × 0,835) × (0,85 + 0,75 × reflet)` ; `ForScenery` de `RaceMaterialRecipes.cs:73-90` ne reprend pas `AmbientColor` importé. Famille `Signage` : 18 matériaux.
- **Verre de la voiture noir** (`Car/Standard 19`, famille `CarGlass`) : la recette prend la couleur diffuse de l'original (13) comme couleur de base d'un métal ; pour un métal, la réflectance est la couleur de base, donc presque nulle. Le moteur fait ce qu'il doit. Le reflet de l'original vaut environ 0,75 en incidence normale. `BuildingGlass` (mur du casino, noir aussi dans l'original : 13 sur 255) ne doit pas changer.
- **Tunnel** : l'émissive (ADR-0030, 0,5) n'est jamais ombrée ; le reflet du ciel éclaire l'intérieur d'un tunnel fermé (plafond bleuté à reflets blancs) ; la recette du tunnel n'a pas de texture d'occlusion (`RaceMaterialRecipes.cs:260-274`).
- **Carrousel** : toit gris plus clair et froid (172/182/189 pour 129/128/130 avant le PBR) par le reflet du ciel, que le carrousel ne règle pas (`CarSelectionCarousel.cs:106-113`, `SpecularIntensity` 1) ; plateaux bleutés parce que la famille `CarouselPlate` est un métal (`RacingGamePbrMaterialMapping.cs:52`) alors que l'original en fait une surface diffuse texturée ; brillance des bandes disparue (peinture `CarPaint` uniforme, rugosité 0,5). Aucune capture de l'écran de sélection de l'original n'existe sur disque : la cible mesurable est l'écran d'avant le PBR (`artifacts/pbr-handover/s1m`, à localiser par l'exécutant ; s'il manque, arrêt de la tranche).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | La cible est le jeu original, mesuré par `scripts/TrackCaptureCompare` contre `artifacts/pbr-handover/s1c/ref` (15 vues) et `s3/ref-tunnel`, variante `nopost`, avec `--probes scripts/TrackCaptureCompare/probes.json --no-terrain-detail-correction`. Référence du port avant ce plan : `artifacts/pbr-handover/s3b/`. Sorties de ce plan : `artifacts/pbr-handover/s20/`. |
| D2 | Tout réglage est choisi par mesure, avec une règle écrite avant la mesure. L'exécutant mesure et rapporte ; la session principale choisit. Un réglage qui ne passe pas sa règle n'est pas appliqué : l'écart reste ouvert, avec ses chiffres. |
| D3 | Les candidats se mesurent avec un build temporaire qui lit des variables d'environnement (`RGCE_BENCH_*`), jamais committé ; le code est remis à l'identique avant tout commit (`git status` propre contrôlé). Les candidats de matériaux se mesurent sur une copie du dossier de build hors du dépôt, comme en O16. |
| D4 | Les décisions de réglage sont des ADR `Proposed` du jeu (0032 : équilibre de lumière du monde de course ; 0033 : matériaux, qui amende 0030 ; 0034 : carrousel, qui amende 0031). Aucune ADR existante n'est réécrite ; une ligne d'état renvoie à la nouvelle. Les décisions prises par l'auteur (ambiant 0,05 ; émissive du tunnel 0,5) ne sont remplacées que par une valeur mesurée avec le critère que l'auteur avait retenu, et le rapport de fin le dit en premier. |
| D5 | Pas de contournement dans le jeu d'un défaut du moteur. Les écarts qui demandent une évolution du moteur non autorisée (facteur de reflet par matériau, ombre en perspective de l'original, ambiant par matériau) sont notés et laissés. |
| D6 | Le mode `legacy` reste un mode de banc : ses images peuvent changer, elles sont mesurées et notées, sans critère d'arrêt propre, sauf le critère 3 des sondes (au moins 27 sur 33). |
| D7 | Le pointeur du sous-module avance sur `chantier/draw-order-emissive-shadow` du moteur. `main` du moteur, `master` et `remaster` du jeu ne sont pas avancés par ce plan, et rien n'est poussé. |

## Règles d'exécution pour l'agent

- Branche `chantier/rgce-pbr-handover` (suite du chantier). `git add` fichier par fichier ; jamais `.serena/`, `.codegraph/`, `log.txt` ; aucune commande git dans `CasaEngine/MGUI` ni `CasaEngine/NvgSharp` ; jamais de `git submodule update`, `checkout`, `switch`, `reset`. Commandes de commit commençant par `cd D:/...`, message par fichier et `git commit -F`.
- Un lancement du jeu à la fois, jamais pendant un build ; rebuild avant tout lancement après un changement de code ou une cuisson. Le jeu lit son contenu dans le dossier `Content` de l'exécutable.
- Builds à 0 erreur ; cuiseur `--verify` (0 écart après cuisson) et `--check` ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès, journaux sans `[Warning]` ni `[Error]`.
- Un commit par tâche, ce plan mis à jour dans le même commit ; messages, code, docs et ADR en anglais ; plan en français ; fins de ligne des fichiers existants conservées (pas de `sed -i`).
- Chiffres du doc de banc produits par script et relus par un second script.
- Retour arrière de toute tranche : `git revert` de son commit (le contenu cuit revient avec lui) ; les références sous `artifacts/pbr-handover/s20/` sont gardées.

## Légende des statuts

⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Tâches

### ⏳ R1 — Suppression de `scripts/TrackPlacementExporter`

- Fichiers : les trois fichiers suivis de l'outil (`git rm`) ; `ai-agent/tasks/rgce-editor-project-tasks.md` (`:1662` : retirer l'outil des arrêts de S6 ; `:1740` : remplacer la ligne d'acceptation par la mesure de placement du banc de captures, « objets appariés, 0 manquant, 0 en trop ») ; `ai-agent/tasks/rgce-3d-geometry-fidelity-tasks.md:107` (O2 clos) ; ce plan. Les documents d'historique qui nomment l'outil ne sont pas modifiés. `TrackRuntimeSceneExporter` et `scripts/compare_race_scene_exports.py` restent.
- Validation : `dotnet build RacingGame.slnx` à 0 erreur ; `rg -l TrackPlacementExporter` hors `artifacts/` ne rend que des fichiers d'historique, listés dans la note, et aucune ligne de tâche ⏳ ou 🚧 ; aucun fichier du jeu ni de son contenu ne change.
- Arrêts : un fichier hors des documents d'historique qui construit, lit ou nomme l'outil comme une étape vivante ; une erreur de build ; un changement sous `RacingGame*/` ou dans le contenu.
- Retour arrière : `git revert` du commit de R1.
- Commit : `chore(scripts): remove the track placement exporter`

### ⏳ R2 — Jeu sur le moteur de `chantier/draw-order-emissive-shadow`

- Prérequis : plan du moteur `CasaEngine/ai-agent/tasks/draw-order-emissive-shadow-tasks.md` T0 à T3 faits et confirmés.
- Étapes : pointeur du sous-module sur le sommet de cette branche ; build ; cuiseur ; fumée ; rejeux `linear` et `legacy` des 15 vues et des vues de tunnel, interface, audit, contre `s3b`.
- Validation : tout est identique octet pour octet à `s3b`, ou bien chaque fichier qui diffère est nommé, mesuré (pixels qui diffèrent, écart maximal), regardé et attribué à l'ordre de dessin d'éléments de clé égale ; dans ce cas deux rejeux par mode sont identiques entre eux et la nouvelle référence est `s20/ref-engine`. Arrêt : un écart qui ne s'explique pas par l'ordre de dessin, ou deux rejeux qui diffèrent.
- Commit : `chore(engine): point the CasaEngine submodule at the deterministic draw order and shadowed emissive`

### ⏳ R3 — Équilibre de lumière du monde de course (ADR-0032)

- Prérequis : R2 fait et confirmé. État de départ et candidat « actuel » : `s20/ref-engine` si R2 a pris une nouvelle référence, sinon `s3b` ; la « part de pixels à 255 d'aujourd'hui » se lit sur cet état.
- Mesure (D3), mode `linear`, 15 vues, en trois étapes ; à chaque étape la session principale choisit selon la règle, puis l'étape suivante part de ce choix :
  1. Courbe de ton : actuel (ACES 1,0) ; ACES 0,6 ; ACES 0,9 ; Reinhard 1,2 ; 1,3 ; 1,4.
  2. Ambiant : actuel (`Auto`, 0,05) ; `Flat` 0,25 ; 0,28 ; 0,31 ; 0,35. L'auteur avait choisi 0,05 sur la mesure de O13 (Phase 13 de `rgce-pbr-handover-tasks.md` : luminance moyenne contre 96,3, écart moyen par canal avec l'original, sondes) : ce sont les grandeurs de la règle de choix ci-dessous, qui tient donc lieu de son critère. Passer de `Auto` à `Flat` est un remplacement de sa décision au sens de D4 : il n'est fait que si la règle le retient, et le rapport de fin le dit en premier, avec les chiffres des deux états.
  3. Reflet du ciel (`SpecularIntensity`) : 1 ; 0,5 ; 0,25 ; puis une retouche d'exposition (± 0,1) si la moyenne sort de ± 3.
- Chiffres rapportés par candidat : luminance moyenne des 15 vues ; écart moyen par canal ; critères 1 à 4 des sondes ; rapports ombre sur lumière du terrain ; part des pixels à 255 ; sur `Beginner-start`, moyenne par canal de la boîte de la voiture (x 595-640, y 365-412, carrosserie seule : l'exécutant contrôle sur l'image de l'original et sur celle du port qu'elle ne contient que la voiture, et la resserre sinon, en notant les coordonnées retenues) contre l'original ; planche d'images des vues `Beginner-start`, `Beginner-sector-50`, `Expert-sector-20`, `Expert-sector-80`.
- Règle de choix, par étape : parmi les candidats qui gardent le critère 3 à au moins 27 sur 33 et dont la part de pixels à 255 ne dépasse pas celle d'aujourd'hui de plus de 0,5 point, prendre celui dont l'écart moyen par canal avec l'original (moyenne des trois canaux, 15 vues) est le plus bas ; à moins de 0,3 d'écart entre deux candidats, prendre celui qui passe le plus de sondes (critères 1 et 2), puis celui qui vient en premier dans la liste de l'étape. À l'étape 3, un candidat dont la boîte de la voiture (celle ci-dessus, carrosserie seule) est plus loin de l'original que celle du candidat `SpecularIntensity` 1, de plus de 5 sur un canal, est écarté. Si aucun candidat ne fait mieux que l'actuel de plus de 0,3, l'étape ne change rien.
- Application : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` (valeurs en constantes nommées ; commentaire corrigé : la couleur d'ambiant est décodée en `linear`) ; ADR-0032 `Proposed`, avec le tableau des mesures ; ligne d'état sur les ADR qu'elle amende ; `docs/racinggame-3d-capture-bench-local-baseline.md` (section « After the race world light balance ») ; ce plan (O13 corrigé : référence 104,0).
- Validation après application : rejeux `linear` identiques octet pour octet au candidat retenu ; deux rejeux par mode identiques entre eux ; critère 3 au moins 27 sur 33 dans les deux modes ; vues de tunnel rejouées et mesurées (luminance, médiane de la bande haute, lignes 0 à 251) : notées, la remise au niveau du tunnel est en R4 ; interface et audit rejoués, écarts mesurés et attribués ; nouvelles références sous `s20/r3/`.
- Arrêts : build temporaire non remis à l'identique ; critère 3 sous 27 après application.
- Commit : `fix(racing-casa): balance the race world light against the original`

### ⏳ R4 — Matériaux : vitres de la voiture, panneaux, tunnel (ADR-0033)

- Prérequis : R3 fait et confirmé (état de départ : `s20/r3/`) ; `emissive_shadow_factor` disponible dans le moteur depuis R2.
- Mesure (D3) :
  1. Vitres (`CarGlass`) : couleur de base candidate 224 gris (réflectance linéaire d'environ 0,75), opaque. Boîte de mesure : la lunette arrière seule sur `Beginner-start`. L'exécutant la localise sur l'image de l'original et sur celle du port (la voiture occupe environ x 587-648, y 358-420), contrôle qu'elle ne contient que du verre sur les deux, et note ses coordonnées ; s'il n'existe pas de boîte de verre seul d'au moins 40 pixels commune aux deux images, l'étape s'arrête et rien n'est appliqué. Grandeur : écart par canal entre les moyennes de la boîte (port moins original), résumé par la moyenne des trois valeurs absolues. Règle : appliqué si cette grandeur baisse par rapport à l'état de départ et si `BuildingGlass` ne change pas (sonde `building-away-1` de `Beginner-overview` inchangée). Le pare-brise de l'écran de sélection est seulement rapporté, contre `s1m`, sans entrer dans la règle.
  2. Panneaux (`Signage`) : d'abord mesurer la région des chevrons du tunnel de Beginner (x 770-850, y 140-220) après R3. Si le pixel le plus rouge et le pixel le plus clair sont à ± 15 par canal de l'original, rien n'est fait. Sinon : émissive = texture de base × `AmbientColor` importé × niveau, niveaux candidats 0,5 ; 0,75 ; 1,0 ; 1,3 ; règle : le plus bas niveau qui met ces deux pixels à ± 15, sans faire monter l'écart moyen par canal des 15 vues de plus de 0,1 ; si aucun, le niveau qui minimise l'écart de la région, sous la même contrainte, sinon rien.
  3. Tunnel, en deux temps, mesurés sur les trois vues de tunnel contre `s3/ref-tunnel`, avec l'ambiant retenu en R3.
     - Occlusion : sans, puis avec une texture d'occlusion constante à 0 sur le matériau du tunnel, qui retire le reflet du ciel et aussi l'ambiant diffus du tunnel. Gardée si l'écart moyen par canal des trois vues baisse et si la part de pixels de luminance supérieure à 150 dans la bande haute (lignes 0 à 251) se rapproche de celle de l'original ; sinon abandonnée.
     - Émissive ombrée : couples (`emissive_shadow_factor`, niveau de l'émissive) avec facteur 0,25 ; 0,5 ; 0,75 ; 1 et niveau 0,5 à 1,0 par pas de 0,1 (l'exécutant vérifie d'abord si `emissive_color` accepte une valeur au-dessus de 1 ; si oui, les niveaux 1,5 et 2,0 s'ajoutent pour le facteur 1, dont l'effet dans l'ombre est d'environ × 0,26). Critère de l'auteur en O16 : médiane de la bande haute égale à celle de l'original, 30,3, à ± 1,5. Règle : parmi les couples qui tiennent ce critère, celui dont l'écart moyen par canal des trois vues est le plus bas, s'il est plus bas que celui de l'état de départ (facteur 0, niveau 0,5) d'au moins 0,3. Si aucun couple ne passe : le facteur reste 0 et le niveau 0,5 de l'auteur ne change pas ; les chiffres sont notés et l'écart reste ouvert.
- Application : `RacingGame.Gameplay/Import/RaceMaterialRecipes.cs`, `RaceMaterialRecipe.cs`, `RacingGamePbrMaterialMapping.cs` ; `scripts/RgceAssetCooker` (`Cooker.cs`, `CookPlan.cs`, `Verifier.cs` : les clés `emissive_shadow_factor` et de texture d'occlusion ne sont écrites que si la recette les demande, comme `emissive_texture`) ; si l'occlusion est gardée : champ de recette pour la texture d'occlusion, texture constante source (1 × 1, noire) avec son asset cuit et son entrée de catalogue, nom exact de la clé de `lit-pbr` relevé dans le moteur (`BuiltInMaterialDefinitions.cs`) ; ni le jeu ni le cuiseur ne connaissent l'occlusion aujourd'hui ; matériaux recuits ; `LegacyImportProfileVerifier` si une valeur y est épinglée ; ADR-0033 `Proposed` ; ligne d'état de l'ADR-0030 ; doc de banc ; `probes.json` (note sur les sondes de la ville de `Beginner-sector-20` : non atteignables, ombre en perspective de l'original).
- Validation : cuiseur `--verify` avant (écarts sur les seuls matériaux visés, listés) et après (0), `--check`, second passage sans écriture ; `git status` du contenu limité aux matériaux visés ; rejeux identiques aux candidats retenus ; deux rejeux par mode identiques ; critère 3 au moins 27 sur 33 ; écart moyen par canal des 15 vues pas plus haut qu'après R3 de plus de 0,1 ; interface et audit rejoués, écarts attribués ; références sous `s20/r4/`.
- Commit : `fix(racing-casa): light the signs and car glass and shadow the tunnel emissive as the original`

### ⏳ R5 — Carrousel (ADR-0034)

- Prérequis : R4 fait et confirmé (état de départ : `s20/r4/`).
- Mesure sur `--capture-ui-screens`, écran de sélection, contre l'écran d'avant le PBR (`artifacts/pbr-handover/s1m/ui/ui-car-selection.png`) :
  1. Plateau : famille `CarouselPlate` métal 0, rugosité 0,3 ou 0,45. Règle : boîtes du dessus du plateau (x 760-800 et x 1150-1190, y 600-630, captures en 1920 × 1080) avec |B − R| ≤ 12 et luminance à ± 12 de `s1m` ; si les deux rugosités passent, celle dont la somme des écarts de luminance à `s1m` sur les deux boîtes est la plus petite ; si aucune ne passe, celle-là aussi, pourvu qu'elle fasse mieux que l'état actuel sur les deux boîtes, sinon rien.
  2. `SpecularIntensity` du monde du carrousel : 1 ; 0,25 ; 0,15 ; 0,1. Boîte du toit : x 905-925, y 470-490 (zone grise entre les bandes ; valeur de `s1m` relevée par l'enquête : 97,0 / 94,5 / 94,8 ; l'exécutant la remesure sur `s1m` et contrôle sur les deux captures que la boîte ne touche aucune bande, sinon il la déplace et note les coordonnées). Règle : boîte du toit à ± 10 par canal de la valeur de `s1m` et |B − R| ≤ 5 ; à défaut, la valeur dont le plus grand écart par canal à `s1m` est le plus petit.
  3. Peinture (`CarPaint`) : rugosité 0,42 et rugosité du vernis 0,15, seulement si le 99e centile de luminance des bandes orange (x 940-960 et x 1010-1030, y 600-650) monte sans que la boîte du toit sorte de sa règle et sans que la boîte de la voiture de `Beginner-start` (celle de R3, carrosserie seule) s'éloigne de l'original de plus de 5 sur un canal ; sinon laissé, et noté.
- Application : `RacingGameCasaEngine/UI/CarSelectionCarousel.cs`, recettes et matériaux recuits ; ADR-0034 `Proposed` ; ligne d'état de l'ADR-0031 ; doc de banc.
- Validation : les neuf autres écrans d'interface identiques à la référence de R4 sauf ceux qui montrent la voiture, mesurés et attribués ; 15 vues et tunnel identiques à R4 si la peinture ne change pas, sinon mesurés ; aucun aplat à 255 (moins de 2 % des pixels d'une boîte) ; références sous `s20/r5/`.
- Commit : `fix(racing-casa): bring the carousel plate and reflections toward the original`

### ⏳ R6 — Vérification indépendante et clôture

- Vérification en volets frais : lancements (builds, cuiseur, fumée, rejeux contre `s20`), relecture des ADR et du doc contre les images et les mesures, regard sur les planches contre l'original.
- `ai-agent/HANDOVER.md` (section 11), points ouverts de `rgce-pbr-handover-tasks.md` mis à jour (O2, O10, O13, O14, O16, O17).
- Commit : `docs(ai-agent): record the verification of the remaining render gaps`

## Points ouverts

- O1 : évolutions du moteur qui lèveraient les écarts laissés (D5) : facteur de reflet d'environnement par matériau ; ombre projetée en perspective comme l'original ; ambiant par matériau. À décider par l'auteur.
- O2 : avance de `main` du moteur et de `remaster` du jeu, push : à l'auteur.
- O3 : validation des ADR 0032 à 0034 du jeu, 0125 et 0126 du moteur.
