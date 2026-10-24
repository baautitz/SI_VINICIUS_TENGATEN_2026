# Atalhos

A fonte da verdade e `frontend/src/lib/atalhos.ts` (`ATALHOS`: id, combinacao, rotulo). Regra: [ADR 0001](adr/0001-uma-letra-um-sentido-nos-atalhos.md).

- Novo atalho: adicionar no registro; `npm run test:atalhos` falha se a combinacao ja existir.
- Telas e `HotkeyKbd` devem ler o registro, nao usar strings livres.
- Vocabulario (atalho, dica de atalho): `GLOSSARY.md`.
