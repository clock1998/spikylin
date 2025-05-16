---
title : How to add certificates to proxmox for HTTPS access?
description: To do items in the future
date: '2025-02-15'
tags: 
    - DevOps
    - Proxmox
published: true
featured: false
---
# Prerequists

- Domain Name
- DNS server

# Steps

1. Go to Datacenter, click ACME.
2. Add a Challenge Plugins.
3. Choose your DNS and put your API key if it is needed.
4. Add an Account
5. Add certificate to each node.
![Node](/post_images/how-to-setup-certificate-for-your-proxmox/add-certificate-to-node.png)
![Add Domain](/post_images/how-to-setup-certificate-for-your-proxmox/add-domain.png)