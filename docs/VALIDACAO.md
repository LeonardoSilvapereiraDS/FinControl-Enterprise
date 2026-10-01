# Validação da entrega

Validação realizada em 01/10/2026 com .NET SDK 8.0.419, EF Core/SQLite 8.0.25 e Chromium Headless 131. Os dados e contas usados foram fictícios, em bancos temporários, separados dos dados do usuário.

## Resultados

| Verificação | Resultado |
|---|---|
| Compilação da solução completa, incluindo Web, WinForms e Tests | Concluída, sem erros ou avisos reportados |
| Testes de domínio e serviços (inclui os testes originais) | 21 casos aprovados |
| Testes reais da API HTTP/SQLite | 25 verificações aprovadas |
| Fluxos de navegador e responsividade | 61 verificações aprovadas |
| Erros JavaScript não tratados nos fluxos testados | Nenhum |

O executor VSTest padrão falhou internamente no ambiente de validação. Os 21 casos xUnit foram executados em processo por um executor de reflexão, incluindo os dados das theories e as mesmas asserções originais. Não se afirma que o comando `dotnet test` tenha concluído neste ambiente. O projeto mantém sua configuração xUnit padrão para execução local.

## O que foi verificado na API

- Cadastro/login, troca de senha, logout, cabeçalho de proteção de requisições e bloqueio de leitura anônima.
- Separação de dados entre usuários e espaços; compartilhamento com leitor; impedimento de alteração e de backup administrativo pelo leitor.
- Preservação de pelo menos um administrador no espaço.
- Saldos em decimal, exclusão de pendências do saldo e preservação do saldo ao arquivar conta.
- Edição de lançamento existente em conta arquivada.
- Rejeição de valores negativos/zero em transações, frações de centavo, enumerações inválidas, categorias incompatíveis, descrição nula e data inválida.
- Impedimento de troca de tipo de categoria já utilizada.
- Orçamento único por categoria/mês e progresso de meta.
- Prévia/importação CSV, repetição da importação sem duplicar registros e leitura de OFX SGML.
- Recorrência mensal em 31/01 → 28/02 → 31/03, com pendências e sem duplicatas.
- Backup sem senhas, restauração em espaço novo, preservação das chaves de importação e rollback integral de backup inválido.

## O que foi verificado no navegador

- Cadastro pela interface e criação de lançamento com moeda brasileira (`150,25`).
- Alternância de tema e persistência ao recarregar.
- Dez telas nas larguras de 360, 390, 768, 1024 e 1440 pixels, sem rolagem horizontal da página.
- Formulário de lançamento no celular sem transbordamento horizontal.
- Menu mobile fora da tela quando fechado, abertura e navegação.
- Tema no celular, fechamento de diálogo pelo teclado e conteúdo cadastrado renderizado como texto, sem executar HTML.
- Capturas reais em claro/escuro, desktop e celular. Disponíveis em `docs/imagens`.

Durante a validação foram corrigidos o fallback que interceptava arquivos estáticos e o conflito entre o campo de formulário chamado `id` e a propriedade `id` do elemento form.

## Limites da validação

- Testes de telas são simulações no Chromium; não foram executados em aparelhos físicos, Safari/iOS ou todos os navegadores possíveis.
- O projeto Windows foi compilado, mas sua interface nativa não foi executada neste ambiente Linux. A interface profissional, os temas e os novos módulos são da versão web.
- Docker/Caddy foram preparados como implantação opcional; não houve contratação de servidor, deploy público, emissão real de certificado HTTPS nem ensaio de recuperação de volume Docker.
- A PWA tem manifesto, ícones e service worker, mas a instalação depende do navegador e do contexto HTTPS/localhost. Dados financeiros não funcionam offline.
- Não houve teste de carga empresarial, auditoria independente de segurança ou validação de todos os formatos de extrato bancário.

## Reproduzir

Os scripts estão em `tests/api_smoke.py` e `tests/ui.mjs`; as instruções estão no README. Eles iniciam um servidor em banco temporário. O aplicativo não vem com dados de demonstração inseridos na base de uso real.
