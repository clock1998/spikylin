# Copilot Instructions

## Project Context
- This repository is a .NET 9 Razor Pages application.
- Prefer solutions that fit Razor Pages patterns over MVC or Blazor unless explicitly requested.

## General Guidelines
- Maintain a repository-level custom Copilot instruction file for consistency across the project.

## Coding Guidelines
- Use C# 13 features only when they improve clarity and match existing code style.
- Keep changes minimal and focused on the requested task.
- Reuse existing abstractions and utilities before introducing new ones.
- Avoid adding new dependencies unless required.

## UI and Interactivity Guidelines
- Use DaisyUI for UI components and styling, specifically the cupcake theme for the site.
- Use DaisyUI navbar patterns for clearer navigation visibility in the layout.
- In Razor Pages, use HTMX for client-side interactivity when interactivity is required.

## Markdown and Content Handling
- For blog posts, preserve YAML front-matter formatting with valid YAML syntax.
- Use spaces (not tabs) in YAML indentation.
- Keep metadata keys in `key: value` format with no space before `:`.

## Validation
- Ensure the solution builds after code changes.
- Prefer targeted tests for modified areas when tests are available.
