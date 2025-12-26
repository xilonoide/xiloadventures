using System;
using System.IO;
using XiloAdventures.Engine;
using Xunit;

using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

public class CryptoUtilTests
{
    [Fact]
    public void EncryptToFile_ThenDecrypt_RoundtripsContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"crypto_{Guid.NewGuid():N}.dat");
        const string content = "hello world";

        try
        {
            CryptoUtil.EncryptToFile(tempFile, content, "xas");
            var decrypted = CryptoUtil.DecryptFromFile(tempFile);

            Assert.Equal(content, decrypted);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void DecryptFromFile_WhenPlainTextFile_ReturnsPlainText()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"crypto_plain_{Guid.NewGuid():N}.dat");
        const string content = "plain text content";

        try
        {
            File.WriteAllText(tempFile, content);
            var decrypted = CryptoUtil.DecryptFromFile(tempFile);

            Assert.Equal(content, decrypted);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
