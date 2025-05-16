---
title : ThreeJs 3D Carousel
description:  ThreeJs Experiment.
date: '2023-09-11'
tags: 
    - SvelteKit
    - ThreeJs
published: true
featured: false
---

![image](/post_images/threejs.png "Example")

Ce n'est pas un tutoriel, mais une expérience avec ThreeJs. Je veux créer un effet de carrousel 3D pour afficher les profils des utilisateurs. Chaque rectangle affichera une photo de profil avec quelques informations de base sur l'utilisateur. Comme j'utilise SvelteKit, il est logique de choisir une bibliothèque compatible avec SvelteKit pour intégrer ThreeJs dans le projet. Mon choix s'est porté sur Threlte, qui rend les choses plus claires et intuitives.

L'idée est très simple : nous modifions la position des rectangles pour les disposer en cercle. Lorsqu'un défilement ou un clic de souris est détecté, les positions changent, créant ainsi un effet de carrousel. Pour l'animation de transition, j'utilise spring, qui offre un bel effet d'inertie.

Limitations: J'ai essayé d'afficher du HTML sur les cartes, mais cela ne s'affiche pas correctement, comme le montre l'image ci-dessus. En effet, le HTML est affiché au-dessus du canvas via CSS3D, ce qui signifie qu'il n'appartient pas à ThreeJS. Il ne sait donc pas quel élément est derrière, ce qui pose des problèmes d'affichage.

## Solutions possibles:

Une solution serait de transformer le HTML en une texture afin qu'il puisse être rendu sur le maillage. Des recherches montrent que cela pourrait fonctionner, mais cela nécessiterait d'utiliser des bibliothèques spécifiques pour convertir le HTML en texture. Mon contenu HTML n'est pas très complexe, mais des expérimentations supplémentaires sont nécessaires.

Une autre approche consiste à ne pas afficher du HTML du tout et à afficher uniquement des images et du texte directement sur le maillage du rectangle. Cette solution est plus simple et facile à mettre en œuvre, bien qu'elle permette d'afficher un contenu moins riche.


```svelte
<script lang="ts">
    import { isTouchDevice } from "$lib/helper/util";
    import { T, useFrame } from "@threlte/core";
    import {
        Center,
        HTML,
        OrbitControls,
        interactivity,
    } from "@threlte/extras";
    import { onDestroy, onMount } from "svelte";
    import { spring } from "svelte/motion";

    let items = [
        { color: "red" },
        { color: "green" },
        { color: "blue" },
        { color: "blue" },
        { color: "blue" },
        { color: "blue" },
        { color: "blue" },
        { color: "blue" },
        { color: "blue" },
        { color: "blue" },
        // ... add more items/colors as needed
    ];
    let radius = 2;
    let rotation = Math.PI / 2;
    let lastMouseX = 0;
    const scale = spring(1);

    onMount(() => {
        // Initialize positions
        updatePositions();
    });

    interactivity();
    useFrame((state, delta) => {
        // $rotation.radian += delta * 0.5;
        // if (rotation >= 2 * Math.PI) rotation -= 2 * Math.PI;
    });

    function setCenterItem(itemIndex: number) {
        let clickedAngle = (2 * Math.PI * itemIndex) / items.length + rotation;
        if (clickedAngle < Math.PI / 2) {
            rotation += Math.PI / 2 - clickedAngle;
        } else {
            rotation -= clickedAngle - Math.PI / 2;
        }
        updatePositions();
    }

    // Create an array of position springs, one for each item
    let positionSprings = items.map(() => spring({ x: 0, y: 0, z: 0 }));
    let positionSpringStoreValues: any[] = [];
    for (let [i, store] of positionSprings.entries()) {
        let unsubscribe = store.subscribe((value) => {
            positionSpringStoreValues[i] = value; // Svelte makes this reactive
        });
        onDestroy(unsubscribe);
    }

    function updatePositions() {
        items.forEach((_, i) => {
            let radian = rotation + (2 * Math.PI * i) / items.length;
            let x = Math.cos(radian) * radius;
            let z = Math.sin(radian) * radius;
            if (Math.sin(radian) == 1) {
                updateScales(i);
            }
            positionSprings[i].set({ x, y: 0, z });
        });
    }

    let scaleSprings = items.map(() => spring(1));
    let scaleSpringsStoreValues: number[] = [];
    for (let [i, store] of scaleSprings.entries()) {
        let unsubscribe = store.subscribe((value) => {
            scaleSpringsStoreValues[i] = value; // Svelte makes this reactive
        });
        onDestroy(unsubscribe);
    }

    function updateScales(itemIndex: number) {
        items.forEach((_, i) => {
            if (i == itemIndex) scaleSprings[i].set(1.12);
            else scaleSprings[i].set(1);
        });
    }
</script>

<T.PerspectiveCamera
    makeDefault
    position={[0, 0, 5]}
    on:create={({ ref }) => {
        ref.lookAt(0, 0, 0);
    }}
>
</T.PerspectiveCamera>
<T.DirectionalLight position={[0, 10, 10]} />
{#each items as item, i}
    <T.Mesh
        on:wheel={(e) => {
            e.stopPropagation();
            rotation +=
                e.nativeEvent.deltaY * ((2 * Math.PI) / items.length) * 0.01;
            updatePositions();
            if (rotation >= 2 * Math.PI) rotation -= 2 * Math.PI;
            if (rotation <= -2 * Math.PI) rotation += 2 * Math.PI;
        }}
        on:pointerup={(e) => {
            //ignore object behind.
            e.stopPropagation();
            setCenterItem(i);
            console.log("click");
            // Ensure rotation stays within [0, 2 * Math.PI]
            while (rotation >= 2 * Math.PI) rotation -= 2 * Math.PI;
            while (rotation < 0) rotation += 2 * Math.PI;
        }}
        on:pointermove={(e) => {
            e.stopPropagation();
            if (isTouchDevice()) {
                const stepSize = 0.1; // Number of pixels for one step of rotation. Adjust as needed.
                const rotationAnglePerStep = (2 * Math.PI) / items.length; // Fixed angle of rotation for one step.
                const deltaX = Math.abs(e.pointer.x - lastMouseX);
                // Calculate the number of steps based on the mouse movement
                const steps = Math.round(deltaX / stepSize);
                // Rotate by a fixed angle based on the number of steps
                rotation += steps * rotationAnglePerStep;
                lastMouseX = e.pointer.x;
                if (rotation >= 2 * Math.PI) rotation -= 2 * Math.PI;
                if (rotation <= -2 * Math.PI) rotation += 2 * Math.PI;
                updatePositions();
            }
        }}
        position={[
            positionSpringStoreValues[i].x,
            positionSpringStoreValues[i].y,
            positionSpringStoreValues[i].z,
        ]}
        scale={scaleSpringsStoreValues[i]}
    >
        <HTML occlude transform position.z={0.0001}>
            <button
                class="bg-orange-500 rounded-full px-3 text-white hover:opacity-90 active:opacity-70"
            >
                I'm
            </button>
        </HTML>

        <T.PlaneGeometry args={[1.3, 2]} />

        <T.MeshStandardMaterial color={item.color} />
    </T.Mesh>
{/each}
```