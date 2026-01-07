---
title : A Simple Experiment: An AI Research Assistant Powered by Arxiv  
description:  An experiment project that demonstrates a simple AI research assistant implementation using LLM, Gradio, Hugging Face, and Python
date: '2025-12-30'
tags: 
    - AI
    - LLM
published: true
featured: false
---

The goal of the project to make the experience of searching academic papers more human like, more like a human conversation. The project contains a API and also an web UI. 

The UI is powered by gradio. The API is powered by FastAPI. The Web UI includes a chat window and both text and audio input and output. On a high level, the system works like this: when a user send a voice chat, Whisper will process the voice message to text. The The LLM will take the text and convert it to a Arxiv search query. The LLM will also execute the search query. The system will take the first 50 search result and send to a re-ranking model, because the search query is very basic keyword search. The model will calculate a relevance score based on the original user question and summary of each query results. The system will sort the search result based on the relevance score in a descending order. The system will then take the first three articles and summarize them again to output a shorter version of the summary along with the article link. The final output is also sent to a TTS engine. I used gTTS here. Lastly, the AI assistant will upload the response to Notion using the notion integration API.

```mermaid
A[Start] --> B{Is it raining?};
```
## Future Work

I have two plans for the project. I am thinking about making the AI assistant to a MCP. This will increase the usability and accessibility of the AI assistant. Another plan is to make an agent. This could further improve the performance of the AI assistant as it will self prompt to find the most relevance papers. 


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