using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace XiloAdventures.Engine;

/// <summary>
/// Utilidad para cifrado/descifrado de archivos usando AES.
/// NOTA DE SEGURIDAD: Las claves están embebidas en el código para simplificar el uso.
/// Para aplicaciones en producción, considerar usar el sistema de protección de datos
/// de Windows (DPAPI) o Azure Key Vault para mayor seguridad.
/// </summary>
public static class CryptoUtil
{
    /// <summary>
    /// Clave por defecto para cifrado AES (32 caracteres = 256 bits).
    /// Expuesta públicamente para permitir cifrado personalizado de mundos.
    /// </summary>
    public const string DefaultKeyString = "XiloAdv-Key-1234XiloAdv-Key-1234"; // 32 chars

    /// <summary>
    /// Clave por defecto para cifrado de partidas guardadas (32 caracteres = 256 bits).
    /// Se usa cuando el mundo no especifica una clave de encriptación personalizada.
    /// </summary>
    public const string DefaultSaveKeyString = "XiloAdventuresXiloAdventuresXilo"; // 32 chars

    private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes(DefaultKeyString); // 32 bytes

    /// <summary>
    /// Vector de inicialización (IV) para AES (16 bytes = 128 bits).
    /// </summary>
    private static readonly byte[] Iv = Encoding.UTF8.GetBytes("XiloAdv-IV-12345"); // 16 bytes

    public static void EncryptToFile(string path, string plainText, string extension, string? customKey = null, bool encryptIfEmpty = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var filename = Path.GetFileNameWithoutExtension(path);
        var newPath = Path.Combine(Path.GetDirectoryName(path)!, filename + "." + extension);

        if (!encryptIfEmpty && string.IsNullOrWhiteSpace(customKey))
        {
            File.WriteAllText(newPath, plainText, Encoding.UTF8);
            return;
        }

        using var aes = Aes.Create();
        aes.Key = GetKey(customKey);
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var crypto = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var sw = new StreamWriter(crypto, Encoding.UTF8);
        sw.Write(plainText);
    }

    public static string DecryptFromFile(string path, string? customKey = null, bool throwOnError = false)
    {
        var bytes = File.ReadAllBytes(path);

        try
        {
            using var aes = Aes.Create();
            aes.Key = GetKey(customKey);
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream(bytes);
            using var crypto = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(crypto, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        catch
        {
            if (throwOnError)
                throw;

            // Compatibilidad con ficheros antiguos en texto plano
            return Encoding.UTF8.GetString(bytes);
        }
    }

    /// <summary>
    /// Obtiene la clave efectiva para cifrado/descifrado de partidas guardadas.
    /// Si la clave está vacía, usa la clave por defecto para partidas.
    /// </summary>
    public static string GetEffectiveSaveKey(string? userKey)
    {
        return string.IsNullOrWhiteSpace(userKey) ? DefaultSaveKeyString : userKey.Trim();
    }

    private static byte[] GetKey(string? customKey)
    {
        // Si no hay clave, usamos la por defecto (aunque para guardar partida
        // debería haber una obligatoria, esto mantiene compatibilidad si se usa para otras cosas).
        if (string.IsNullOrWhiteSpace(customKey))
            return DefaultKey;

        var userKey = customKey.Trim();

        // Si tiene más de 8 caracteres, cortamos
        if (userKey.Length > 8)
            userKey = userKey.Substring(0, 8);

        // Padding específico solicitado
        const string padding = "XiloAdventuresXiloAdvent";

        // Concatenamos: clave_usuario + padding
        // Ejemplo: "12345678" + "XiloAdventuresXiloAdvent" = 32 chars
        // Si la clave es más corta, se rellenará con el principio del padding
        var combined = userKey;

        // Rellenar hasta 32 bytes con el string de padding
        int needed = 32 - combined.Length;
        if (needed > 0)
        {
            if (needed > padding.Length)
            {
                // Por seguridad/robustez, si faltaran más de lo que mide el padding (caso raro si key es vacía)
                // repetimos el padding.
                while (combined.Length < 32)
                {
                    combined += padding;
                }
            }
            else
            {
                combined += padding.Substring(0, needed);
            }
        }

        // Aseguramos longitud exacta de 32
        if (combined.Length > 32)
            combined = combined.Substring(0, 32);

        return Encoding.UTF8.GetBytes(combined);
    }
}
