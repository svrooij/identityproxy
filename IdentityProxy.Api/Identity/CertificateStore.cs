using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IdentityProxy.Api.Identity;

internal class CertificateStore : IDisposable
{
    private const int VALID_FROM_MINUTES_ADJUSTMENT = -5;
    private const int VALID_UNTIL_DAYS_ADJUSTMENT = 10;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private X509Certificate2? certificate;

    public X509Certificate2 GetX509Certificate2()
    {
        if (certificate == null)
        {
            semaphore.Wait();
            try
            {
                if (certificate == null)
                {
                    certificate = GenerateCertificate("TokenProxySingingCert");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        return certificate;
    }

    private static X509Certificate2 GenerateCertificate(string subjectCn, int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize); // Generate a new RSA key pair

        var request = new CertificateRequest($"CN={subjectCn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Set key usage
        //request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign, false));

        // Set basic constraints
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        // Set the validity period
        var notBefore = DateTimeOffset.Now.AddMinutes(VALID_FROM_MINUTES_ADJUSTMENT);
        var notAfter = DateTimeOffset.Now.AddMinutes(VALID_UNTIL_DAYS_ADJUSTMENT);

        // Create the certificate
        var cert = request.CreateSelfSigned(notBefore, notAfter);

        // Export the certificate with the private key, then re-import it to generate an X509Certificate2 object
        return new X509Certificate2(cert.Export(X509ContentType.Pfx), "", X509KeyStorageFlags.Exportable);
    }

    public void Dispose()
    {
        if (certificate != null)
        {
            certificate.Dispose();
        }
    }
}
