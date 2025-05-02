// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Aspire;

public enum DrivenAdapters
{
    AGNOSTIC,
    AWS,
    AZURE
}

public static class DrivenAdapterSettings
{
    private static DrivenAdapters? _drivenAdapter;

    public static DrivenAdapters? DrivenAdapter
    {
        get
        {
            if (_drivenAdapter is not null)
            {
                return _drivenAdapter;
            }
            
            var drivenAdapterSetting = Environment.GetEnvironmentVariable("DRIVEN");
        
            switch (drivenAdapterSetting)
            {
                case "AZURE":
                    _drivenAdapter = Aspire.DrivenAdapters.AZURE;
                    break;
                case "AWS":
                    _drivenAdapter = Aspire.DrivenAdapters.AWS;
                    break;
                case "AGNOSTIC":
                    _drivenAdapter = Aspire.DrivenAdapters.AGNOSTIC;
                    break;
                default:
                    throw new Exception($"Unknown host type: '{drivenAdapterSetting}'");
            }
            
            return _drivenAdapter;
        }
    }

    public static void OverrideTo(DrivenAdapters drivenAdapters)
    {
        _drivenAdapter = drivenAdapters;
    }
}