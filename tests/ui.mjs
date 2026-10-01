/** Fluxos de navegador e responsividade. Instale playwright e Chromium antes de executar.
 * node tests/ui.mjs --dotnet dotnet
 * FINCONTROL_CHROME permite escolher um Chromium já instalado.
 */
import {spawn} from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const {chromium}=await import(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES?path.join(process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES,'playwright/index.mjs'):'playwright');
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const di=process.argv.indexOf('--dotnet');const dotnet=di>=0?process.argv[di+1]:'dotnet';
const temp=fs.mkdtempSync(path.join(os.tmpdir(),'fincontrol-ui-')),port=5179;
const server=spawn(dotnet,[path.join(root,'FinControl.Web/bin/Debug/net8.0/FinControl.Web.dll'),'--urls',`http://127.0.0.1:${port}`],{cwd:path.join(root,'FinControl.Web'),env:{...process.env,FINCONTROL_DATA_DIR:temp,DOTNET_CLI_TELEMETRY_OPTOUT:'1',Logging__LogLevel__Default:'Warning'}});
let logs='';server.stdout.on('data',d=>logs+=d);server.stderr.on('data',d=>logs+=d);
let browser,page;let count=0;const errors=[];
const check=(yes,msg)=>{if(!yes)throw new Error(msg);console.log('OK '+msg);count++;};
const output=process.env.FINCONTROL_SCREENSHOTS||path.join(temp,'screenshots');fs.mkdirSync(output,{recursive:true});
try{
 for(let i=0;i<150;i++){try{if((await fetch(`http://127.0.0.1:${port}/health`)).ok)break;}catch{}await new Promise(r=>setTimeout(r,100));}
 browser=await chromium.launch({headless:true,executablePath:process.env.FINCONTROL_CHROME||undefined,args:['--no-sandbox']});
 const ctx=await browser.newContext({viewport:{width:1440,height:1000},timezoneId:'America/Sao_Paulo',colorScheme:'light',locale:'pt-BR'});page=await ctx.newPage();
 page.on('pageerror',err=>{errors.push(err.message);console.log('PAGE ERROR',err.message)});page.on('console',m=>{if(m.type()==='error')console.log('CONSOLE',m.text())});
 await page.goto(`http://127.0.0.1:${port}`);await page.getByRole('button',{name:'Criar conta',exact:true}).click();
 await page.locator('[name=nome]').fill('Equipe Aurora');await page.locator('[name=email]').fill('aurora@example.test');await page.locator('[name=senha]').fill('Minha frase segura 2026!');await page.locator('[name=perfil]').selectOption('Empresa');await page.locator('[name=espaco]').fill('Aurora Studio');await page.getByRole('button',{name:'Criar minha conta'}).click();
 await page.getByRole('heading',{name:'Seu financeiro, com clareza.'}).waitFor();check(true,'registration through actual browser');
 await page.locator('[data-action=new-transaction]').first().click();await page.locator('#transaction-form [name=descricao]').fill('Venda pelo formulário');await page.locator('#transaction-form [name=tipo]').selectOption('1');await page.locator('#transaction-form [name=valor]').fill('150,25');await page.locator('#transaction-form [name=data]').fill('2026-09-30');await page.locator('#transaction-form [type=submit]').click();await page.locator('#modal').waitFor({state:'hidden'});check(true,'transaction form accepts Brazilian currency');
 await page.evaluate(async()=>{
  const api=async(p,body,method='POST')=>{const r=await fetch('/api'+p,{method,headers:{'Content-Type':'application/json','X-FinControl':'1'},body:body===undefined?undefined:JSON.stringify(body)});if(!r.ok)throw new Error(await r.text());return r.status===204?null:r.json();};
  const session=await api('/session',undefined,'GET'),wid=session.workspaces[0].id,w='/w/'+wid,state=await api(w+'/state?month=2026-09',undefined,'GET');const a=state.accounts[0].id,cats=state.categories,income=cats.find(c=>c.tipo===1).id,expenses=cats.filter(c=>c.tipo===2);
  await api(w+'/accounts/'+a,{nome:'Conta principal',tipoConta:1,saldoInicial:12500},'PUT');
  for(let m=4;m<=9;m++){
   await api(w+'/transactions',{descricao:'Serviços de design',valor:6400+m*150,tipo:1,data:`2026-0${m}-05`,categoriaId:income,contaId:a,observacao:'Dados fictícios para teste visual',pago:true,recorrente:false});
   for(let i=0;i<4;i++)await api(w+'/transactions',{descricao:['Aluguel do escritório','Plataformas e ferramentas','Materiais de produção','Campanha de divulgação'][i],valor:[1850,429.90,670,340][i]+m*15,tipo:2,data:`2026-0${m}-${[10,12,18,22][i]}`,categoriaId:expenses[i].id,contaId:a,observacao:null,pago:true,recorrente:false});
  }
  await api(w+'/transactions',{descricao:'Projeto identidade visual',valor:1800,tipo:1,data:'2026-09-30',categoriaId:income,contaId:a,observacao:'Cliente pendente',pago:false,recorrente:false});
  for(let i=0;i<3;i++)await api(w+'/budgets',{categoriaId:expenses[i].id,valorLimite:[2500,900,1500][i],mes:9,ano:2026});
  await api(w+'/goals',{nome:'Reserva do negócio',descricao:'Seis meses de despesas fixas',valorObjetivo:30000,valorAtual:12400,dataInicio:'2026-01-01',dataLimite:'2027-06-01'});
 });
 await page.locator('#month').fill('2026-09');await page.locator('#month').dispatchEvent('change');await page.getByText('Projeto identidade visual',{exact:true}).waitFor();
 await page.locator('.toast').waitFor({state:'hidden'});await page.screenshot({path:path.join(output,'desktop-claro.png'),fullPage:true,animations:'disabled'});
 await page.locator('[data-action=theme]').click();await page.screenshot({path:path.join(output,'desktop-escuro.png'),fullPage:true,animations:'disabled'});check(await page.locator('html').getAttribute('data-theme')==='dark','dark mode');await page.reload();await page.getByRole('heading',{name:'Seu financeiro, com clareza.'}).waitFor();check(await page.locator('html').getAttribute('data-theme')==='dark','theme persists on reload');await page.locator('[data-action=theme]').click();
 for(const size of [360,390,768,1024,1440]){
  await page.setViewportSize({width:size,height:size<700?844:1000});
  for(const route of ['dashboard','transactions','accounts','categories','budgets','goals','recurring','reports','import','settings']){
   await page.evaluate(r=>location.hash=r,route);await page.locator('#breadcrumb-title').filter({hasText:({dashboard:'Visão geral',transactions:'Lançamentos',accounts:'Contas e carteiras',categories:'Categorias',budgets:'Orçamentos',goals:'Metas financeiras',recurring:'Recorrências',reports:'Relatórios',import:'Importar extrato',settings:'Configurações'})[route]}).waitFor();
   await page.waitForTimeout(30);
   const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth+1);check(!overflow,`${route} has no page overflow at ${size}px`);
  }
 }
 await page.setViewportSize({width:390,height:844});await page.evaluate(()=>location.hash='dashboard');await page.getByRole('heading',{name:'Seu financeiro, com clareza.'}).waitFor();await page.waitForTimeout(250);check(await page.locator('.sidebar').evaluate(el=>el.getBoundingClientRect().right<=1),'closed mobile menu stays outside viewport');await page.screenshot({path:path.join(output,'celular-claro.png'),fullPage:false,animations:'disabled'});
 await page.locator('[data-action=new-transaction]').first().click();check(await page.locator('#transaction-form').isVisible(),'transaction dialog opens on phone');
 const modalOverflow=await page.locator('#modal').evaluate(el=>el.scrollWidth>el.clientWidth+1);check(!modalOverflow,'mobile form fits screen');await page.keyboard.press('Escape');await page.locator('[data-action=menu]').click();check(await page.locator('.shell').evaluate(el=>el.classList.contains('menu-open')),'mobile navigation opens');await page.getByRole('link',{name:'Configurações',exact:true}).click();await page.getByRole('heading',{name:'Um espaço com a sua cara.'}).waitFor();await page.getByRole('button',{name:'Escuro',exact:true}).click();check(await page.locator('html').getAttribute('data-theme')==='dark','phone theme control');
 await page.evaluate(()=>location.hash='dashboard');await page.getByRole('heading',{name:'Seu financeiro, com clareza.'}).waitFor();await page.waitForTimeout(250);await page.screenshot({path:path.join(output,'celular-escuro.png'),fullPage:false,animations:'disabled'});
 // Stored strings must remain text, never HTML.
 await page.locator('[data-action=new-transaction]').first().click();await page.locator('#transaction-form [name=descricao]').fill('<img src=x onerror=alert(1)>');await page.locator('#transaction-form [name=valor]').fill('1,25');await page.locator('#transaction-form [type=submit]').click();await page.locator('#modal').waitFor({state:'hidden'});check(await page.locator('img[src=x]').count()===0,'transaction descriptions are HTML escaped');
 check(errors.length===0,'no uncaught JavaScript errors: '+errors.join('; '));console.log(`PASS: ${count} browser checks. Screenshots: ${output}`);
} catch(err){if(page){console.error('BODY',await page.locator('body').innerText());await page.screenshot({path:path.join(output,'failure.png'),fullPage:true,animations:'disabled'});}console.error(logs.slice(-4000));throw err;}finally{await browser?.close();server.kill();}
