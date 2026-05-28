---
title: Nouveau routeur pour mon home lab
description: Matériel
date: '2025-03-06'
tags:
  - HomeLab
  - DevOps
published: true
featured: true
---

Le routeur que j'utilisais était un Netgear R7000. Il fournit un bon Wi‑Fi et il est très stable, mais les fonctions DHCP et DNS sont boguées. Comme je n'ai pas d'onduleur, à chaque coupure de courant, tout mon équipement de home lab s'éteint. Bien que j'aie des IP statiques pour mes serveurs, elles ne sont pas conservées après le redémarrage du routeur. Le routeur offre aussi des options limitées pour le DNS.

Netgear R7000
<img src="/images/post_images/my-home-lab-2/2.jpg" alt="Netgear R7000" width="400"/>

J'ai décidé d'acheter un vrai routeur et de convertir mon ancien routeur en point d'accès.

MikroTik RB5009

<img src="/images/post_images/my-home-lab-2/1.jpg" alt="Netgear R7000" width="400"/>

J'ai suivi [An introduction to MIkroTick RouterOS for Newbies](https://www.youtube.com/watch?v=rwjtRLQjMjA&t=1038s) pour configurer mon routeur. RouterOS a effectivement une courbe d'apprentissage abrupte, mais cela vaut vraiment le temps investi. Je ne pense pas que ce soit très difficile à apprendre pour les personnes qui ont quelques bases en réseau.

Je créerai une note d'apprentissage sur RouterOS.

```