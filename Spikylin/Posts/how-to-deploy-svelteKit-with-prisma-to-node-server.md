---
title : How to Deploy SvelteKit with Prisma to Node server
description: Steps to deploy SvelteKit barebone
date: '2023-07-17'
tags: 
    - DevOps
    - SvelteKit
published: true
featured: false
---
Hello everyone. I know Vercel is quick and free, but I like to do things myself and contribute to the community, so I create this quick deployment guide for people who wish to deploy Sveltekit to their home server or VPS. ( Disclaimer: I am not sure if this guide is ready for your 10 billion dollar company project) Anyway, it works for my small personal app.
My environment and tech stacks: I am running Ubuntu 20.04.6 LTS on my grandma's ten years old Dell. My application is built with SvelteKit, Prisma, and Lucia.

## Prepare the app

Use the node adapter for the svelte configuration. Here is mine:

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

Commit the code to GitHub, as I will git clone the app to the server afterwards. (I don't like to upload using ssh as you have to track all the file changes (like package.json and build files) by yourself, which can lead to errors and unnecessary complexity. Git clone your whole project to your server and add a .env file to the project root directory and put your connection string in this for Prisma client. Also, install dotenv-cli if you have not.

cd to your project folder:

```shellscript
npm install
npm run build //this will generate a build in ./build
dotenv -e .env.production -- npx prisma migrate deploy // this will run migrations on your choosing environment.
dotenv -e .env.production -- npx prisma generate // this re-establishes the Prisma client 
```

## Configure pm2 and Nginx

```shellscript
pm2 init simple //to generate a pm2 ecosystem config file at the current directory
```
pm2 config file:

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
You can multiple apps configure. This is great if you have dev, test, QA, and production running on the same server. You will, of course, need multiple configurations for Nginx, but I think it is very straightforward. The"BODY_SIZE_LIMIT" is for SvelteKit to upload large files.
Before you start pm2 you can always use "node ./build/index.js" to test your application. Use dotenv to test your application with the environment variables you want. After the test, you can start the application with pm2 with your production environment variables.

```shellscript
dotenv -e ./myapp/.env.production -- pm2 start ./ecosystem.config.js
```

FYI, you can also add the environment variables in ecosystem config file:
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

When start pm2, just put the environment name:
```shellscript
start ./ecosystem.config.js --env production
```

Now you need to config Nginx:

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

This is just a template. You will need your domain name, of course.
If you want to server uploaded file, you have to serve it through Nginx at the moment. Do not serve the file in build folder as when it creates a new build it will erase the files.

```nginx
location /image/ {
    root /home/lin/spikylin-app/static;
}
```
Then you can go to certbot for an SSL certificate for your application. (Select 1 by 1 if you have multiple sites when running the command). Remember to open ports 80 and 443 on your server. Set client_max_body_size 50M for uploading big files.
## Bonus:

You can add aliases to your package.json and create a simple shell script to automate future deployment processes. Remember to add +x to the script. You can also create multiple scripts for different environments. For me, I do not need a test environment for my small app, so I only have a script for production.

```shellscript
pm2 stop ../ecosystem.config.js --env production
git pull origin main
npm install
npm run build
npm run migrate:prod
npm run generate-client:prod
pm2 start ../ecosystem.config.js --env production
```
Thanks for your reading.