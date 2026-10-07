# WG Web — protocolo existente

Fonte de verdade: `web-panel/protocol/web-contract.json` e `messages.ts`.
Este documento descreve o bridge existente; não substitui o motor de trainers.

## Endpoints e descoberta

O desktop usa `127.0.0.1`. A porta preferida e o intervalo de busca vêm do
contrato compartilhado; a porta efetiva pode mudar quando a preferida está ocupada.
O controlador não deve duplicar esses valores em C#.

| Endpoint | Uso |
| --- | --- |
| `/remote/` | Painel estático compilado |
| `/remote/ws` | WebSocket do painel |
| `/remote/api/health` | Estado HTTP e snapshots recebidos |

Health inclui `ok`, `protocolVersion`, `port`, `remoteUrl`, `advertisedUrls`,
`installedAppsReady`, `gameStatusReady`, `installedAppsCount`, `trainerId`,
`gameSessionState`, `gameSessionEvent` e `runningTrainerId`.

`installedAppsReady: true` com `installedAppsCount: 0` significa biblioteca
carregada e vazia. `false` significa que nenhum snapshot válido chegou.
`ok` comprova o servidor HTTP, não o renderer nem o funcionamento de um trainer.
`gameStatusReady` indica recebimento de status, não garantia de renderer ainda vivo.

Nesta rodada, health permite confirmar a porta encontrada. A descoberta inicial
por descriptor de sessão do controller ainda será implementada; não existe aqui.
No desktop, `advertisedUrls` contém apenas o endereço do bind local. Bind explícito
em endereço curinga mantém o comportamento LAN legado; não habilitar no shell WG
até existir autenticação própria de sessão remota.

## Envelope

Todas as mensagens usam `{ type, version, requestId, payload }`.
`version` é a versão do protocolo. `requestId` correlaciona comando e resultado;
eventos espontâneos usam `null`. Não incluir key nem token Vision no envelope.

| Mensagem | Direção | Payload e comportamento |
| --- | --- | --- |
| `hello` | UI → bridge | `client: mobile-web`, `clientVersion`, capabilities de delta e troca de trainer |
| `hello_ack` | bridge → UI | sessão, accepted, versão, URLs anunciadas |
| `trainer_meta` | bridge → UI | session.instanceId, trainer, schema.categories e schema.cheats |
| `trainer_values` | bridge → UI | trainerId e mapa values por target |
| `value_changed` | bridge → UI | trainerId, target, value, oldValue, source, cheatId opcional |
| `game_status` | bridge → UI | instanceId, updatedAt, session e trainer em idle/running |
| `installed_apps` | bridge → UI | instanceId, updatedAt e apps normalizados, sem caminhos privados |
| `set_value` | UI → bridge | trainerId, target, value, cheatId opcional |
| `set_value_result` | bridge → UI | ok, trainerId, target, error opcional |
| `remote_command` | UI → bridge | action launch/stop, gameId e titleId opcionais; launch exige gameId |
| `remote_command_result` | bridge → UI | ok, action, IDs e error opcional |
| `trainer_changed` | bridge → UI | previousTrainerId e trainerId; string vazia representa ausência |
| `error` | bridge → UI | code, message e details opcionais |

## Semântica importante

- Depois de `hello`, enviar todos os snapshots disponíveis, inclusive biblioteca
  e status quando nenhum trainer está ativo.
- `set_value_result.ok` confirma encaminhamento aceito pelo bridge. A confirmação
  do valor efetivo depende de `value_changed`/`trainer_values`; não tratar como
  prova de alteração no jogo.
- `remote_command` aguarda resposta IPC do renderer, com timeout de 60 segundos.
- Renderer ausente produz `bridge_not_ready`; comando inválido produz erro sem execução.
- WebSockets de outro origin são rejeitados. Origins locais de desenvolvimento
  continuam aceitos nas portas Vite declaradas no contrato.
- Reconectar exige novo `hello` e novos snapshots. Não reaplicar valores antigos
  automaticamente em outro trainer.

## Arquivos responsáveis

`server.ts`: HTTP, WebSocket, bind, fallback de porta e handlers.
`bridge-state.ts`: normalização/cache, snapshots, deltas e health.
`wand/runtime.ts`: handlers IPC e encaminhamento aos renderers vivos.
`wand/renderer-scripts.ts`: carregamento de scripts no renderer.
`src/remote-session/remote-session.client.ts`: sessão WebSocket do frontend.
`src/app/use-remote-panel.ts`: coordenação das funções já existentes da UI.
