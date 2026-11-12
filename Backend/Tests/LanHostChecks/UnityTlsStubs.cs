// Only the UnityWebRequest type shell is stubbed in this .NET harness.
// HttpClient and ClientWebSocket below are the real production LanTls implementation.
namespace UnityEngine.Networking
{
    public class CertificateHandler : System.IDisposable
    { protected virtual bool ValidateCertificate(byte[] data)=>false; public void Dispose(){} }
    public class UnityWebRequest
    { public string url="";public int redirectLimit;public CertificateHandler? certificateHandler;public bool disposeCertificateHandlerOnDispose; }
}
