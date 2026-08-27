using System.Security.Cryptography;

namespace LeipzigCraft.Launcher.Services;

public static class ManifestSignatureService
{
    private const string PrimaryPublicKeyPem = """
-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA3UJhSIhgsSxrhEmM/j1B
QmgaD2LNdScpQyI54LeTMXajJjCcfHH/sVWO8XuVoJGM3/cqjKK4ZvYOA+XBF9bq
MWHZUGy9Nq6XsJMy50225kMNxSDMiK66qc5zh1rg8U6a170N23rncvPoEe3nsCz4
g/SRz+pEWW26hdKOTHO9m8tj9IO8x4RwsEzqCEeMe8jRqNS5iIVaH1Ot1SG3Y2iD
/dX9SK6U4+Rz0KNrM77iqAFMqVt2f84tWwi/Yj+CykBR6mOZmxm4w/CIgnIaJObw
k1O3DqP82cZMVlzJPEZuL3E5pHKwxTXSz5HpGG86WP1MIRJAvg/2Zo8WNejOJIev
/YXOMTpi3ppzO8FNYk0QQz9sVV74gBtzIrMVLI5IFY3IE2vG6OcYP5NMF8fUqgKy
IRDhO45KXWO8TvGWL8eMqntOyknlQ/WpOL5Mt53SqvJc7fftNb2cgMYrnjIGnGzh
6NEiw1MkD5Qa8zbF17aIyzPI9VRY1VgvDKfxoMWwj0tzAgMBAAE=
-----END PUBLIC KEY-----
""";

    private const string RecoveryPublicKeyPem = """
-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAomqpeuFH359j2B6y48pl
4avKL5LTEXEbE1V89MydJrObgr0db5cVArmDMPUUp1b0sFgucik3vX657Wo4ibK4
qvbeHmgB482u067SraMyjNtbsCkV5T56YuMD4eEbs2OqhU4oxT96N/61D//paK3Q
ts3ttznhdy9YMlUaPP2IwaViotSTqnopRmlKz8/ZwN8SPZP5dV3V4spGodUsiJTH
YQDG1qpdk00qGO9dEiIIOhI76Uwixovj6hdCJ2wRPqDUi99OJa8PP0yrCqJN/Hrz
UuSXwMNkVoAINw9vkHaLKbVEaOq1vqExZ9X6TbuP9xaeiKu/LX1m1HvtaglM4V4k
YDrU4P1t7BP26Z29BbOvayiGWEgQHa7KQkBp+LgAG8BD+GwV7eGGfausgY/sALg4
tq+DNV5/KYpQizf+4BgNUILF3EK2IBVYKzVoZ819FyS98FLCYhWvucl8QTgZ2J9Z
lNrnVfQDk9ok9LSxnLYvh4DwojDQBCVqr5X2MSVsWH0ZAgMBAAE=
-----END PUBLIC KEY-----
""";

    public static bool Verify(
        byte[] manifestBytes,
        string base64Signature,
        out string verifiedBy)
    {
        verifiedBy = "";

        if (manifestBytes.Length == 0 ||
            string.IsNullOrWhiteSpace(base64Signature))
        {
            return false;
        }

        byte[] signature;

        try
        {
            signature = Convert.FromBase64String(
                base64Signature.Trim());
        }
        catch
        {
            return false;
        }

        if (VerifyWithKey(
                manifestBytes,
                signature,
                PrimaryPublicKeyPem))
        {
            verifiedBy = "primary";
            return true;
        }

        if (VerifyWithKey(
                manifestBytes,
                signature,
                RecoveryPublicKeyPem))
        {
            verifiedBy = "recovery";
            return true;
        }

        return false;
    }

    private static bool VerifyWithKey(
        byte[] data,
        byte[] signature,
        string publicKeyPem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            return rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}
