using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JungleBackend.Hosting;
using JungleBackend.Protocol;
using JungleMaze.App.Config;
using JungleMaze.Infrastructure.Network;

internal static class ClientTlsChecks
{
    public static async Task Run(string directory,Action<bool,string> check)
    {
        LanTls.Configure(Path.Combine(directory,"client"));
        GameServer.Configuration.LanHostTools.EnsureCertificate(directory,"192.168.1.20",7777,8443);
        using var pem=X509Certificate2.CreateFromPemFile(Path.Combine(directory,"server.crt"),Path.Combine(directory,"server.key"));
        using var cert=new X509Certificate2(pem.Export(X509ContentType.Pkcs12));
        // Both sockets bind loopback. A confined CONNECT proxy preserves the advertised private
        // TLS origin while routing only this fixture to loopback; no LAN/firewall setting changes.
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o=>o.Listen(IPAddress.Loopback,0,p=>p.UseHttps(cert)));
        await using var app=builder.Build();app.UseWebSockets();var credentialsReceived=0;
        app.MapGet("/health",()=>Results.Ok(new{ready=true}));
        app.MapPost("/account",()=>{credentialsReceived++;return Results.Ok();});
        app.MapPost("/redirect",()=>Results.Redirect("http://192.168.1.20/account"));
        app.Map("/game",async ctx=>
        {
            using var ws=await ctx.WebSockets.AcceptWebSocketAsync();var bytes=new byte[32];
            var received=await ws.ReceiveAsync(bytes,CancellationToken.None);
            await ws.SendAsync(bytes.AsMemory(0,received.Count),WebSocketMessageType.Binary,true,CancellationToken.None);
        });
        await app.StartAsync();
        var address=new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
        var codeText=LanConnectionCode.Create("192.168.1.20",7777,address.Port,cert.RawData);LanConnectionCode.TryParse(codeText,out var code);
        var config=new ClientConfig(ConnectionMode.LAN,code.Api,code.Ip,code.GamePort,JoinAuthMode.RoomCode,4,5000,"Auto","fixture",code.WebSocket,true,code.CertificateSha256);
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var previous=HttpClient.DefaultProxy;
        var proxy=new WebProxy("http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port){BypassProxyOnLocal=false};
        var proxyTask=AcceptLoop(listener,address.Port,cancel.Token);HttpClient.DefaultProxy=proxy;
        try
        {
            check(!LanTls.IsTrusted(config),"LAN starts unconfirmed");
            var refused=false;try{using var blocked=LanTls.CreateHttpClient(config);}catch(InvalidOperationException){refused=true;}
            check(refused && credentialsReceived==0,"unconfirmed LAN rejects account HTTP before sending");
            using(var probe=LanTls.CreateHttpClient(config,true))using(var health=await probe.GetAsync(code.Api+"/health",cancel.Token))check(health.IsSuccessStatusCode,"exact-pin HTTPS probe succeeds without account data");
            check(credentialsReceived==0 && !LanTls.IsTrusted(config),"health probe alone does not persist trust or send account");
            LanTls.Accept(config);
            using(var client=LanTls.CreateHttpClient(config))
            {
                using var response=await client.PostAsync(code.Api+"/account",new StringContent("synthetic fixture only"),cancel.Token);
                check(response.IsSuccessStatusCode && credentialsReceived==1,"confirmed exact-pin account HTTP succeeds");
                using var redirect=await client.PostAsync(code.Api+"/redirect",new StringContent("synthetic fixture only"),cancel.Token);
                check((int)redirect.StatusCode==302 && credentialsReceived==1,"redirect cannot resend account payload to HTTP");
            }
            using(var ws=new ClientWebSocket())
            {
                ws.Options.Proxy=proxy;LanTls.Attach(ws,new Uri(code.WebSocket),config);
                await ws.ConnectAsync(new Uri(code.WebSocket),cancel.Token);
                await ws.SendAsync(new byte[]{1,2,3}.AsMemory(),WebSocketMessageType.Binary,true,cancel.Token);
                var buffer=new byte[16];var result=await ws.ReceiveAsync(buffer,cancel.Token);
                check(result.Count==3 && buffer[2]==3,"confirmed LAN WebSocket uses same exact pin and echoes binary");
            }
            var wrong=new ClientConfig(ConnectionMode.LAN,code.Api,code.Ip,7777,JoinAuthMode.RoomCode,4,5000,"Auto","fixture",code.WebSocket,true,new string('0',64));
            LanTls.Accept(wrong);var rejected=false;
            try{using var bad=LanTls.CreateHttpClient(wrong);using var response=await bad.PostAsync(code.Api+"/account",new StringContent("must never arrive"),cancel.Token);}
            catch(HttpRequestException){rejected=true;}
            check(rejected && credentialsReceived==1,"wrong advertised fingerprint rejects TLS before account bytes");
            LanTls.Accept(config);
            using var ordinary=LanTls.CreateHttpClient(null);var strict=false;
            try{using var response=await ordinary.GetAsync(code.Api+"/health",cancel.Token);}catch(HttpRequestException){strict=true;}
            check(strict,"non-LAN client still rejects the self-signed certificate");
        }
        finally
        {
            HttpClient.DefaultProxy=previous;cancel.Cancel();listener.Stop();
            try{await proxyTask;}catch(OperationCanceledException){}catch(SocketException){}
            await app.StopAsync();
        }
    }
    private static async Task AcceptLoop(TcpListener listener,int port,CancellationToken ct)
    {
        var tasks=new List<Task>();
        try{while(!ct.IsCancellationRequested)tasks.Add(Tunnel(await listener.AcceptTcpClientAsync(ct),port,ct));}
        finally{try{await Task.WhenAll(tasks);}catch(OperationCanceledException){}catch(IOException){}}
    }
    private static async Task Tunnel(TcpClient socket,int port,CancellationToken ct)
    {
        using(socket)using(var upstream=new TcpClient())
        {
            try
            {
                var stream=socket.GetStream();var header=new List<byte>();var single=new byte[1];
                while(header.Count<4096){var n=await stream.ReadAsync(single,ct);if(n==0)return;header.Add(single[0]);if(header.Count>=4 && header[^4]==13 && header[^3]==10 && header[^2]==13 && header[^1]==10)break;}
                var line=System.Text.Encoding.ASCII.GetString(header.ToArray());
                if(!line.StartsWith("CONNECT 192.168.1.20:"+port+" HTTP/1.1\r\n",StringComparison.Ordinal))return;
                await upstream.ConnectAsync(IPAddress.Loopback,port,ct);var server=upstream.GetStream();
                await stream.WriteAsync("HTTP/1.1 200 Connection Established\r\n\r\n"u8.ToArray(),ct);
                await Task.WhenAny(stream.CopyToAsync(server,ct),server.CopyToAsync(stream,ct));
            }
            catch(Exception e) when(e is OperationCanceledException or IOException or SocketException){}
        }
    }
}
