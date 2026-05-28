---
title: Comment déployer SvelteKit avec Prisma sur un serveur Node
description: Étapes pour déployer un projet SvelteKit minimaliste
date: '2023-07-17'
tags:
  - DevOps
  - SvelteKit
published: true
featured: false
---
Bonjour à tous. Je sais que Vercel est rapide et gratuit, mais j'aime faire les choses moi-même et contribuer à la communauté, alors j'ai créé ce guide rapide de déploiement pour les personnes qui souhaitent déployer SvelteKit sur leur serveur domestique ou leur VPS. (Avertissement : je ne suis pas sûr que ce guide soit prêt pour le projet de votre entreprise à 10 milliards de dollars.) Bref, cela fonctionne pour ma petite application personnelle.
Mon environnement et ma stack technique : je tourne sur Ubuntu 20.04.6 LTS sur le vieux Dell de dix ans de ma grand-mère. Mon application est construite avec SvelteKit, Prisma et Lucia.

## Préparer l'application

Utilisez l'adaptateur Node dans la configuration Svelte. Voici la mienne :

```ts
import adapter from '@sveltejs/adapter-node';
import { vitePreprocess } from '@sveltejs/kit/vite';

/** @type {import('@sveltejs/kit').Config} */
const config = {
    preprocess: vitePreprocess(),

    kit: {
        adapter: adapter({ out: 'build' }),
        alias: {
            $components: 'src/components',
        }
    }
};
export default config;
```

Commitez le code sur GitHub, car je vais ensuite cloner l'application sur le serveur avec Git. (Je n'aime pas utiliser SSH pour téléverser les fichiers, car il faut suivre soi-même tous les changements de fichiers — comme package.json et les artefacts de build — ce qui peut provoquer des erreurs et une complexité inutile.) Clonez tout votre projet sur votre serveur, ajoutez un fichier .env à la racine du projet et mettez-y votre chaîne de connexion pour le client Prisma. Installez aussi dotenv-cli si ce n'est pas déjà fait.

Placez-vous dans le dossier du projet :

```shellscript
npm install
npm run build // cela génère un build dans ./build
dotenv -e .env.production -- npx prisma migrate deploy // exécute les migrations sur l'environnement choisi.
dotenv -e .env.production -- npx prisma generate // recrée le client Prisma
```

## Configurer pm2 et Nginx

```shellscript
pm2 init simple // génère un fichier de configuration ecosystem pm2 dans le répertoire courant
```
Fichier de configuration pm2 :

```javascript
module.exports = {
  apps : [{
    name   : "spikylin-app",
    script : "/home/lin/spikylin-app/build/index.js",
    watch: true,
    env: {
        "PORT": 3000,
        "BODY_SIZE_LIMIT":"50000000",
        "NODE_ENV": "development"
    },
    env_production: {
        "DATABASE_URL":"*",
        "BODY_SIZE_LIMIT":"50000000",
        "NODE_ENV": "production",
    }
  }]
}
```
Vous pouvez configurer plusieurs applications. C'est très pratique si vous avez des environnements dev, test, QA et production sur le même serveur. Il vous faudra bien sûr plusieurs configurations Nginx, mais je pense que c'est assez simple. Le `BODY_SIZE_LIMIT` sert à permettre à SvelteKit d'envoyer de gros fichiers.
Avant de démarrer pm2, vous pouvez toujours utiliser `node ./build/index.js` pour tester votre application. Utilisez dotenv pour tester votre application avec les variables d'environnement souhaitées. Après le test, vous pouvez démarrer l'application avec pm2 en utilisant vos variables d'environnement de production.

```shellscript
dotenv -e ./myapp/.env.production -- pm2 start ./ecosystem.config.js
```

À noter, vous pouvez aussi ajouter les variables d'environnement directement dans le fichier ecosystem config :
```javascript
  apps : [{
    ...
    env: {
        "DATABASE_URL":"dev-database",
        "NODE_ENV": "development"
    },
    env_production: {
        "DATABASE_URL":"prod-database",
        "NODE_ENV": "production",
    }
  }]

```

Au démarrage de pm2, il suffit d'indiquer le nom de l'environnement :
```shellscript
start ./ecosystem.config.js --env production
```

Maintenant, vous devez configurer Nginx :

```nginx
server {
    server_name www.example.com
    listen 80;
    location / {
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header Host $http_host;

        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";

        proxy_pass http://127.0.0.1:3000;
        proxy_redirect off;
        proxy_read_timeout 240s;
    } 
}
```

Ceci n'est qu'un modèle. Il vous faudra bien sûr votre nom de domaine.
Si vous voulez servir des fichiers téléversés, vous devez pour l'instant les servir via Nginx. Ne servez pas les fichiers depuis le dossier build, car lorsqu'un nouveau build est généré, il effacera ces fichiers.

```nginx
location /image/ {
    root /home/lin/spikylin-app/static;
}
```
Ensuite, vous pouvez utiliser certbot pour obtenir un certificat SSL pour votre application. (Sélectionnez les sites un par un si vous en avez plusieurs pendant l'exécution de la commande.) N'oubliez pas d'ouvrir les ports 80 et 443 sur votre serveur. Définissez `client_max_body_size 50M` pour autoriser le téléversement de gros fichiers.
## Bonus :

Vous pouvez ajouter des alias à votre package.json et créer un petit script shell pour automatiser les futurs déploiements. N'oubliez pas d'ajouter le droit d'exécution avec +x au script. Vous pouvez aussi créer plusieurs scripts pour différents environnements. Pour ma part, je n'ai pas besoin d'environnement de test pour ma petite application, donc je n'ai qu'un script pour la production.

```shellscript
pm2 stop ../ecosystem.config.js --env production
git pull origin main
npm install
npm run build
npm run migrate:prod
npm run generate-client:prod
pm2 start ../ecosystem.config.js --env production
```
Merci de votre lecture.

```