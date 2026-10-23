# FinControl Enterprise para Windows

Esta versão substitui as telas antigas do `FinControl.WinForms` por uma interface nativa em C#. Não usa WebView2 nem abre o navegador. Os controles de janela, tabelas, formulários e seletores são Windows Forms.

## Iniciar

### Visual Studio

1. Instale a carga de trabalho **Desenvolvimento para desktop com .NET** e o SDK .NET 8. Como os serviços compartilhados usam ASP.NET Core, mantenha também seus componentes instalados.
2. Abra `FinControl.sln` ou `FinControl.Desktop.slnf`.
3. Defina **FinControl.WinForms** como projeto de inicialização e pressione F5.

### Visual Studio Code

Abra a pasta completa do repositório. Se aparecer o aviso de que WinForms não faz parte de `FinControl.Web.slnf`, use **Alternar Solução** e escolha `FinControl.sln` ou `FinControl.Desktop.slnf`.

No terminal, execute:

```powershell
dotnet run --project FinControl.WinForms
```

Também é possível abrir `INICIAR-WINFORMS.bat`. O arquivo `INICIAR.bat` continua iniciando a versão web.

### Usar sem instalar o SDK

Na aba **Actions** do GitHub, abra uma execução bem-sucedida de **Windows Desktop** e baixe **FinControl-Windows-portatil** na seção Artifacts. É necessário estar conectado ao GitHub para baixar artefatos. Extraia a pasta inteira e abra `FinControl.WinForms.exe`.

O pacote Windows x64 inclui o runtime .NET; não precisa compilar no computador de destino. Permissões e políticas do computador ainda se aplicam. Para gerar o mesmo pacote localmente, use `PUBLICAR-WINDOWS.bat` em um computador com SDK.

## Recursos nativos

- Login e cadastro, alteração de senha, espaços Pessoal/MEI/Empresa e permissões Administrador/Editor/Leitor.
- Painel com cartões financeiros, pendências, gráfico de seis meses e últimos lançamentos.
- Receitas/despesas com filtros corretos, pesquisa, edição, exclusão e confirmação de pagamento.
- Contas e categorias com ativação/desativação e preservação de histórico.
- Orçamentos mensais e metas com valores acumulados e prazos.
- Recorrências semanais, mensais e anuais; geração de pendências e pausa/reativação. Para alterar os dados de uma regra, desative-a e crie outra.
- Relatório por categoria separando valores pagos e pendentes; exportação CSV das tabelas.
- Importação CSV UTF-8/Windows-1252 e OFX de até 2 MB, com prévia e detecção de duplicatas.
- Backup financeiro por espaço e restauração em um espaço novo, sem sobrescrever o atual.
- Temas Claro, Escuro e Sistema, com preferência persistida e resposta à mudança do tema do Windows.

## Dados existentes e funcionamento

O aplicativo inicia o serviço financeiro compartilhado **dentro do próprio processo**, em um endereço `127.0.0.1` com porta atribuída automaticamente. Ele encerra o serviço ao fechar a janela e não aceita conexões de outros computadores. Isso permite reaproveitar integralmente autenticação, permissões e validações da API, mantendo todas as telas nativas.

O banco permanece em `%LOCALAPPDATA%\FinControl\fincontrol.db`, ou na pasta definida por `FINCONTROL_DATA_DIR`. Se já houver um cadastro da versão web, use as mesmas credenciais. No primeiro cadastro após uso do aplicativo antigo, seus registros são vinculados em espaços **Dados anteriores**. O aplicativo explica essa vinculação antes de concluir o cadastro.

Use uma interface por vez no mesmo banco para evitar concorrência entre instâncias dos serviços. A versão Windows impede duas instâncias desktop na mesma sessão. As recorrências são processadas enquanto o aplicativo está aberto e recuperam os vencimentos passados ao iniciar. Não há serviço permanente instalado no Windows.

O backup financeiro não é um backup das contas de acesso: não contém senhas. Guarde o arquivo com cuidado. Ele pode ser restaurado pela versão web ou pela versão Windows.

## Validação

O workflow `.github/workflows/windows-desktop.yml` compila a solução no Windows, executa testes de domínio e API, inicia a interface nativa com um banco temporário e gera capturas reais de controles WinForms nos dois temas. Também testa a renderização dos módulos em janelas de 1280 e 1000 pixels, filtros de receita/despesa, precisão de valores, seleção de categorias e restauração de backup.

Os resultados e as capturas ficam no artefato **WinForms-evidencias**. O teste é acionado com `--smoke-test` e isola o banco para não usar informações reais.

A compilação em Linux consegue validar o código Windows, mas não executar seus controles nativos. A validação visual e a execução nativa dependem do job Windows. Testes automatizados não substituem a conferência em monitores físicos, escalas de DPI diferentes e tecnologias assistivas.
