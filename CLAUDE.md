## Agent skills

### Issue tracker

Issues live in GitHub Issues (baautitz/SI_VINICIUS_TENGATEN_2026), via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.

## Commits

Todo commit é atômico: uma mudança lógica por commit, que compila e passa nos testes sozinha. Nunca juntar assuntos diferentes (ex.: glossário, token de design e componentes) num commit só. Se a mudança tem mais de um motivo, dividir antes de commitar.

Formato da mensagem: `tipo(escopo): descrição` (Conventional Commits).

- **tipo**: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `build`, `ci`.
- **escopo** (opcional): área afetada, como `frontend` ou `backend`. Omitir quando a mudança é transversal ao repositório (ex.: `docs: ...` para o GLOSSARY.md).
- **descrição**: português, sem acentos, no imperativo ou infinitivo, minúscula, sem ponto final, até ~72 caracteres. Descreve o que muda, não como.
- Sem linha de co-autoria ou atribuição (`Co-Authored-By`) em commits e PRs.
- Corpo só quando o porquê não é óbvio pelo título.

Ordem dentro de uma branch: documentação e vocabulário primeiro, depois a base (tokens, tipos, componentes base), depois quem consome. Cada commit deve poder ser revertido sem quebrar os anteriores.
