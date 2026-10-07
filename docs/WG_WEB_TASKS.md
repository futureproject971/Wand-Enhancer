# WG Enhancer Web — tarefas e estado

Referência: handoff de 2026-10-07. Base local: `wg-rebrand-keyauth`,
`9cfb537cc0da63110b695424ed0ffeb9c30103ec`.

Objetivo: somente a janela WG visível, usando o Wand instalado e seu renderer
técnico vivo. Primeiro provar a integração; depois aplicar o visual completo.
Execução nesta sessão pelo agente principal. Um commit consolidado e um push
quando a rodada estiver validada. Preservar LICENSE, backups e restore.

## 0. Base e licença

- [x] Ler código atual, AGENTS.md, workflow e logs do CI.
- [x] Confirmar `fork: false` na resposta do workflow run.
- [x] Rerodar jobs falhos do run `37554108716`.
- [x] Ajustar workflow para aceitar `VISION_LOADER_TOKEN` ou `WG_VISION_LOADER_TOKEN`.
- [ ] Guard Vision passar em build push/dispatch e gerar Release com token.
- [ ] Testar no Windows key válida, inválida, expirada, HWID diferente e rede offline.
- [ ] Testar DPAPI e revalidação da licença salva após reiniciar.
- [ ] Endurecer parser: HTTP 2xx sem confirmação explícita não deve autorizar.
- [ ] Definir e testar revalidação periódica; hoje a validação ocorre na inicialização.

Bloqueio observado: a segunda tentativa do run falha por token ausente/vazio.
O print enviado depois mostra secret `WG_VISION_LOADER_TOKEN`, enquanto o workflow
lia `VISION_LOADER_TOKEN`. O fallback passou no CI push `37561759904`, que gerou Release.
O teste do usuário mostrou recusa de autorização (401/403) ao inserir a key;
a validação real da licença segue pendente. Isso não prova token inválido isoladamente.
Se ambos os secrets estiverem cadastrados, `VISION_LOADER_TOKEN` tem prioridade.
Atualizar secret exige recompilar e baixar o artefato novo; não muda EXEs antigos.
O artefato do run `37554113956` não prova Vision: guard skipped no evento PR.
O token pode ser fornecido por ambiente em runtime, mas isso não valida o artefato
distribuível com configuração de build. Não armazenar secrets nesta documentação.

## 1. Protocolo e bridge

- [x] Mapear mensagens/endpoints em `WG_WEB_PROTOCOL.md`.
- [x] Mudar bind padrão para `127.0.0.1` no contrato compartilhado.
- [x] Anunciar somente URLs alcançáveis pelo bind local.
- [x] Expor porta real e versão do protocolo no health.
- [x] Distinguir snapshot ainda ausente de biblioteca vazia já recebida.
- [x] Adicionar teste de porta ocupada com fallback e health real.
- [ ] Publicar descriptor efêmero de sessão com PID, porta, URLs e nonce do controller.
- [ ] Verificar descriptor, PID controlado e nonce antes de aceitar health.
- [ ] Medir prontidão e desconexão do renderer separadamente do HTTP.

## 2. POC Windows — somente após validar fase 0

- [ ] Criar `feature/wg-web-shell` a partir da base Vision validada.
- [ ] Isolar sessão do controller: não assumir controle de Wand já aberto.
- [ ] Localizar instalação e validar compatibilidade antes do launch.
- [ ] Garantir criação de janela técnica com `show:false` antes do primeiro frame.
- [ ] Impedir show/restore, retirar taskbar e manter webContents vivos.
- [ ] Aplicar failsafe Win32 somente aos processos/janelas da sessão controlada.
- [ ] Testar renderer invisível e `backgroundThrottling:false` se necessário.
- [ ] Aguardar descriptor, health compatível, renderer e snapshots com timeout.
- [ ] Hospedar painel existente em WebView2; restringir navegação ao bridge local.
- [ ] Encerrar processos controlados ao fechar WG; tratar crash e reinício.
- [ ] Provar biblioteca, launch/stop, trainer, toggle e slider reais.
- [ ] Provar cold boot sem flash, popup antigo, taskbar ou restore do Wand.

Se invisibilidade não puder ser garantida, abortar e mostrar:
“Não foi possível iniciar o WG Engine. Esta versão ainda não é compatível.”

## 3. Visual WG e distribuição

- [ ] Rebrand web com logo oficial, preto/branco/vermelho e textos PT/EN.
- [ ] HOME, MEUS JOGOS, TRAINER e CONFIGURAÇÕES usando funções existentes.
- [ ] Mostrar progresso, erros e diagnóstico sem secrets.
- [ ] Testar update/reapply, segunda instância, reconexão e recovery.
- [ ] Validar empacotamento WebView2: loader nativo/runtime antes de prometer single exe.
- [ ] Gerar WGEnhancer.exe; Wand continua dependência instalada separadamente.
- [ ] Mobile fica para fase posterior com opt-in, token efêmero e expiração.

## Limites desta primeira rodada

Não há teste de engine/WPF/WebView2 neste ambiente Linux e não há instalação Wand.
Nenhum controle real de jogo ou invisibilidade foi comprovado. A implementação
atual cobre preparação do bridge, documentação e testes locais.

## Rodada de diagnóstico Vision

- [x] Incluir código HTTP e descrição controlada em erros de resposta do servidor.
- [x] Impedir reprodução de corpo bruto/token/key no diagnóstico de falha.
- [x] Preservar licença salva em recusa genérica de autorização/erro do loader.
- [x] Adicionar 14 casos de diagnóstico ao build Windows, sem requests nem secrets reais.
- [ ] Confirmar build e casos de diagnóstico no CI desta rodada.
- [ ] Testar o novo EXE contra Vision real e obter confirmação de licença válida.

O teste de mensagem 401/403 não substitui a verificação do token no backend.
Nenhum secret foi lido, alterado ou impresso pelo agente nesta rodada.
