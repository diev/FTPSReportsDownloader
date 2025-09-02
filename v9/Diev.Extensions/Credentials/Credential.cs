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
#endregion

namespace Diev.Extensions.Credentials;

public enum CredentialType
{
    Generic = 1,
    DomainPassword,
    DomainCertificate,
    DomainVisiblePassword,
    GenericCertificate,
    DomainExtended,
    Maximum,
    MaximumEx = Maximum + 1000,
}

/// <summary>
/// Windows Credential Manager credential
/// </summary>
/// <param name="CredentialType"></param>
/// <param name="TargetName"></param>
/// <param name="UserName"></param>
/// <param name="Password"></param>
public record Credential(CredentialType CredentialType, string TargetName, string? UserName, string? Password)
{
    public override string ToString()
        => $"CredentialType: {CredentialType}, TargetName: {TargetName}, UserName: {UserName}, Password: {Password}";
}
