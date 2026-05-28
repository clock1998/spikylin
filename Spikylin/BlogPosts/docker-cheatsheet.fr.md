---
title: Aide-mémoire Docker
description: Quelques commandes et scripts Docker utiles
date: '2025-01-05'
tags:
  - DevOps
  - Docker
published: true
featured: true
---
# Aide-mémoire Docker

## Vider le cache de build
``` shellscript
docker builder prune -f
```

## Réinitialiser Docker
``` shellscript
docker system prune -a

docker stop $(docker ps -a -q)

docker rm $(docker ps -a -q)
``` 

## Comment afficher tous les journaux de build
``` shellscript
docker build --no-cache --progress=plain .
```

## Comment afficher les conteneurs

## Comment consulter les journaux d'un conteneur Docker
``` shellscript
docker logs --since=1h <container_id>
```

```