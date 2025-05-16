---
title : RouterOS Learning Notes
description: Router OS learning notes
date: '2025-03-07'
tags: 
    - HomeLab
    - RouterOS
published: true
featured: true
---

## What is a bridge?

A bridge is like a switch. There are serveral ports on a router and usually each port has a different network. With bridge, we can put serveral ports in the same network and act like a switch.

## How to set up custom DNS?

1. If the router gets the public IP using DHCP, go to the DHCP client and uncheck the Use DNS Peer. Disable the network and re-enable the DHCP client interface to make the change in effect.

<img src="/post_images/routeros-learning-note/dhcp-client.png" alt="DNS1" width="400"/>

2. Set up custom DNS and check allow remote request (to let the router accept and forward DNS request). I have a DNS server running on 192.168.1.3

<img src="/post_images/routeros-learning-note/dns.png" alt="" width="400"/>

3. Set up customer DNS in the dhcp server. The DNS should be the router's address. In this case, all the devices will get 192.168.1.1 as their DNS server.

<img src="/post_images/routeros-learning-note/dhcp-dns.png" alt="" width="400"/>

4. Release and renew the IP on the devices.

## How to set up wireguard?

1. Set up Wireguard interface.
2. Set up IP Address for the wireguard interface
3. Add wireguard peers. The Allowed Address needs to be a subnet of the Wireguard address. For example, if the Wireguard has an address of 10.0.0.1/24, the Allowed Address of the peer should be 10.1.1.2/32
<img src="/post_images/routeros-learning-note/wireguard1.png" alt="" width="400"/>

4. Use a Wireguard client to generate a public key and copy past that to the wireguard peer.
<img src="/post_images/routeros-learning-note/wireguard1.png" alt="" width="400"/>

5. Fill out wireguard tunnel info:

    [Interface]

    PrivateKey = generated 

    Address = 10.0.0.2/32 Allowed Address in wireguard peer

    DNS = 10.0.0.1 DNS Address.

    [Peer]

    PublicKey = wireguard server public key

    AllowedIPs = 0.0.0.0/0

    Endpoint = domain or ip plus wireguard server port number

6. Add a srcnat rule in Winbox:
    Go to IP → Firewall → NAT.
    Add a new rule:
    Chain: srcnat
    Src. Address: 10.0.0.0/24 (your WireGuard subnet).
    Dst. Address: 192.168.1.0/24 (your LAN subnet).
    Out. Interface: bridge (or whatever your LAN bridge is called).
    Go to the Action tab.
    Choose masquerade.
    Apply and OK.