using System.Runtime.InteropServices;
using System.Text;
using DB2ExportService.Models;
using Microsoft.Extensions.Configuration;

namespace DB2ExportService.Configuration;

public class ConfigurationHelper
{
    private readonly IConfiguration _configuration;

    public ConfigurationHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DB2Config GetDB2Config(string configKey = "DB2")
    {
        var config = _configuration.GetSection(configKey).Get<DB2Config>();
        if (config == null)
        {
            throw new InvalidOperationException($"Brak konfiguracji DB2 w sekcji '{configKey}'");
        }

        // Jeśli UseCredentialManager = true, pobierz credentials z Windows Credential Manager
        if (config.UseCredentialManager && !string.IsNullOrEmpty(config.CredentialKey))
        {
            var credential = GetCredentialFromManager(config.CredentialKey);
            if (credential != null)
            {
                config.User = credential.Username;
                config.Password = credential.Password;
            }
        }

        return config;
    }

    public ExportConfig GetExportConfig()
    {
        var config = _configuration.GetSection("ExportConfig").Get<ExportConfig>();
        if (config == null)
        {
            throw new InvalidOperationException("Brak konfiguracji ExportConfig");
        }
        return config;
    }

    public VehicleConfig GetVehicleConfig()
    {
        var config = _configuration.GetSection("VehicleConfig").Get<VehicleConfig>();
        if (config == null)
        {
            throw new InvalidOperationException("Brak konfiguracji VehicleConfig");
        }
        return config;
    }

    private CredentialInfo? GetCredentialFromManager(string targetName)
    {
        try
        {
            var credential = new CREDENTIAL();
            var credPointer = IntPtr.Zero;

            if (CredRead(targetName, CRED_TYPE.GENERIC, 0, out credPointer))
            {
                try
                {
                    credential = Marshal.PtrToStructure<CREDENTIAL>(credPointer);

                    var username = Marshal.PtrToStringUni(credential.UserName) ?? string.Empty;
                    var passwordBytes = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, credential.CredentialBlobSize);
                    var password = Encoding.Unicode.GetString(passwordBytes);

                    return new CredentialInfo
                    {
                        Username = username,
                        Password = password
                    };
                }
                finally
                {
                    CredFree(credPointer);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Błąd podczas odczytu credentials z Windows Credential Manager (klucz: {targetName})", ex);
        }
    }

    #region Windows Credential Manager P/Invoke

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr cred);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    private enum CRED_TYPE : int
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4
    }

    #endregion

    private class CredentialInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
