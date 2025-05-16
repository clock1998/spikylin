---
title : New Router for my Home Lab
description: Hardware
date: '2025-03-06'
tags: 
    - HomeLab
    - DevOps
published: true
featured: true
---

The router I was using was a Netgear R7000. It provides good Wifi and is very stable, however, the DHCP and DNS functions are bugged. Since I do not have an UPS, everytime there is a power outage, all my home lab equipments will go down. Although I have static IPs for my servers, they are not preserved after the router rebooted. The router provides limited configurations for DNS.

Netgear R7000
<img src="/post_images/my-home-lab-2/2.jpg" alt="Netgear R7000" width="400"/>

I have decided to buy a proper router, and convert my older router as AP.

MikroTik RB5009

<img src="/post_images/my-home-lab-2/1.jpg" alt="Netgear R7000" width="400"/>

I haved followered [An introduction to MIkroTick RouterOS for Newbies](https://www.youtube.com/watch?v=rwjtRLQjMjA&t=1038s) to set up my router. The RouterOS does have a steep learning curve, but it is well worth the time to learn it. I do not think it is very hard to learn for people who have some basic network knowledge.

I will create a learning note for RouterOS.