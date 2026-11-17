---
title: Mon Home Lab v5
description: Infrastructure
date: '2026-11-16'
tags:
  - HomeLab
  - DevOps
published: true
featured: true
---

<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/1.jpg" alt="Dell Precision Rack" width="400"/>

### Changements matériels

- Un nouveau commutateur Aruba JL255A
- Un nouveau boîtier de serveur avec un cage à disque dur appropriée (je cherche encore le matériel)
- Remplacement du ventilateur dans l'onduleur Eaton pour réduire les niveaux de bruit

### Autres changements

- Ajout de panneaux acoustiques pour réduire les niveaux de bruit

#### Panneaux acoustiques

J'ai utilisé du bois 1x4 pour construire les panneaux, mais les planches n'étaient pas parfaitement droites. Si j'en construis d'autres, j'utiliserai du contreplaqué ou du MDF à la place. Ils sont plus droits, mais nécessitent plus de travail pour être coupés à la bonne taille. Pour la membrane interne et le tissu extérieur, j'ai utilisé une barrière géotextile et un drap de protection. Je les ai agrafés au bois à l'aide d'une agrafeuse pneumatique pour tapisserie Meite MT7116. Je ne recommanderais pas la cloueuse manuelle, car elle se bloque et est capricieuse. Elle demandait également une force substantielle, surtout lorsque le tissu devenait plus épais. Le matériau d'insonorisation est le Rockwool Sound and Safe. Essayez de trouver des restes sur le marché pour économiser un peu d'argent.

<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/2.jpg" alt="Panneaux acoustiques" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/3.jpg" alt="Panneaux acoustiques" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/4.jpg" alt="Panneaux acoustiques" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/5.jpg" alt="Panneaux acoustiques" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/6.jpg" alt="Panneaux acoustiques" width="400"/>

## Configuration réseau (À mettre à jour)

Le serveur utilise deux cartes réseau (NIC) physiques :

| Interface | Réseau | Objectif |
|-----------|---------|---------|
| `eth0` | `192.168.1.0/24` | LAN domestique de confiance, gestion, SMB, administration normale |
| `eth1` | `10.10.10.0/24` | Applications auto-hébergées et réseau isolé pour VM/applications |

### Politique réseau (À mettre à jour)

- `192.168.1.0/24` peut initier des connexions vers `10.10.10.0/24`.
- `10.10.10.0/24` ne peut pas initier de connexions vers `192.168.1.0/24`.
- Le trafic de retour est autorisé pour les connexions démarrées depuis `192.168.1.0/24`.
- Le trafic acheminé vers `10.10.10.2` est explicitement rejeté pour protéger le portail de gestion Unraid sur le réseau d'applications.

### Topologie de haut niveau (À mettre à jour)

```text
                    Internet
                       |
                 Routeur MikroTik
                       |
        +--------------+--------------+
        |                             |
  192.168.1.0/24                10.10.10.0/24
   LAN de confiance             Réseau App / VM
        |                             |
        |                             |
    Unraid eth0                   Unraid eth1
        |                             |
        |                     VM(s) exposées au public
        |                     VM de exécution GitHub
        |
  Nginx Proxy Manager
     192.168.1.4
```
