---
title : How Did I Set Up My Blog?
description: Technology, setup...
date: '2024-12-18'
tags: 
    - SvelteKit
published: true
featured: false
---

## Technology

I chose SvelteKit with its static site generator feature as the framework for building my blog. To enhance the design and user experience, I incorporated TailwindCSS and DaisyUI for stylish and responsive styling.

For deployment, I containerized the application using Docker and published the Docker image on GitHub Packages. To streamline the CI/CD process, I leveraged Microsoft Azure DevOps, a tool I frequently use in my professional work. The live instance of the blog runs as a Docker container in my home lab, and I utilized Cloudflare Tunnel to securely expose it to the internet.

You can check at the project at [GitHub](https://github.com/clock1998/spikylin-blog).

You can check the YAML file in my [GitHub](https://github.com/clock1998/spikylin-blog/blob/main/azure-pipelines.yml) repository for the build pipeline.