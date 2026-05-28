---
title: Comment ajouter des certificats à Proxmox pour un accès HTTPS ?
description: Étapes pour configurer les certificats HTTPS sur Proxmox
date: '2025-02-15'
tags:
  - DevOps
  - Proxmox
published: true
featured: false
---
# Prérequis

- Nom de domaine
- Serveur DNS

# Étapes

1. Allez dans Datacenter, puis cliquez sur ACME.
2. Ajoutez un plugin de challenge.
3. Choisissez votre fournisseur DNS et ajoutez votre clé API si nécessaire.
4. Ajoutez un compte.
5. Ajoutez le certificat à chaque nœud.
![Node](/images/post_images/how-to-setup-certificate-for-your-proxmox/add-certificate-to-node.png)
![Add Domain](/images/post_images/how-to-setup-certificate-for-your-proxmox/add-domain.png)

```