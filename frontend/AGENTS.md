<!-- BEGIN:nextjs-agent-rules -->

# Next.js: ALWAYS read docs before coding

Before any Next.js work, find and read the relevant doc in `node_modules/next/dist/docs/`. Your training data is outdated — the docs are the source of truth.

<!-- END:nextjs-agent-rules -->

# Frontend Engineering Standards

## Architecture & Data Management

- **Single Source of Truth (SSOT):** All data structures must be represented by the **Rich Entity** types defined in `src/features/[module]/[entity]/types.ts`.
- **Banned Patterns:**
  - Do NOT use `*Resumo`, `*Dto`, or `*View` interfaces.
  - Do NOT define interface types inline (e.g., `item: { id: number; nome: string }`).
- **Imports:** Import types directly from their source feature folder.
- **Form Values:** Use Zod schemas coupled with `FormValues` types. Reuse existing entity types for nested properties.
- **Type Safety:** `as unknown as T` is strictly forbidden. Use proper type definitions or type guards if necessary.

## Development Workflow

- Always verify type consistency with `npm run build` after changes.
- Ensure all list and upsert components use the Rich Entity type.
- Hotkeys must be explicitly mapped in the component to avoid conflicts.

## Data Fetching & Consistency

- **Real-time Consistency (ERP):** Global `staleTime` must be set to `0`. The system must never trust cached data for more than 0 seconds when opening critical forms.
- **Fetch-on-Edit Pattern:** Upsert dialogs/forms must NEVER rely solely on data passed from a list. They must perform a mandatory `fetchById` (or equivalent) to ensure the most current data from the database is used.
- **ReadOnly Mode:** All `Upsert` components must support a `readOnly` prop to disable inputs and actions for confirmed or cancelled records.
- **Context-Aware Productivity:** Hotkeys (like `Alt+C`) must track the currently focused element to provide relevant actions based on user context.
