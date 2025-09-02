#region License
/*
Copyright 2024 Dmitrii Evdokimov
Open source software

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

// The most up to date original version by Gérald Barré is available 
// on GitHub: https://github.com/meziantou/Meziantou.Framework/tree/master/src/Meziantou.Framework.Win32.CredentialManager
// NuGet package: https://www.nuget.org/packages/Meziantou.Framework.Win32.CredentialManager/
#endregion

namespace Diev.Extensions.Credentials;

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Win32.SafeHandles;

using static NativeMethods;

public static class CredentialManager
{
    /// <summary>
    /// Get Credential by a mask string.
    /// </summary>
    /// <param name="targetName">String with 'Start *' or 'Name' (Windows)
    /// or 'Host UserName Password' (Linux).</param>
    /// <returns>Credential record (type, host, username?, secret?).</returns>
    /// <exception cref="Exception"></exception>
    public static Credential ReadCredential(string targetName)
    {
        var words = targetName.Split();
        int num = words.Length;

        if (num < 3)
        {
            // WIndows

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (targetName.Contains('*'))
                {
                    if (CredEnumerate(targetName, 0, out int count, out nint pCredentials))
                    {
                        if (count > 1)
                            throw new Exception($"Windows Credential Manager has more '{targetName}' entries ({count}).");

                        nint credential = Marshal.ReadIntPtr(pCredentials, 0);
                        var cred = Marshal.PtrToStructure(credential, typeof(NativeCredential));
                        return ReadFromNativeCredential((NativeCredential)cred!);
                    }

                    throw new Exception($"Windows Credential Manager has no '{targetName}' entries.");
                }

                if (CredRead(targetName, CredentialType.Generic, 0, out nint nCredPtr))
                {
                    using CriticalCredentialHandle critCred = new(nCredPtr);
                    var cred = critCred.GetNativeCredential();
                    return ReadFromNativeCredential(cred);
                }

                throw new Exception($"Windows Credential Manager has no '{targetName}' entries.");
            }

            // Linux

            if (num == 2)
            {
                // "host username"
                return new(CredentialType.Generic, words[0], words[1], null);
            }
            else
            {
                // "host"
                return new(CredentialType.Generic, targetName, null, null);
            }
        }

        // Windows or Linux

        if (num == 3)
        {
            // "host username password"
            return new(CredentialType.Generic, words[0], words[1], words[2]);
        }

        if (num > 3)
        {
            // "some additional values user@name password"
            string pattern = @"(.*)\s(\S*)\s(\S*)";
            var v = Regex.Matches(targetName, pattern)[0].Groups;

            if (v.Count == 4)
            {
                return new(CredentialType.Generic, v[1].Value, v[2].Value, v[3].Value);
            }
        }

        throw new Exception($"Invalid Credential in '{targetName}' entry.");
    }

    public static void WriteCredential(string targetName, string userName, string secret)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new InvalidOperationException("Windows Credential Manager exists in Windows only.");

        NativeCredential credential = new()
        {
            AttributeCount = 0,
            Attributes = nint.Zero,
            Comment = nint.Zero,
            TargetAlias = nint.Zero,
            Type = CredentialType.Generic,
            Persist = (uint)CredentialPersistence.LocalMachine,
            CredentialBlobSize = 0,
            TargetName = Marshal.StringToCoTaskMemUni(targetName),
            CredentialBlob = Marshal.StringToCoTaskMemUni(secret),
            UserName = Marshal.StringToCoTaskMemUni(userName ?? Environment.UserName)
        };

        if (secret is not null)
        {
            byte[] byteArray = Encoding.Unicode.GetBytes(secret);

            if (byteArray.Length > 512 * 5)
                throw new ArgumentOutOfRangeException(nameof(secret), "The secret message has exceeded 2560 bytes.");

            credential.CredentialBlobSize = (uint)byteArray.Length;
        }

        bool written = CredWrite(ref credential, 0);

        Marshal.FreeCoTaskMem(credential.TargetName);
        Marshal.FreeCoTaskMem(credential.CredentialBlob);
        Marshal.FreeCoTaskMem(credential.UserName);

        if (!written)
        {
            int lastError = Marshal.GetLastWin32Error();
            throw new Exception(string.Format("CredWrite failed with the error code {0}.", lastError));
        }
    }

    public static Credential[] EnumerateCrendentials(string? filter = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new InvalidOperationException("Windows Credential Manager exists in Windows only.");

        if (CredEnumerate(filter, 0, out int count, out nint pCredentials))
        {
            Credential[] result = new Credential[count];

            for (int n = 0; n < count; n++)
            {
                nint credential = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(nint)));
                var cred = Marshal.PtrToStructure(credential, typeof(NativeCredential));
                result[n] = ReadFromNativeCredential((NativeCredential)cred!);
            }

            return result;
        }

        int lastError = Marshal.GetLastWin32Error();
        throw new Win32Exception(lastError);
    }

    private enum CredentialPersistence : uint
    {
        Session = 1,
        LocalMachine,
        Enterprise
    }

    private static Credential ReadFromNativeCredential(NativeCredential credential)
    {
        string targetName = Marshal.PtrToStringUni(credential.TargetName)!;
        string? userName = Marshal.PtrToStringUni(credential.UserName);
        string? secret = null;

        if (credential.CredentialBlob != nint.Zero)
        {
            secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        }

        return new Credential(credential.Type, targetName, userName, secret);
    }

    sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        public CriticalCredentialHandle(nint preexistingHandle)
        {
            SetHandle(preexistingHandle);
        }

        public NativeCredential GetNativeCredential()
        {
            if (!IsInvalid)
            {
                var cred = Marshal.PtrToStructure(handle, typeof(NativeCredential));
                return (NativeCredential)cred!;
            }

            throw new InvalidOperationException("Invalid CriticalHandle!");
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                CredFree(handle);
                SetHandleAsInvalid();

                return true;
            }

            return false;
        }
    }
}
