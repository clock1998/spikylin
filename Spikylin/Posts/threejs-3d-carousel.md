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

This is not a tutorial but an experiment with using ThreeJs. I want to create a 3D carousel effect for displaying users' profiles. Each rectangle will display a profile picture with some basic information about the user. As I am using SvelteKit, it is logical to choose a SvelteKit friendly library to bring ThreeJs to the project. My choice is Threlte. It makes things clearer and more intuitive.
The idea is very simple. We change the rectangles position to make it a circle. When it detects scroll or mouse down, the positions will change, making it like a carousel. For the transition animation, I use the building animation from spring as it has a very nice inertia effect.

Limitations: I was trying to display HTML on the card, but it does not show correctly as shown in the image above, because the HTML is displayed on top of canvas using CSS3D. It does not belongs to ThreeJS, so it does not know which element is behind and makes the display problematic. 

## Possible solutions:

The first solution is to not use the HTML, instead, convert the HTML to a texture so that it can be rendered on the mesh. Research shows it could be a solution, but it requires converting HTML to texture through some libraries. My HTML content is not very complex, but more experiment is required.

The second solution is to dump the idea of displaying HTML. I just display pictures and text on the rectangle mesh directly. The solution is straightforward and easy to implement, although it can only display less rich content. 


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