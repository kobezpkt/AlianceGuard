# AlianceGuard — Plugin EXILED para SCP: Secret Laboratory

Plugin de proteção comunitária para servidores de SCP: Secret Laboratory, integrado a um painel web para gerenciamento centralizado de banimentos e detecção de ATL's.

---

## Painel

**[https://alianceguard.com](https://alianceguard.com)**

---

## Funcionalidades

- **Verificação instantânea** — cada jogador é verificado no momento em que entra no servidor
- **Expulsão automática** — jogadores banidos são expulsos imediatamente com a razão do banimento
- **Detecção de alts** — identifica contas alternativas de jogadores banidos por IP e SteamID64
- **Log no Discord** — envia notificações automáticas em um canal do Discord quando um jogador banido ou alt tenta entrar
- **Aprovação de alts** — alts de jogadores banidos são enviadas para revisão manual via Discord

---

## Instalação

1. Baixe o arquivo `AlianceGuard.dll` na aba [Releases](https://github.com/kobezpkt/AlianceGuard/releases)
2. Copie o arquivo para a pasta `EXILED/Plugins/` do seu servidor
3. Reinicie o servidor — o plugin será carregado automaticamente

> **Requisito:** EXILED 9.13.x ou superior

---

## Como Funciona


Quando um jogador entra no servidor, o plugin consulta automaticamente o painel. Se o jogador estiver banido, é expulso imediatamente e um log é enviado ao Discord caso a função de webhook estaja habilitada no servidor. Caso o sistema identifique que o jogador pode ser uma conta alternativa de alguém banido — ele também é expulso e uma notificação é enviada ao Discord para os membros da moderação aprovarem/negarem o banimento da conta. Se o jogador estiver liberado, entra normalmente no servidor, podendo receber um cargo personalizado caso tenha um configurado no painel.


---

## Sobre os Banimentos

- Todos os banimentos são feitos com **motivo válido e provas anexadas**
- Apenas membros da staff do projeto podem registrar jogadores no painel
- Todos os banimentos são revisados pelos membros de moderação do projeto para garantir que a causa seja realmente justa
- O sistema conta com múltiplas camadas de verificação para evitar banimentos falsos ou falsificação de informações

---

## Suporte

Para reportar bugs, tirar dúvidas, reportar um jogador ou solicitar funcionalidades, abra um ticket no nosso Discord:

**[discord.com/invite/eA8JusX8tq](https://discord.com/invite/eA8JusX8tq)**
