---
title: Notes d'apprentissage RouterOS
description: Notes d'apprentissage sur RouterOS
date: '2025-03-07'
tags:
  - HomeLab
  - RouterOS
published: true
featured: true
---

## Qu'est-ce qu'un bridge ?

Un bridge fonctionne comme un switch. Il y a plusieurs ports sur un routeur et, en général, chaque port possède un réseau différent. Avec un bridge, on peut placer plusieurs ports sur le même réseau et les faire fonctionner comme un switch.

## Comment configurer un DNS personnalisé ?

1. Si le routeur reçoit l'adresse IP publique via DHCP, allez dans le client DHCP et décochez `Use DNS Peer`. Désactivez le réseau puis réactivez l'interface du client DHCP pour que le changement soit pris en compte.

<img src="/images/post_images/routeros-learning-note/dhcp-client.png" alt="DNS1" width="400"/>

2. Configurez un DNS personnalisé et cochez `allow remote request` (pour permettre au routeur d'accepter et de transférer les requêtes DNS). J'ai un serveur DNS qui tourne sur 192.168.1.3

<img src="/images/post_images/routeros-learning-note/dns.png" alt="" width="400"/>

3. Configurez le DNS personnalisé dans le serveur DHCP. Le DNS doit être l'adresse du routeur. Dans ce cas, tous les appareils recevront 192.168.1.1 comme serveur DNS.

<img src="/images/post_images/routeros-learning-note/dhcp-dns.png" alt="" width="400"/>

4. Libérez puis renouvelez l'adresse IP sur les appareils.

## Comment configurer WireGuard ?

1. Configurez l'interface WireGuard.
2. Configurez l'adresse IP pour l'interface WireGuard.
3. Ajoutez des peers WireGuard. L'adresse autorisée (`Allowed Address`) doit être un sous-réseau de l'adresse WireGuard. Par exemple, si WireGuard a l'adresse 10.0.0.1/24, l'adresse autorisée du peer doit être 10.1.1.2/32
<img src="/images/post_images/routeros-learning-note/wireguard1.png" alt="" width="400"/>

4. Utilisez un client WireGuard pour générer une clé publique et collez-la dans le peer WireGuard.
<img src="/images/post_images/routeros-learning-note/wireguard1.png" alt="" width="400"/>

5. Renseignez les informations du tunnel WireGuard :

    [Interface]

    PrivateKey = generated 

    Address = 10.0.0.2/32 Allowed Address in wireguard peer

    DNS = 10.0.0.1 DNS Address.

    [Peer]

    PublicKey = wireguard server public key

    AllowedIPs = 0.0.0.0/0

    Endpoint = domain or ip plus wireguard server port number

6. Ajoutez une règle `srcnat` dans Winbox :
    Allez dans IP → Firewall → NAT.
    Ajoutez une nouvelle règle :
    Chain: srcnat
    Src. Address: 10.0.0.0/24 (votre sous-réseau WireGuard).
    Dst. Address: 192.168.1.0/24 (votre sous-réseau LAN).
    Out. Interface: bridge (ou le nom de votre bridge LAN).
    Allez dans l'onglet Action.
    Choisissez masquerade.
    Appliquez puis validez.

## Comment faire du port forwarding ?

1. Ajoutez une nouvelle règle dst-nat
2. Ajoutez le port de destination, le protocole du port et l'interface d'entrée.
3. Passez dans Action, définissez l'action sur dst-nat, puis ajoutez les adresses et ports de destination.
4. Ajoutez un Hairpin NAT pour accéder à un serveur du réseau local via l'IP publique et le port.
5. Ajoutez Src. Address (192.168.1.0/24) et Dst. Address (192.168.1.1). Ajoutez le protocole. Choisissez masquerade comme action.

```