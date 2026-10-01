# Alterações principais

## Correções no projeto original

- O saldo consolidado preserva o saldo inicial de contas arquivadas, coerentemente com seus lançamentos históricos.
- Os menus Receitas e Despesas da interface Windows aplicam o filtro correspondente.
- Validações de enumerações, datas, centavos e limites monetários; nomes nulos geram validação, não NullReferenceException.
- Uma atualização inválida de transação não modifica parcialmente a descrição do objeto.
- Categorias já utilizadas não podem trocar de tipo e invalidar o histórico.
- Lançamentos existentes podem continuar vinculados a sua própria conta/categoria arquivada durante edição; novos vínculos exigem cadastros ativos.
- As últimas transações do dashboard correspondem ao mês selecionado.
- O menu da versão Windows permite rolagem e o carregamento de nova transação possui tratamento de falha.

## Nova versão web

- Interface responsiva com temas claro, escuro e sistema, navegação mobile, formulários acessíveis, feedback, estados vazios e carregamento.
- Login com hash de senha, sessão HttpOnly/SameSite, proteção contra requisições externas, limitação de tentativas e verificação de permissões no servidor.
- Espaços Pessoal/MEI/Empresa e equipe com três níveis de acesso. A entidade de login é separada do proprietário financeiro para compartilhar serviços originais sem misturar espaços.
- Orçamentos, metas, relatórios, importação, recorrências, configurações e backup/restauração implementados.
- Processamento de recorrência idempotente, importação atômica e restauração em novo espaço.
- SQLite compartilhado com as camadas existentes e migração adicional não destrutiva.
- PWA, instruções de início local e preparação opcional de HTTPS com Docker/Caddy.

O repositório `.git`, arquivos de usuário do Visual Studio, binários de compilação, bases de teste e ferramentas de validação não integram o pacote final. O ZIP original permanece separado.
