# Hospedagem com HTTPS — opcional

Você não precisa seguir este guia para usar o FinControl no seu computador. `INICIAR.bat` é a opção inicial recomendada. Esta configuração foi preparada, mas não foi publicada nem teve um certificado público emitido nesta entrega.

## O que muda com hospedagem

Um computador remoto permanece ligado, executa o aplicativo e guarda o banco. Os dispositivos acessam o mesmo endereço HTTPS. Para isso, é necessário escolher um servidor com Docker Compose, um domínio e uma rotina de backup. Nenhum serviço foi contratado e não há conta de nuvem vinculada ao projeto.

Peça ajuda técnica para a primeira publicação. Não basta subir apenas a pasta `wwwroot`: ela é a interface; o ASP.NET Core precisa estar funcionando junto.

## Configuração incluída

- `Dockerfile`: compila o projeto e executa como o usuário não privilegiado `app`.
- `deploy/compose.yml`: aplicação, proxy Caddy e volumes persistentes. Só o Caddy publica as portas 80/443.
- `deploy/Caddyfile`: proxy para o aplicativo e HTTPS automático para o domínio configurado.
- O diretório `/data` contém SQLite e chaves de sessão; é mantido em volume.
- O aplicativo confia nos cabeçalhos de proxy apenas do IP do Caddy configurado, não de qualquer cliente.

### Passos para quem administra o servidor

1. Instale Docker Engine e Docker Compose no servidor. Crie um registro DNS A/AAAA para o domínio apontando para esse servidor. As portas 80 e 443 precisam estar acessíveis para o Caddy emitir/renovar certificados.
2. Copie o projeto, sem dados de teste, para o servidor.
3. Na pasta `deploy`, copie `.env.example` para `.env` e troque o domínio de exemplo pelo endereço real.
4. Confira se a rede `172.29.88.0/24` não conflita com redes existentes. Se alterá-la, ajuste também os IPs e `FINCONTROL_TRUSTED_PROXY` em conjunto.
5. Execute a partir da pasta `deploy`:

```bash
docker compose config
docker compose up -d --build
docker compose logs -f app caddy
```

6. Acesse `https://SEU-DOMINIO` e crie uma conta. A criação de contas é aberta a quem acessa o endereço. Cada pessoa recebe um espaço próprio; adicionar membros a um espaço existente exige administrador.
7. Valide criação de lançamento, login/logout, backup, restauração e instalação da PWA no dispositivo que será usado.
8. Configure backups do volume e teste recuperação. Para uma cópia integral simples, interrompa a aplicação durante a cópia do diretório de dados; copiar um SQLite aberto sem considerar WAL pode produzir backup inconsistente. O backup JSON da interface é a alternativa por espaço.

**Não remova o volume `financial-data` ao atualizar.** Para atualizar código, reconstrua as imagens mantendo os volumes.

## Antes de uso empresarial real

Esta entrega não substitui uma avaliação de implantação. Não inclui recuperação de senha por e-mail, verificação de propriedade de e-mail, 2FA, monitoramento gerenciado, auditoria imutável, alta disponibilidade ou teste de carga. Em produção, mantenha .NET/imagens atualizados, defina retenção e recuperação de backup e restrinja o acesso à rede ou VPN quando adequado.

A configuração é destinada a uma única instância do aplicativo com SQLite. Não adicione réplicas apontando para o mesmo arquivo SQLite.

Referências oficiais:

- https://caddyserver.com/docs/automatic-https
- https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-8.0
- https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Making_PWAs_installable
