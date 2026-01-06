# AllianceGuard - Plugin EXILED para SCP:SL

Plugin para integração do painel AllianceGuard com servidores SCP: Secret Laboratory usando EXILED.

## Link do painel:
```
https://alianceguard.apollospace.shop/
```
** sim fiquei com preguiça de fazer um painel de login mais bonito:) **

## Funcionalidades

- Verifica automaticamente o SteamID64 de cada jogador que entra no servidor
- Consulta a API do painel para verificar se o jogador está banido
- Expulsa automaticamente jogadores banidos com mensagem formatada em vermelho
- Detecta contas alternativas de jogadores banidos
- Suporta verificação de contas Steam alternativas cadastradas no painel
- Verificação instantânea sem intervalo de tempo (ao entrar no servidor)

## Instalação

1. **Copiar o DLL para o servidor:**
   - Copie `AlianceGuard.dll` para a pasta `EXILED/Plugins/` do seu servidor



## Como Funciona

1. Quando um jogador entra no servidor, o plugin captura seu SteamID64 imediatamente
2. O plugin faz uma requisição GET para a API do painel
3. A API verifica se o SteamID64 está na lista de infratores ou em contas alternativas
4. Se encontrado, o plugin expulsa o jogador instantaneamente com uma mensagem formatada mostrando o motivo



## Notas Importantes

- A verificação é feita instantaneamente quando o jogador entra, não requer qualquer configuração adicional
- O plugin verifica tanto o SteamID64 principal quanto contas alternativas cadastradas no painel

## Suporte

Para reportar bugs ou solicitar funcionalidades, abra um ticket em nosso discord:

```
https://discord.com/invite/eA8JusX8tq

```
