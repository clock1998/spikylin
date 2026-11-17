---
title: My Home Lab v5
description: Infrastructure
date: '2026-11-16'
tags:
  - HomeLab
  - DevOps
published: true
featured: true
---

<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/1.jpg" alt="Dell Precision Rack" width="400"/>

### Hardware changes

- A new Aruba JL255A switch
- A new server case with a proper hard drive cage (still shopping for the hardware)
- Replaced the fan in the Eaton UPS to reduce noise levels

### Other changes

- Added acoustic panels to reduce noise levels

#### Acoustic panels

I used 1x4 lumber to build the panels, but the boards were not perfectly straight. If I build more, I will use plywood or MDF instead. They are straighter, but require more work to cut to size. For the internal membrane and the outer fabric, I used a geotextile barrier and a drop cloth. I stapled them to the wood using a Meite MT7116 pneumatic upholstery stapler. I would not recommend the manual staple gun, as it jams and is finicky. It also required substantial force, especially as the fabric got thicker. The soundproofing material is Rockwool Sound and Safe. Try to find leftovers on the marketplace to save some money.

<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/2.jpg" alt="Acoustic panels" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/3.jpg" alt="Acoustic panels" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/4.jpg" alt="Acoustic panels" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/5.jpg" alt="Acoustic panels" width="400"/>
<img src="https://spikylin-s3.spikylin.com/blog-images/homelab/my-home-lab-5/6.jpg" alt="Acoustic panels" width="400"/>

## Network Layout (To be updated)

The server uses two physical NICs:

| Interface | Network | Purpose |
|-----------|---------|---------|
| `eth0` | `192.168.1.0/24` | Trusted home LAN, management, SMB, normal administration |
| `eth1` | `10.10.10.0/24` | Self-hosted apps and isolated VM/application network |

### Network Policy (To be updated)

- `192.168.1.0/24` can initiate connections to `10.10.10.0/24`.
- `10.10.10.0/24` cannot initiate connections to `192.168.1.0/24`.
- Return traffic is allowed for connections started from `192.168.1.0/24`.
- Traffic forwarded to `10.10.10.2` is explicitly dropped to protect the Unraid management portal on the application network.

### High-Level Topology (To be updated)

```text
                    Internet
                       |
                 MikroTik Router
                       |
        +--------------+--------------+
        |                             |
  192.168.1.0/24                10.10.10.0/24
   Trusted LAN                  App / VM Network
        |                             |
        |                             |
    Unraid eth0                   Unraid eth1
        |                             |
        |                     Public-facing VM(s)
        |                     GitHub runner VM
        |
  Nginx Proxy Manager
     192.168.1.4
```