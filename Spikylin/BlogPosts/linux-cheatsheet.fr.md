---
title: Aide-mémoire Linux
description: Quelques commandes et scripts Linux utiles
date: '2025-01-05'
tags:
  - DevOps
  - Linux
published: true
featured: true
---

# Commandes Linux

## Comment monter un disque
``` shellscript
qm unlock "vm id"
umount /mnt/pve/"mount name"

lsblk -f
/etc/fstab

UUID=063c75bc-bcc6-4fa5-8417-a7987a26dccb /mnt/backups ext4 defaults,noatime,nofail 0 2
mount -a
```

## Comment installer les mises à jour
``` shellscript
sudo apt update        # Récupère la liste des mises à jour disponibles
sudo apt upgrade       # Installe certaines mises à jour sans supprimer de paquets
sudo apt full-upgrade  # Installe les mises à jour et peut supprimer certains paquets si nécessaire
sudo apt autoremove    # Supprime les anciens paquets devenus inutiles
```
## Comment supprimer un dossier avec son contenu
``` shellscript
rm -rf
```
## Configurer le nom d'utilisateur et l'email Git
``` shellscript
git config --global user.name "FIRST_NAME LAST_NAME"
git config --global user.email "MY_NAME@example.com"
```
## Comment redimensionner une partition Ubuntu ?

1. Utiliser un live CD et redimensionner avec GParted
2. Entrer dans le système.
``` shellscript
sudo lvextend -l 100%VG ubuntu-vg/ubuntu-lv
sudo resize2fs /dev/mapper/ubuntu--vg-ubuntu--lv
```

## Comment activer les mises à jour automatiques d'Ubuntu ?

``` shellscript
sudo dpkg-reconfigure -plow unattended-upgrades
sudo systemctl restart unattended-upgrades
which unattended-upgrades
```

## Comment générer une clé SSH ? (MacOS)
``` shellscript
ssh-keygen -o -a 100 -t ed25519 -f ./.ssh/homeserver -C example@gmail.com
```

## Comment envoyer une clé SSH vers un serveur distant ? (MacOS)
``` shellscript
ssh-copy-id -i ./.ssh/homeserver username@192.168.1.4
```

## Comment tester la connexion à un hôte Ansible
``` shellscript
ansible all -m ping --ask-vault-pass
```

## Comment exécuter un playbook Ansible ?
``` shellscript
ansible-playbook run.yml -K --ask-vault-pass
```

```