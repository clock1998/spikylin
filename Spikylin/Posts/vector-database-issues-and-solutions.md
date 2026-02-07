---
title : 'Vector Database Issues and Solutions'
description: Vector database
date: '2026-02-06'
updated: '2026-02-06'
tags: 
    - AI
    - Database
published: true
featured: true
---

Vector database is the key technology when developing a RAG solution. During the development of my AI powered personal budget planner, I have encountered two main issues: slow insertion and huge file size. For the context, I have put all the local businesses records in a cvs file. It has all the information that I need such as business name, business description, and business domain. In order to perform semantic search, I will need to convert for example business name to a embedding and save it to a database, that is Postgresql in my case. At the beginning, I just take a popular embedding model, sentence-transformers/all-mpnet-base-v2 from hugging face, to do the converting. It outputs a 700 dimension vector, and it worked fine with my small test sample data which only has 60 records. Problems appear when I try it with the real data file, which has 4 million records. It takes extremely long time for embedding and also database insertion. I have experimented here several techniques such as batching and the COPY command from Postgresql, but they are not effective. I noticed that every time I launch my script I needed to redo the embedding for each record which is a wasted of time and computing resources. I decided to compute the embedding once and save it to the cvs file so I will not need to redo the embedding in the future. However, another problem emerges here. sentence-transformers/all-mpnet-base-v2 by default outputs a 758 dimensions embedding. This makes the file size to explode. A 400 mb file becomes a 78 GB file. I had to adjust the model. I changed the model to jinaai/jina-embeddings-v3 so I can change to a smaller embedding output. After I switched to a 128 dimension output, the file size shrank significantly but it is still not optimal. Then, I applied another technique called binary quantization. This converts float to integer, significantly reduce the size. After all this, adding embeddings to a 400 file, it is only 1 GB. I then used the COPY command provided by Postgresql to import the data to database. 