---
title: Comment ai-je configuré mon blog ?
description: Technologie, configuration...
date: '2024-12-18'
tags:
  - SvelteKit
published: true
featured: false
---

## Technologie

J'ai choisi SvelteKit avec sa fonctionnalité de générateur de site statique comme framework pour construire mon blog. Pour améliorer le design et l'expérience utilisateur, j'ai intégré TailwindCSS et DaisyUI afin d'obtenir une interface élégante et réactive.

Pour le déploiement, j'ai conteneurisé l'application avec Docker et publié l'image Docker sur GitHub Packages. Pour fluidifier le processus CI/CD, j'ai utilisé Microsoft Azure DevOps, un outil que j'utilise souvent dans mon travail. L'instance en ligne du blog tourne comme conteneur Docker dans mon home lab, et j'ai utilisé Cloudflare Tunnel pour l'exposer de façon sécurisée à Internet.

Vous pouvez consulter le projet sur [GitHub](https://github.com/clock1998/spikylin-blog).

Vous pouvez également consulter le fichier YAML dans mon dépôt [GitHub](https://github.com/clock1998/spikylin-blog/blob/main/azure-pipelines.yml) pour le pipeline de build.

```