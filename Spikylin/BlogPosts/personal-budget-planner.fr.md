---
title: 'Construire un planificateur de budget personnel'
description: Un planificateur de budget personnel capable d'extraire des transactions bancaires et de cartes de crédit à partir de PDF et de catégoriser automatiquement les transactions.
date: '2026-01-29'
updated: '2026-01-29'
tags:
  - AI
  - DevOps
published: true
featured: true
---
- L'analyseur PDF doit accepter une liste de PDF.
- L'analyseur PDF doit produire une liste de transactions.
- Le catégoriseur d'entreprises attribue une étiquette à chaque transaction.
- Le catégoriseur d'entreprises retourne une liste de transactions avec étiquette.
- L'utilisateur doit vérifier le résultat.
- L'utilisateur accepte le résultat.
- Le système enregistre les transactions dans la base de données.

```mermaid
graph TD;
    1[PDF Parser]-->LLM-->Arxiv[Arxiv Query]-->Re[Re-rank Model]-->|Take First three|Summarize[LLM Summarize]-->Response-->TTS
```

```