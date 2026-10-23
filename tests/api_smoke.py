"""Teste real da API em banco temporário. Execute após dotnet build FinControl.Web.
python tests/api_smoke.py --dotnet dotnet
"""
import argparse, http.cookiejar, json, os, pathlib, socket, subprocess, tempfile, time, urllib.request, urllib.error
root = pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--dotnet',default='dotnet');args=parser.parse_args()
checks=0

def check(condition, label):
 global checks
 if not condition: raise AssertionError(label)
 checks+=1;print('OK',label,flush=True)

with tempfile.TemporaryDirectory(prefix='fincontrol-api-') as temp:
 with socket.socket() as probe: probe.bind(('127.0.0.1',0));port=probe.getsockname()[1]
 base=f'http://127.0.0.1:{port}'
 log=open(pathlib.Path(temp)/'server.log','w+')
 env={**os.environ,'FINCONTROL_DATA_DIR':temp,'DOTNET_CLI_TELEMETRY_OPTOUT':'1','Logging__LogLevel__Default':'Warning'}
 proc=subprocess.Popen([args.dotnet,str(root/'FinControl.Web/bin/Debug/net8.0/FinControl.Web.dll'),'--urls',base],cwd=root/'FinControl.Web',env=env,stdout=log,stderr=log)
 def client(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
 a,b,anonymous=client(),client(),client()
 def request(c,path,method='GET',body=None,expected=200,csrf=True):
  headers={'Content-Type':'application/json'}
  if csrf: headers['X-FinControl']='1'
  req=urllib.request.Request(base+'/api'+path,method=method,headers=headers,data=None if body is None else json.dumps(body).encode())
  try:
   response=c.open(req);code=response.status;raw=response.read()
  except urllib.error.HTTPError as exc: code=exc.code;raw=exc.read()
  data=json.loads(raw) if raw else None
  if code!=expected: raise AssertionError(f'{method} {path}: {code}, expected {expected}: {data}')
  return data
 try:
  for i in range(150):
   try:
    urllib.request.urlopen(base+'/health',timeout=.3);break
   except (OSError,urllib.error.URLError): time.sleep(.1)
  else: raise RuntimeError('API did not start')
  check(request(a,'/session')['user'] is None,'anonymous session has no data')
  request(anonymous,'/workspaces','POST',{'nome':'No','perfil':'Pessoal'},403,False);check(True,'CSRF header enforced')
  creds={'nome':'QA Admin','email':'admin@example.test','senha':'Teste seguro 2026!','perfil':'Empresa','espaco':'Empresa QA'}
  request(a,'/auth/register','POST',creds);sess=request(a,'/session');wid=sess['workspaces'][0]['id'];w=f'/w/{wid}'
  check(sess['user']['nome']=='QA Admin','registration and authenticated session')
  state=request(a,w+'/state?month=2026-09');account=state['accounts'][0]['id'];income=next(c['id'] for c in state['categories'] if c['tipo']==1);expense=next(c['id'] for c in state['categories'] if c['tipo']==2)
  check(len(state['categories'])>=8,'business workspace categories seeded')
  request(anonymous,w+'/state?month=2026-09',expected=401);check(True,'anonymous cannot read finances')
  request(a,w+'/accounts/'+str(account),'PUT',{'nome':'Caixa','tipoConta':3,'saldoInicial':1000})
  payload={'descricao':'Venda QA','valor':500.25,'data':'2026-09-15','tipo':1,'categoriaId':income,'contaId':account,'observacao':None,'pago':True,'recorrente':False}
  sale=request(a,w+'/transactions','POST',payload)
  request(a,w+'/transactions','POST',{**payload,'descricao':'Despesa QA','tipo':2,'categoriaId':expense,'valor':100.10})
  pending=request(a,w+'/transactions','POST',{**payload,'descricao':'A receber','pago':False,'valor':50})
  state=request(a,w+'/state?month=2026-09');check(state['dashboard']['saldoGeral']==1400.15,'balance uses decimal and excludes pending')
  request(a,w+'/accounts/'+str(account),'PATCH',{'ativa':False},204)
  state=request(a,w+'/state?month=2026-09');check(state['dashboard']['saldoGeral']==1400.15,'archiving preserves consolidated balance')
  request(a,w+'/transactions/'+str(sale['id']),'PUT',{**payload,'descricao':'Venda corrigida'})
  check(True,'existing transaction editable on archived account')
  request(a,w+'/accounts/'+str(account),'PATCH',{'ativa':True},204)
  for patch in [{'tipo':99},{'valor':1.001},{'valor':0},{'categoriaId':expense},{'descricao':None},{'data':'0001-01-01'}]:request(a,w+'/transactions','POST',{**payload,**patch},400)
  check(True,'invalid enums, money, dates and categories rejected')
  request(a,w+'/categories/'+str(income),'PUT',{'nome':'Vendas','tipo':2},400);check(True,'used category type protected')
  request(a,w+'/budgets','POST',{'categoriaId':expense,'valorLimite':300,'mes':9,'ano':2026})
  request(a,w+'/budgets','POST',{'categoriaId':expense,'valorLimite':400,'mes':9,'ano':2026},409)
  goal=request(a,w+'/goals','POST',{'nome':'Reserva','descricao':None,'valorObjetivo':1000,'valorAtual':250,'dataInicio':'2026-09-01','dataLimite':'2026-12-31'})
  check(goal['percentualProgresso']==25,'goal progress and unique monthly budgets')
  csv='data;descricao;valor;id\n2026-09-10;Cliente CSV;350,25;bank-001\n10/09/2026;Fornecedor CSV;-50,25;bank-002\n'
  imp={'conteudo':csv,'formato':'csv','contaId':account,'categoriaReceitaId':income,'categoriaDespesaId':expense,'confirmar':False}
  preview=request(a,w+'/import','POST',imp);check(preview['novos']==2,'CSV preview without writes')
  imported=request(a,w+'/import','POST',{**imp,'confirmar':True});again=request(a,w+'/import','POST',{**imp,'confirmar':True});check(imported['novos']==2 and again['novos']==0 and again['duplicados']==2,'CSV import is idempotent')
  ofx='<OFX><BANKTRANLIST><STMTTRN><DTPOSTED>20260912120000[-3:BRT]<TRNAMT>-20.30<FITID>ofx-01<MEMO>Compra OFX</STMTTRN></BANKTRANLIST></OFX>'
  result=request(a,w+'/import','POST',{**imp,'conteudo':ofx,'formato':'ofx','confirmar':True});check(result['novos']==1,'OFX SGML import')
  # Uma recorrência passada com fim definido evita dependência da data do teste.
  rule=request(a,w+'/recurring','POST',{'descricao':'Mensal 31','valor':10,'tipo':2,'categoriaId':expense,'contaId':account,'frequencia':'Mensal','inicio':'2026-01-31','fim':'2026-03-31'})
  request(a,w+'/recurring/generate','POST',{});state=request(a,w+'/state?month=2026-09');generated=[t for t in state['transactions'] if t['descricao']=='Mensal 31'];check(sorted(t['data'][:10] for t in generated)==['2026-01-31','2026-02-28','2026-03-31'],'recurrence preserves anchor day and prevents duplicates')
  check(all(not t['pago'] for t in generated),'recurrences remain pending')
  request(b,'/auth/register','POST',{**creds,'nome':'QA Reader','email':'reader@example.test','espaco':'Pessoal QA','perfil':'Pessoal'})
  request(b,w+'/state?month=2026-09',expected=404);check(True,'separate users cannot access another workspace')
  request(a,w+'/members','POST',{'email':'reader@example.test','papel':'Leitor'})
  reader=request(b,w+'/state?month=2026-09');check(reader['role']=='Leitor','shared reader access')
  request(b,w+'/transactions','POST',payload,403);request(b,w+'/backup',expected=403);check(True,'reader cannot mutate or export administrative backup')
  members=request(a,w+'/members');admin=next(m for m in members if m['email']=='admin@example.test')
  request(a,w+'/members/'+str(admin['id']),'DELETE',expected=400);check(True,'last administrator cannot be removed')
  backup=request(a,w+'/backup');check('senha' not in json.dumps(backup).lower(),'backup excludes passwords')
  restored=request(a,'/restore','POST',backup);rwid=restored['id'];state2=request(a,f'/w/{rwid}/state?month=2026-09');check(len(state2['transactions'])==len(state['transactions']) and state2['dashboard']['saldoGeral']==state['dashboard']['saldoGeral'],'backup restored atomically in separate workspace')
  ai=state2['accounts'][0]['id'];ci=next(c['id'] for c in state2['categories'] if c['tipo']==1);ce=next(c['id'] for c in state2['categories'] if c['tipo']==2)
  repeated=request(a,f'/w/{rwid}/import','POST',{**imp,'contaId':ai,'categoriaReceitaId':ci,'categoriaDespesaId':ce});check(repeated['novos']==0,'restored backup retains import duplicate detection')
  before=len(request(a,'/session')['workspaces']);request(a,'/restore','POST',{**backup,'transacoes':[{**backup['transacoes'][0],'categoriaId':999999}]},400);after=len(request(a,'/session')['workspaces']);check(before==after,'invalid backup rolls back entire restore')
  request(a,'/auth/password','PUT',{'senhaAtual':creds['senha'],'novaSenha':'Outra frase segura 2026!'});request(a,'/auth/logout','POST',{},204);request(a,'/auth/login','POST',{'email':creds['email'],'senha':creds['senha']},401);request(a,'/auth/login','POST',{'email':creds['email'],'senha':'Outra frase segura 2026!'});check(True,'password change, logout and login')
  print(f'PASS: {checks} API checks',flush=True)
 except Exception:
  log.flush();log.seek(0);print(log.read()[-7000:]);raise
 finally:
  proc.terminate()
  try:proc.wait(timeout=5)
  except subprocess.TimeoutExpired:
   proc.kill();proc.wait(timeout=5)
  log.close()
