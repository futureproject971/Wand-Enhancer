# WG Web Foundation Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task by task in the current session.

**Goal:** Preparar o bridge existente para um shell WG desktop local, sem refazer o protocolo.

**Architecture:** Manter o painel Preact e o bridge Electron. O bind padrão será
loopback; health exporá a porta efetiva e prontidão dos snapshots. O futuro
controller WPF hospedará o painel em WebView2 após validar a licença e a engine.

**Tech Stack:** TypeScript, Node HTTP/WebSocket, Preact, Vite, Vitest; host futuro C#/WPF .NET 4.8.

**Spec:** `docs/WG_WEB_TASKS.md`, baseado no handoff WG ENHANCER WEB de 2026-10-07.

## Global Constraints

- Bind desktop padrão `127.0.0.1`; não habilitar LAN automaticamente.
- Contrato de porta e paths somente em `web-panel/protocol/web-contract.json`.
- Nunca expor Vision token ou license key no frontend/health/logs.
- Não destruir renderer, reimplementar trainers nem redistribuir Wand.
- Um commit consolidado e um push após validação da rodada.
- Não iniciar redesign nem declarar POC pronta antes do teste Windows real.

## Review Focus

- Biblioteca vazia recebida deve diferir de ausência de snapshot.
- Porta preferida ocupada deve produzir health na porta real.
- Bind loopback não pode anunciar endereço LAN.
- Bind explícito deve anunciar o host correspondente.
- HTTP online não comprova renderer nem toggle real.

### Task 1: Auditoria e contrato

**Files:** `docs/WG_WEB_TASKS.md`, `docs/WG_WEB_PROTOCOL.md`.
**Interfaces:** Descrever o envelope vigente e health aditivo, sem mudar mensagens WS.

- [x] Ler AGENTS.md, logs de CI, AuthGate e VisionAuthService.
- [x] Registrar bloqueio de secret e limitações de teste Windows.
- [x] Mapear handshake, snapshots, comandos e resultados com semântica real.

### Task 2: Bind e readiness HTTP

**Files:** `web-panel/protocol/web-contract.json`,
`web-panel/bridge/src/server-files.ts`, `server.ts`, `bridge-state.ts`,
`runtime.integration.test.ts`.
**Interfaces:** `getAdvertisedUrls(port: number, host: string): string[]`;
health adiciona `port: number`, `protocolVersion: number`,
`installedAppsReady: boolean`, `gameStatusReady: boolean`.

- [x] Adicionar testes de runtime local, health e porta ocupada.
- [x] Executar contra bundle anterior e observar falha.
- [x] Implementar loopback padrão, URLs coerentes com bind e campos health.
- [x] Validar suite completa, TypeScript, build web, bundle e dist.
- [ ] Revisar diff e consolidar somente quando CI Vision puder passar.

### Próxima rodada

O descriptor, readiness de renderer, lifecycle e host Windows exigem plano
próprio após a base Vision real passar. Ver critérios de saída em `WG_WEB_TASKS.md`.
