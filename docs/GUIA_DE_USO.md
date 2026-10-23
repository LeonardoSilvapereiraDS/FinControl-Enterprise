# Guia de uso — FinControl Enterprise

Gestão de finanças pessoais e de caixa para MEIs e empresas. A nova interface web funciona no navegador do computador, tablet e celular. O projeto C# original foi preservado; as camadas Domain, Application e Infrastructure são compartilhadas.

## Aplicativo Windows Forms

Para a nova janela nativa, abra `INICIAR-WINFORMS.bat` ou selecione `FinControl.WinForms` na solução completa. O guia [WINDOWS.md](WINDOWS.md) detalha login, instalação e pacote sem SDK.

## Comece aqui — versão web no Windows

1. Extraia o ZIP para uma pasta, por exemplo `Documentos\FinControl`.
2. Instale o **SDK .NET 8**: https://dotnet.microsoft.com/download/dotnet/8.0. Escolha **SDK**, não apenas Runtime.
3. Abra **INICIAR.bat** com dois cliques. A primeira execução baixa as dependências; mantenha a internet conectada nessa etapa.
4. Acesse **http://localhost:5078**. Se o navegador abrir antes do início, atualize a página.
5. Clique em **Criar conta**, escolha Pessoal, MEI ou Empresa e dê um nome ao espaço.
6. Mantenha a janela do programa aberta. Fechá-la interrompe o acesso; os dados continuam salvos.

Não é preciso contratar servidor, domínio, banco de dados ou serviço de nuvem para usar localmente. O navegador não usa inteligência artificial nem serviço pago para operar o FinControl.

Há também um guia visual em **LEIA-ME.html**, que pode ser aberto diretamente no navegador.

## Usar no celular pela mesma rede

- No computador, feche uma execução anterior e abra **INICIAR-NA-REDE.bat**.
- Conecte computador e celular ao mesmo Wi-Fi de confiança.
- O script exibe os endereços de rede. Localize o IPv4 da conexão ativa, por exemplo `192.168.1.20`.
- No navegador do celular, abra `http://192.168.1.20:5078`, usando o IP real do computador.
- Se o Windows solicitar acesso pelo firewall, permita somente rede privada. Redes corporativas/visitantes podem bloquear comunicação entre dispositivos.
- O computador precisa permanecer ligado. No celular, `localhost` significa o próprio celular, por isso use o IP do computador.

Nesse modo, HTTP não criptografa a comunicação. Use uma rede de confiança. Para acesso pela internet, utilize o procedimento HTTPS opcional em [Hospedagem](HOSPEDAGEM.md); não exponha diretamente a porta 5078 à internet.

## macOS e Linux

Instale o SDK .NET 8, abra um terminal na pasta do projeto e execute:

```bash
bash iniciar.sh
```

Para permitir acesso na rede local:

```bash
FINCONTROL_LISTEN_URL=http://0.0.0.0:5078 bash iniciar.sh
```

Também é possível iniciar em qualquer sistema com:

```bash
dotnet run --project FinControl.Web --urls http://localhost:5078
```

## O que funciona na versão web

- Login por usuário com senha protegida por hash, bloqueio temporário de tentativas e encerramento das outras sessões ao alterar senha.
- Vários espaços separados por usuário: pessoal, MEI ou empresa. O perfil muda a organização inicial de categorias, não aplica regras tributárias.
- Equipe com permissões de administrador, editor e leitor. Para adicionar uma pessoa, ela precisa primeiro criar uma conta nessa mesma instalação.
- Lançamentos com edição, exclusão, receitas/despesas, pago/pendente, pesquisa, filtros, paginação e exportação CSV.
- Contas e categorias com arquivamento/reativação, preservando os registros existentes.
- Saldo consolidado, fluxo mensal, despesas por categoria, orçamentos, metas e relatórios para impressão.
- Contas a pagar/receber: lançamentos pendentes, com identificação de atrasos e confirmação manual.
- Recorrências semanais, mensais e anuais. Dias 29/30/31 são ajustados ao mês sem perder o dia original nos meses seguintes.
- Importação de extratos CSV e OFX, prévia, validações e detecção de duplicatas.
- Backup/restauração JSON pela interface. Restauração cria outro espaço de forma atômica; não sobrescreve o atual. Senhas e membros não são exportados.
- Temas claro, escuro e automático; preferência e mês selecionado salvos por navegador.
- PWA preparada para instalação em navegadores compatíveis em HTTPS ou localhost.

## Comportamentos que você precisa conhecer

**Saldo:** soma todos os saldos iniciais e os lançamentos confirmados de todo o histórico, inclusive contas arquivadas. O resultado do mês considera apenas receitas recebidas e despesas pagas do mês selecionado. Pendências ficam em separado.

**Metas:** o valor reservado é informado manualmente. Não movimenta nem bloqueia dinheiro nas contas.

**Recorrências:** o serviço verifica vencimentos a cada minuto enquanto está aberto e recupera os vencidos ao reiniciar. São gerados lançamentos pendentes, nunca pagamentos automáticos. Pausar interrompe novas ocorrências; reativar retoma a partir do dia atual, sem gerar os períodos pausados. Conta ou categoria arquivada impede novas gerações até a reativação. Excluir um lançamento recorrente não recria sua ocorrência. Para mudar valor/frequência, pause a regra anterior e crie outra.

**CSV:** UTF-8 ou Windows-1252; cabeçalho `data;descricao;valor;id` (`id` opcional). Datas `dd/MM/aaaa` ou `aaaa-MM-dd`. Valores positivos = receitas; negativos = despesas. Até 2 MB/5.000 linhas. O arquivo deve representar movimentações efetivamente realizadas. A categoria de entrada e a de saída são escolhidas antes de importar, e podem ser editadas depois.

**Duplicatas:** o ID do banco, quando presente, é usado dentro da conta selecionada. Sem ID, a chave usa data, descrição, valor e ordem entre linhas idênticas. Confira extratos sobrepostos ou descrições alteradas; formatos sem identificadores não permitem reconhecer todas as duplicatas possíveis. A restauração mantém as chaves de importação.

**OFX:** suporta transações bancárias `STMTTRN`, tanto XML quanto SGML, com `DTPOSTED`, `TRNAMT`, `FITID` e `MEMO`/`NAME`. Não importa posições de investimentos, saldos iniciais ou lançamentos internos de cartão que não estejam nessa estrutura.

**Backup:** limite da interface de 7,5 MB e 30.000 registros por restauração. O backup é financeiro, por espaço; não é uma cópia completa das contas de acesso. Preserve também o diretório de dados para recuperação da instalação completa.

**PWA/offline:** apenas a interface pública é armazenada para carregamento básico. Dados financeiros, senhas e respostas da API não são cacheados. A operação financeira exige conexão com a instalação. Instalar em Android/iPhone depende do navegador e de HTTPS; um endereço HTTP do Wi-Fi pode não oferecer instalação.

## Onde ficam os dados

Por padrão, usa a pasta `FinControl` em `LocalApplicationData` do sistema. No Windows: `%LOCALAPPDATA%\FinControl\fincontrol.db`. A pasta `keys` guarda as chaves das sessões. Não exclua essa pasta ao atualizar.

Para um diretório independente, defina `FINCONTROL_DATA_DIR` antes de iniciar. O banco é SQLite. As migrações são aplicadas automaticamente no início.

Se houver dados da versão Windows anterior e ainda nenhum login web, o primeiro cadastro pede um **código inicial** exibido no terminal para proteger o acesso a esse histórico. Os dados anteriores serão vinculados ao primeiro usuário como espaços adicionais. A nova versão Windows Forms também usa login, espaços e permissões. No cadastro nativo, a vinculação dos dados antigos é explicada e autorizada localmente, sem exigir copiar o código do terminal.

Se não houver dados antigos, o cadastro começa normalmente, sem código. A criação de contas está aberta a quem consegue acessar a instalação; cada nova conta recebe seu próprio espaço.

## Escopo para negócios

O FinControl atende organização financeira e gestão básica de caixa. Não é um ERP completo, sistema contábil ou emissor de nota fiscal. Não calcula tributos, não transmite obrigações, não integra bancos automaticamente e não possui estoque, conciliação automática, folha de pagamento ou auditoria imutável. Cliente, fornecedor, centro de custo e referência podem ser anotados na observação do lançamento.

SQLite e esta primeira arquitetura são adequados a uso pessoal e equipes pequenas. Antes de implantar em empresa com alta concorrência ou requisitos regulatórios, faça uma avaliação de capacidade, autenticação corporativa, auditoria, recuperação e operação. Não foi realizado teste de carga empresarial nem auditoria independente de segurança.

Não há recuperação de senha por e-mail: nenhum provedor de e-mail está configurado. É possível alterar a senha estando autenticado, informando a atual. Guarde suas credenciais.

## Estrutura e desenvolvimento

- `FinControl.Domain`: entidades e regras financeiras.
- `FinControl.Application`: serviços compartilhados por usuário financeiro.
- `FinControl.Infrastructure`: EF Core, SQLite, configurações e migrações.
- `FinControl.Web`: ASP.NET Core, autenticação, verificação de acesso por espaço, importação, recorrências, backup e interface HTML/CSS/JavaScript sem CDN.
- `FinControl.WinForms`: interface Windows nativa com login, temas, módulos financeiros e serviço local compartilhado.
- `FinControl.Tests`: testes xUnit de domínio e serviços.
- `tests/api_smoke.py`: teste integrado contra uma instalação temporária.
- `tests/ui.mjs`: fluxos no Chromium e verificação de largura de 360 a 1440 px.

Abra `FinControl.sln` no Visual Studio; selecione `FinControl.Web` como projeto inicial. Para compilação sem a interface Windows, use `FinControl.Web.slnf` ou o projeto web diretamente.

```bash
dotnet build FinControl.Web/FinControl.Web.csproj
dotnet test FinControl.Tests/FinControl.Tests.csproj
python tests/api_smoke.py --dotnet dotnet
```

Testes de interface requerem Node.js e Playwright (`npm install --no-save playwright` e `npx playwright install chromium`), depois `node tests/ui.mjs --dotnet dotnet`. Nenhuma dependência npm é necessária para executar o aplicativo.

Veja [Validação](VALIDACAO.md) para a evidência de validação e limitações desta entrega. Hospedagem é opcional e está documentada separadamente; nenhum serviço foi contratado ou publicado.
