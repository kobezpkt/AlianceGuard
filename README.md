# AllianceGuard - Plugin EXILED para SCP:SL

Plugin para integração do painel AllianceGuard com servidores SCP: Secret Laboratory usando EXILED.

## Link do painel:

[https://alianceguard.com/](https://alianceguard.com/)


## Funcionalidades

- Verifica automaticamente cada jogador que entra no servidore verifica se o jogador está banido
- Expulsa automaticamente jogadores banidos e envia um log para uma webhook no discord
- Detecta contas alternativas de jogadores banidos
- Verificação instantânea sem intervalo de tempo (ao entrar no servidor)

## Instalação

1. **Copiar o DLL para o servidor:**
   - Copie `AlianceGuard.dll` para a pasta `EXILED/Plugins/` do seu servidor



## Como Funciona

1. Quando um jogador entra no servidor, o plugin verifica se ele esta banido no painel e caso esteja o jogador e automaticamente expulso do servidor.
2. envia uma webhook para um canal no discord caso algum jogador tente entrar no servidor.
3. detecta se e conta e uma possivel alt.


## Notas Importantes

- A verificação é feita instantaneamente quando o jogador entra, não requer qualquer configuração adicional
- Os Banimentos so são feitos com um motivo valido e com provas
- NÃO e qualquer pessoa q pode adicionar um jogador no painel

## Suporte

Para reportar bugs ou solicitar funcionalidades, abra um ticket em nosso discord:


 [https://discord.com/invite/eA8JusX8tq](https://discord.com/invite/eA8JusX8tq)
