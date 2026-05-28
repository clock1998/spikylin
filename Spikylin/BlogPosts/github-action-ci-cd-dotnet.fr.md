---
title: 'Un pipeline CI/CD pour un projet .NET avec GitHub Actions'
description: Une note expliquant un pipeline CI/CD GitHub Actions pour un projet ASP.NET
date: '2026-01-11'
tags:
  - DevOps
  - Github Action
  - Pipeline
published: true
featured: false
---

Le [pipeline](https://github.com/clock1998/spikylin/blob/master/.github/workflows/pipeline.yml) construit une image Docker et la pousse vers ghcr.io. Il déploie également le conteneur Docker sur le serveur ciblé via SSH.

``` yaml
name: Build and Deploy

on:
# Déclenche le pipeline lorsqu'un nouveau tag Git est poussé sur master ou main
  push:
    branches: [ "master", "main" ]
    tags: [ 'v*.*.*' ]
  pull_request:
    branches: [ "master", "main" ]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build:
    runs-on: self-hosted
    outputs:
      tags: ${{ steps.meta.outputs.tags }}
      labels: ${{ steps.meta.outputs.labels }}
    steps:
      - uses: actions/checkout@v4

# utilisation de buildx pour profiter du cache avancé. On peut aussi l'utiliser pour les builds Docker multi-plateformes
      - name: Set up buildx (single-platform, with cache)
        uses: docker/setup-buildx-action@v3

      - id: meta
        name: Extract metadata (tags, labels)
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            # Tag avec la version complète (par ex. v1.2.3) lorsqu'un tag Git est poussé
            type=semver,pattern=v{{version}}
            # Conserve aussi les tags existants
            type=raw,value=latest
            # type=sha
# Construit l'image Docker pour vérifier les erreurs sans la pousser vers le registre Docker GitHub.
      - name: Build Docker image
        uses: docker/build-push-action@v5
        with:
          context: ./Spikylin
          push: false
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  push:
    runs-on: self-hosted
    needs: build
    if: |
      startsWith(github.ref, 'refs/tags/')
    steps:
      - uses: actions/checkout@v4

      - name: Set up buildx (single-platform, with cache)
        uses: docker/setup-buildx-action@v3

      - name: Log in to the Container registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Push Docker image
        uses: docker/build-push-action@v5
        with:
          context: ./Spikylin
          push: true
          tags: ${{ needs.build.outputs.tags }}
          labels: ${{ needs.build.outputs.labels }}
          provenance: false # Empêche l'envoi d'artefacts OCI supplémentaires (comme la provenance ou un SBOM)
          sbom: false # Empêche l'envoi d'artefacts OCI supplémentaires (comme la provenance ou un SBOM)
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    needs: push
    # Ne déploie que sur un push de tag ou lorsqu'un tag est fourni à workflow_dispatch
    # Comme le runner GitHub Action est sur le même serveur que celui à déployer, je me connecte au serveur via SSH sur localhost. Cela évite des problèmes de permissions car GitHub Action tourne avec un utilisateur différent. Quand le runner tourne sous un autre utilisateur, il peut ne pas voir les autres conteneurs Docker ni les réseaux Docker.
    if: |
      startsWith(github.ref, 'refs/tags/') 
    runs-on: self-hosted
    env:
      REGISTRY: ghcr.io

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Determine image tag
        id: tag
        run: |
          if [[ "${GITHUB_REF}" == refs/tags/* ]]; then
            TAG=${GITHUB_REF#refs/tags/}
          elif [ -n "${{ github.event.inputs.tag }}" ]; then
            TAG=${{ github.event.inputs.tag }}
          else
            TAG=latest
          fi
          echo "tag=$TAG" >> $GITHUB_OUTPUT

      - name: Deploy via SSH Loopback (localhost)
        uses: appleboy/ssh-action@v1.2.4
        with:
          host: 127.0.0.1
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            echo "${{ secrets.GITHUB_TOKEN }}" | docker login ${{ env.REGISTRY }} -u ${{ github.actor }} --password-stdin
            export TAG=${{ steps.tag.outputs.tag }}
            export IMAGE=${{ env.REGISTRY }}/${{ github.repository }}:$TAG
            docker pull $IMAGE
            docker compose -f ${{ github.workspace }}/Spikylin/docker-compose.yml -p spikylin up -d --remove-orphans

      - name: Announce deployment
        run: |
          echo "Successfully deployed ${{ github.repository }}:${{ steps.tag.outputs.tag }} via SSH loopback"
```

```