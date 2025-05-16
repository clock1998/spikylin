---
title : Linux Cheatsheet
description: Some useful linux commands and scripts
date: '2025-01-05'
tags: 
    - DevOps
    - Linux
published: true
featured: true
---

# Linux commands

## How to mount a drive
``` shellscript
qm unlock "vm id"
umount /mnt/pve/"mount name"

lsblk -f
/etc/fstab

UUID=063c75bc-bcc6-4fa5-8417-a7987a26dccb /mnt/backups ext4 defaults,noatime,nofail 0 2
mount -a
```

## How to install updates
``` shellscript
sudo apt update        # Fetches the list of available updates
sudo apt upgrade       # Installs some updates; does not remove packages
sudo apt full-upgrade  # Installs updates; may also remove some packages, if needed
sudo apt autoremove    # Removes any old packages that are no longer needed
```
## How to remove a directory with content
``` shellscript
rm -rf
```
## Set up git username and email
``` shellscript
git config --global user.name "FIRST_NAME LAST_NAME"
git config --global user.email "MY_NAME@example.com"
```
## How to resize ubuntu partition?

1. use live CD and resize in Gparted
2. enter the system. 
``` shellscript
sudo lvextend -l 100%VG ubuntu-vg/ubuntu-lv
sudo resize2fs /dev/mapper/ubuntu--vg-ubuntu--lv
```

## How to auto update ubuntu?

``` shellscript
sudo dpkg-reconfigure -plow unattended-upgrades
sudo systemctl restart unattended-upgrades
which unattended-upgrades
```