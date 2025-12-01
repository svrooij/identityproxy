using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IdentityProxy.Api.Identity;

/// <summary>
/// Register the <see cref="CertificateStore"/> as a singleton, we only want one certificate to sign the tokens
/// </summary>
internal class CertificateStore : IDisposable
{
    private const int VALID_FROM_MINUTES_ADJUSTMENT = -5;
    private const int VALID_UNTIL_DAYS_ADJUSTMENT = 10;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private X509Certificate2? certificate;

    /// <summary>
    /// Certificate Store to generate a certificate to sign the tokens
    /// </summary>
    /// <param name="timeProvider"></param>
    public CertificateStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (certificate != null)
        {
            certificate.Dispose();
        }
    }

    /// <summary>
    /// Get the certificate to sign the tokens
    /// </summary>
    /// <remarks>By design the certificate is not stored anywhere, this 'IdentityProxy' is meant to fake tokens during integration tests. And is by no means to ever be exposed to the internet!</remarks>
    public X509Certificate2 GetX509Certificate2()
    {
        // Double-checked locking
        // We only want to generate the certificate once
        if (certificate == null)
        {
            semaphore.Wait();
            try
            {
                if (certificate == null)
                {
                    certificate = GenerateCertificate("TokenProxySingingCert", _timeProvider.GetUtcNow());
                }
            }
            finally
            {
                // Always release the semaphore, to avoid deadlocks
                semaphore.Release();
            }
        }
        // Return a copy of the certificate, since the certificate is not thread-safe and we don't want to dispose the certificate
        return new X509Certificate2(certificate);
    }

    private static X509Certificate2 GenerateCertificate(string subjectCn, DateTimeOffset now, int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize); // Generate a new RSA key pair

        var request = new CertificateRequest($"CN={subjectCn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Set key usage
        //request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign, false));

        // Set basic constraints
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        // Set the validity period
        var notBefore = now.AddMinutes(VALID_FROM_MINUTES_ADJUSTMENT);
        var notAfter = now.AddDays(VALID_UNTIL_DAYS_ADJUSTMENT);

        // Create the certificate
        var cert = request.CreateSelfSigned(notBefore, notAfter);

        // Export the certificate with the private key, then re-import it to generate an X509Certificate2 object
        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), "", X509KeyStorageFlags.Exportable);
    }
}
