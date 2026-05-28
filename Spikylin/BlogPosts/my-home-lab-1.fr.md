---
title: Mon home lab v1
description: Matériel
date: '2024-12-22'
tags:
  - HomeLab
  - DevOps
published: true
featured: true
---

![home lab](/images/post_images/my-home-lab2.jpg)
![home lab](/images/post_images/my-home-lab.jpg "Example")

Voici la version 1 de mon home lab. Le matériel que j'utilise est un ancien PC gaming que j'ai monté en 2016. Il dispose d'un processeur i7-6800k et de 32 Go de mémoire. Pour mieux gérer et exploiter ce matériel, j'ai opté pour un hyperviseur. Mon choix s'est porté sur Proxmox, un système d'exploitation très populaire dans la communauté home lab. Il est gratuit, facile à utiliser et puissant.

## Infrastructure

Mon infrastructure actuelle ressemble à ceci. Ce n'est pas très compliqué, mais cela correspond parfaitement à mes besoins. Tout est virtualisé avec Proxmox.
![architecture](/images/post_images/architecture.svg "Architecture")

## Plan futur

### Infrastructure

Je prévois d'ajouter un NAS dédié à mon home lab. Deux options s'offrent à moi : TrueNAS ou Synology. Si je choisis TrueNAS, je devrai construire une machine dédiée et cela demandera plus de configuration. Synology offrira une meilleure expérience utilisateur dès le départ et nécessitera un minimum de maintenance.
Un NAS me permettra d'avoir plus d'espace de stockage pour les sauvegardes et un cloud privé. Enfin, je prévois d'ajouter un onduleur au système pour améliorer la fiabilité.

### Services

Je prévois d'ajouter davantage de services :
- languagetool
- joplin
- nextcloud
- Terraform
- Ansible

```