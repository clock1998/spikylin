---
title : 'Building a personal budget planner'
description: A personal budget planner that can parse bank/credit card transactions from PDFs and categorize transactions automatically. 
date: '2026-01-29'
updated: '2026-01-29'
tags: 
    - AI
    - DevOps
published: true
featured: true
---
- PDF parser should take a list of PDFs.
- PDF parser should output a list of transactions.
- Business categorizer labels each transaction
- Business categorizer returns a list of transactions with label
- User should verify the output
- User accept output
- System saves transactions in database.

```mermaid
graph TD;
    1[PDF Parser]-->LLM-->Arxiv[Arxiv Query]-->Re[Re-rank Model]-->|Take First three|Summarize[LLM Summarize]-->Response-->TTS
```