---
title : Docker Cheatsheet
description: Some useful docker commands and scripts
date: '2025-01-05'
tags: 
    - DevOps
    - Docker
published: true
featured: true
---
# Docker Cheat Sheet

## clear build cache
``` shellscript
docker builder prune -f
```

## Reset docker
``` shellscript
docker system prune -a

docker stop $(docker ps -a -q)

docker rm $(docker ps -a -q)
``` 

## How to show all build logs
``` shellscript
docker build --no-cache --progress=plain .
```

## How to view container

## How to check a docker container log
``` shellscript
docker logs --since=1h <container_id>
```