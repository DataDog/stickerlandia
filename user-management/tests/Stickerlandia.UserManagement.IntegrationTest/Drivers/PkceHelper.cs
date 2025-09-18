/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Cryptography;
using System.Text;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal static class PkceHelper
{
    public static (string codeVerifier, string codeChallenge) GeneratePkceParameters()
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        return (codeVerifier, codeChallenge);
    }
    
    private static string GenerateCodeVerifier()
    {
        const int length = 128;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        
        using var random = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        var buffer = new byte[1];
        
        for (int i = 0; i < length; i++)
        {
            int index;
            do
            {
                random.GetBytes(buffer);
                index = buffer[0] % chars.Length;
            } while (index >= chars.Length);
            
            result.Append(chars[index]);
        }
        
        return result.ToString();
    }
    
    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}