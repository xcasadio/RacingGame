# Plans d'agent IA

Plans d'exécution des chantiers menés par des agents IA dans ce dépôt, rédigés selon [plan-template.md](plan-template.md).
Le plan du chantier précédent (mise à jour de CasaEngine) est resté dans [docs/racinggame-casaengine-update-tasks.md](../docs/racinggame-casaengine-update-tasks.md).

| Fichier | Sujet | Reste à faire |
|---|---|---|
| [tasks/rgce-xaml-screens-tasks.md](tasks/rgce-xaml-screens-tasks.md) | Écrans MGUI de RacingGameCasaEngine en assets `.uiscreen` liés à des view models | Exécuté (2026-10-05), validation globale et passe `verifier` CONFIRMED. Reste : vérifications manuelles de l'auteur (tâches 🧪 T2.1 → T4.2, T6.3) ; O4 réglé par `d95bd6f`, O6 traité par le plan suivant |
| [tasks/rgce-hud-shadows-start-tasks.md](tasks/rgce-hud-shadows-start-tasks.md) | HUD de course ×2, réglage des ombres, feu de départ animé et sons « Beep » / « Bleep » | Exécuté (2026-10-05), validation globale et passe `verifier` CONFIRMED. Reste : vérifications manuelles de l'auteur (tâches 🧪 T1.1 → T4.1), point ouvert O1 |
| [tasks/rgce-automation-settings-tasks.md](tasks/rgce-automation-settings-tasks.md) | Les modes automation de RGCE n'écrivent plus `display-settings.json` ni `front-end-options.json` ; enregistrement au seul bouton Retour d'Options | Exécuté (2026-10-05), validation globale et passe `verifier` CONFIRMED. Reste : vérifications manuelles de l'auteur (T1.1 🧪, remarque A1), point ouvert O1 |
| [tasks/rgce-main-menu-xna-look-tasks.md](tasks/rgce-main-menu-xna-look-tasks.md) | Menu principal au look du jeu XNA d'origine : bande, boutons en dégradés MGUI, sélection avec anneau, animation et libellé | Exécuté (2026-10-05), vérification indépendante (pixels et code CONFIRMED, doc corrigée). Reste : vérifications manuelles de l'auteur (T2.2, T3.1 🧪), P3/P4 reportés |
| [tasks/rgce-title-car-selection-xna-look-tasks.md](tasks/rgce-title-car-selection-xna-look-tasks.md) | Écran titre et sélection de voiture au look du jeu XNA d'origine : bande, Press START, carrousel 3D, barres, palette, police GameFont, sons de menu, peinture de la voiture | En cours |
