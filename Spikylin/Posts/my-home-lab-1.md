---
title : My Home Lab v1
description: Hardware
date: '2024-12-22'
tags: 
    - HomeLab
    - DevOps
published: true
featured: true
---

![home lab](/post_images/my-home-lab2.jpg)
![home lab](/post_images/my-home-lab.jpg "Example")

This is the version 1 of my home lab. The hardware I use is an old gaming computer that I built back in 2016. It has a I7-6800k cpu and 32 GB of memory. To better manage and utilize the hardware, I went with a hypervisor. My choice was Proxmox, a very popular OS in the home lab community. It is free, easy to use, and powerful. 

## Infrastructure

My current infrastructure looks this. It is nothing complicated but fits my need perfectly. Everything is virtualized using Proxmox.
![architecture](/architecture.svg "Architecture")

## Future plan

### Infrastructure

I plan to add an dedicated NAS to my home lab. There are two options: TrueNas or Synology. If I decide to go with TrueNAS, I will need to build a dedicated machine and requires more fiddling. The Synology will provide the best user experience out of box, and requires minimum maintenance. 
A NAS will let me have more storage for backup and private cloud. Lastly, I plan to add an UPS to the system to add more reliability. 

### Services

I plan to add more services:
- languagetool
- joplin
- nextcloud
- Terraform
- Ansible