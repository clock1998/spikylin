---
title: 'A Simple Experiment: An AI Research Assistant Powered by Arxiv'
description: An experiment project that demonstrates a simple AI research assistant implementation using LLM, Gradio, Hugging Face, and Python
date: '2025-12-30'
tags: 
    - AI
    - LLM
published: true
featured: false
---

Le but de ai-research-assistant est de rendre l'expérience de recherche d'articles académiques plus humaine, semblable à une conversation réelle. Le projet comprend une API ainsi qu'une interface web (UI). J'ai utilisé Hugging Face pour l'ensemble des pipelines d'IA.

L'interface utilisateur est propulsée par Gradio, tandis que l'API fonctionne sous FastAPI. L'interface web inclut une fenêtre de chat avec des entrées et sorties textuelles et audio. De manière générale, le système fonctionne ainsi : lorsqu'un utilisateur envoie un message vocal, Whisper convertit la voix en texte. Le LLM prend ensuite ce texte pour générer une requête de recherche Arxiv, puis exécute cette requête. Le système récupère les 50 premiers résultats et les envoie à un modèle de ré-ordonnancement (re-ranking), car la recherche initiale est une simple recherche par mots-clés. Le modèle calcule un score de pertinence basé sur la question originale de l'utilisateur et le résumé de chaque résultat. Le système trie ensuite les résultats par score de pertinence décroissant. Enfin, il sélectionne les trois premiers articles et les résume à nouveau pour fournir une version courte accompagnée du lien de l'article. La réponse finale est envoyée à un moteur TTS (gTTS dans ce cas). Pour finir, l'assistant IA télécharge la réponse sur Notion via l'API d'intégration Notion.

## Travaux futurs
J'ai deux projets pour la suite. Je réfléchis à transformer l'assistant IA en un MCP (Model Context Protocol). Cela permettrait d'augmenter l'ergonomie et l'accessibilité de l'assistant. L'autre projet consiste à en faire un agent. Cela pourrait améliorer encore les performances, car il serait capable de s'auto-inciter (self-prompt) pour trouver les articles les plus pertinents.

## Problèmes rencontrés
J'ai remarqué que le modèle LLM open source utilisé ici, Llama-3.1-8B-Instruct, ne génère pas toujours des données au format JSON parfait. D'après mes recherches, il existe plusieurs solutions potentielles. La plus simple est d'utiliser un modèle commercial incluant un paramètre pour définir le format de sortie souhaité. Je pourrais également implémenter une fonction de secours (fallback) : si le format est incorrect, la fonction relance simplement le prompt. Enfin, il existe des outils permettant de contraindre la sortie, comme Outlines.

## Interesting code snippets  

### System prompt of the search query builder
```python
    SYSTEM_PROMPT = """
    You are a search query engineer. Your goal is to transform a user's research question into a precise arXiv API query string.
    Rules:
    Use field prefixes: ti: (title), au: (author), abs: (abstract), cat: (category).
    Use Boolean operators: AND, OR, ANDNOT (must be capitalized).
    Group terms using parentheses.
    If the user mentions a specific field (e.g., "find papers by Hinton"), use au:.
    If a user is looking for a specific concept, you should use Title(ti:) or Abstract(abs:).
    Query Expansion: Include synonyms (e.g., "LLM" OR "Large Language Model").
    [FUNCTION_SCHEMA]
    {"function": "search_arxiv", "arguments": {"query": "string"}}
    [EXAMPLES]
    User: "Search for quantum computing."
    Assistant: {"function": "search_arxiv", "arguments": {"query": "all:quantum AND all:computing"}}
    User: "Find papers by Einstein."
    Assistant: {"function": "search_arxiv", "arguments": {"query": "au:Einstein"}}
    """
```

### Function calling using LLM

```python
def _route_llm_output(self, llm_output: str) -> str:
        """
        Route LLM response to the correct tool if it's a function call, else return the text.
        Expects LLM output in JSON format like {"function": "...", "arguments": {...}}.
        """
        # Try to parse the entire output as JSON directly
        try:
            output = json.loads(llm_output.strip())
        except json.JSONDecodeError:
            # If that fails, try to extract JSON object from the text
            json_match = re.search(r'\{[^{}]*"function"[^{}]*\}', llm_output)
            if json_match:
                try:
                    output = json.loads(json_match.group())
                except json.JSONDecodeError:
                    # Not a JSON function call; return the text directly
                    return llm_output
            else:
                # Not a JSON function call; return the text directly
                return llm_output

        # Extract function name and arguments
        func_name = output.get("function")
        args = output.get("arguments", {})

        if not func_name:
            # Invalid JSON structure; return the text directly
            return llm_output

        if func_name == "none":
            # No function call needed; return empty or default response
            return ""
        elif func_name == "search_arxiv":
            query = args.get("query", "")
            return self._search_arxiv(query)
        else:
            return f"Error: Unknown function '{func_name}'"
```

### Re-ranker

```python
from sentence_transformers import CrossEncoder

model = CrossEncoder('BAAI/bge-reranker-v2-m3', device='cuda')

def rerank_crossencoder(question: str, candidates: list[str]) -> list[float]:
    """
    Given a question and a list of candidate strings, return CrossEncoder relevance scores.
    Args:
        question (str): The input question.
        candidates (list of str): List of candidate answer strings.
    Returns:
        list of float: Scores for each candidate, in order.
    """
    pairs = [(question, candidate) for candidate in candidates]
    scores = model.predict(pairs)
    return scores
```