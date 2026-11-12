import subprocess,secrets,json,time,socket,ssl,http.client,hashlib,os,shutil,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'Backend/out/lan-host-tests'/secrets.token_hex(6);OUT.mkdir(parents=True);TLS=OUT/'gateway-tls';CONFIG=OUT/'gateway.env';OVERRIDE=OUT/'gateway.override.yaml';COMPOSE=['docker','compose','--project-name','lastaway-lan-check-'+OUT.name,'--project-directory',str(ROOT/'Backend'),'-f',str(ROOT/'Backend/compose.host.yaml'),'--env-file',str(CONFIG),'--profile','lan','-f',str(OVERRIDE)]
checks=[]
def check(name,ok):
 checks.append({'name':name,'status':'PASS' if ok else 'FAIL'});print(checks[-1]['status'],name,flush=True)
 if not ok:raise RuntimeError(name)
def cmd(args):
 r=subprocess.run(args,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 if r.returncode:raise RuntimeError('command failed: '+args[0]+' (see local fixture log)')
 return r.stdout
try:
 if sys.platform!='win32':raise RuntimeError('This fixture checks the Windows host setup')
 for image in ['backend-api:latest','backend-game-server:latest','caddy:2','mongo:7.0.43']:cmd(['docker','image','inspect',image])
 # A random temporary Swagger credential is used only by this isolated stack. Never printed.
 password=secrets.token_urlsafe(24)
 ps="$ErrorActionPreference='Stop';$p=ConvertTo-SecureString $env:LASTAWAY_FIXTURE_PASSWORD -AsPlainText -Force;& './Backend/Deploy/Host/configure-host.ps1' -Mode lan -OutputFile '"+str(CONFIG).replace("'","''")+"' -SwaggerPassword $p"
 import os
 env=os.environ.copy();env['PSModulePath']=os.environ['WINDIR']+'/System32/WindowsPowerShell/v1.0/Modules';env['LASTAWAY_FIXTURE_PASSWORD']=password
 r=subprocess.run(['powershell','-NoProfile','-Command',ps],env=env,cwd=ROOT,capture_output=True,text=True,timeout=60)
 if r.returncode:raise RuntimeError('fixture config failed')
 with CONFIG.open('a',encoding='utf-8') as f:f.write('\nAPI_IMAGE=backend-api:latest\nGAME_IMAGE=backend-game-server:latest\nHOST_MODE=lan\nHOST_LAN_IP=127.0.0.1\n')
 cert=json.loads(cmd(['dotnet','Backend/GameServer/bin/Release/net8.0/GameServer.dll','--lan-cert',str(TLS),'192.168.1.20','7777','18443']))
 OVERRIDE.write_text('''services:
  game-server:
    ports: !override ["127.0.0.1:17777:7777/udp"]
  caddy-lan:
    ports: !override ["127.0.0.1:18443:8443/tcp"]
    volumes: !override
      - "'''+(ROOT/'Backend/Deploy/Host/Caddyfile.lan').as_posix()+''':/etc/caddy/Caddyfile:ro"
      - "'''+TLS.as_posix()+''':/lan-tls:ro"
''',encoding='utf-8')
 # Preflight resolves Compose without printing secrets. All published fixture sockets are loopback.
 effective=json.loads(cmd(COMPOSE+['config','--format','json']))
 check('LAN requires secure private proxy and forbids direct HTTP',effective['services']['api']['environment']['PUBLIC_HOSTING']=='true' and effective['services']['api']['environment']['ALLOW_LOCAL_HTTP']=='false')
 check('only fixture gateway and game port are published on loopback',all(p['host_ip']=='127.0.0.1' for s in effective['services'].values() for p in s.get('ports',[])))
 cmd(COMPOSE+['up','-d','--no-build','mongo','api','game-server','caddy-lan'])
 context=ssl.create_default_context(cafile=str(TLS/'server.crt'));pem=(TLS/'server.crt').read_text();expected=hashlib.sha256(ssl.PEM_cert_to_DER_cert(pem)).hexdigest()
 def api(method,path,body=None,token=None):
  raw=socket.create_connection(('127.0.0.1',18443),timeout=8);secure=context.wrap_socket(raw,server_hostname='192.168.1.20');assert hashlib.sha256(secure.getpeercert(binary_form=True)).hexdigest()==expected
  payload=b'' if body is None else json.dumps(body).encode();headers={'Host':'192.168.1.20:18443','Connection':'close','Content-Length':str(len(payload)),'Content-Type':'application/json'}
  if token:headers['Authorization']='Bearer '+token
  secure.sendall((method+' '+path+' HTTP/1.1\r\n'+''.join(k+': '+v+'\r\n' for k,v in headers.items())+'\r\n').encode()+payload)
  response=http.client.HTTPResponse(secure);response.begin();data=response.read();status=response.status;secure.close()
  try:return status,json.loads(data) if data else None
  except ValueError:return status,None
 for _ in range(20):
  try:
   if api('GET','/health')[0]==200:break
  except (OSError,ssl.SSLError):pass
  time.sleep(1)
 check('real HTTPS gateway health',api('GET','/health')[0]==200)
 user='lan'+secrets.token_hex(5);status,_=api('POST','/api/auth/register',{'username':user,'password':password});check('register through LAN HTTPS',status==201)
 status,login=api('POST','/api/auth/login',{'username':user,'password':password});check('login through LAN HTTPS',status==200 and bool(login.get('accessToken')));token=login['accessToken']
 check('unauthenticated room creation refused',api('POST','/api/rooms',{'name':'Denied','maxPlayers':10})[0]==401)
 status,room=api('POST','/api/rooms',{'name':'LAN TLS fixture','maxPlayers':10},token);check('authenticated room creation through secure proxy',status==201 or status==200)
 check('authenticated map catalog through secure proxy',api('GET','/api/maps',token=token)[0]==200)
 status,ticket=api('POST','/api/game-tickets',{'attemptId':str(__import__('uuid').uuid4()),'roomCode':room['roomCode']},token);check('one-use game ticket through secure LAN proxy',status==200 and bool(ticket.get('ticket')))
 check('game route reaches private WebSocket listener',api('GET','/game')[0]==400)
 check('internal control route is not public',api('GET','/internal/deployment-status')[0]==404)
 check('fixture room cleanup',api('DELETE','/api/rooms/'+room['id'],token=token)[0] in (200,204))
finally:
 try:
  if CONFIG.exists() and OVERRIDE.exists():cmd(COMPOSE+['down','--volumes'])
 except Exception:print('Fixture cleanup needs review',flush=True)
 (OUT/'gateway-checks.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
 CONFIG.unlink(missing_ok=True)
 if TLS.exists():
  assert TLS.resolve().parent==OUT.resolve();shutil.rmtree(TLS)
 print('Evidence: '+str(OUT/'gateway-checks.json'),flush=True)
