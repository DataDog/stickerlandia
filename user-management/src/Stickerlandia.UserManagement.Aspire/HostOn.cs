// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Aspire;

public enum HostOn
{
    AGNOSTIC,
    AWS,
    AZURE
}

public static class HostOnSettings
{
    private static HostOn? _hostOn;

    public static HostOn? HostOn
    {
        get
        {
            if (_hostOn is not null)
            {
                return _hostOn;
            }
            
            var runAsSetting = Environment.GetEnvironmentVariable("RUN_AS");
        
            switch (runAsSetting)
            {
                case "AZURE":
                    _hostOn = Aspire.HostOn.AZURE;
                    break;
                case "AWS":
                    _hostOn = Aspire.HostOn.AWS;
                    break;
                case "AGNOSTIC":
                    _hostOn = Aspire.HostOn.AGNOSTIC;
                    break;
                default:
                    throw new Exception($"Unknown host type: '{runAsSetting}'");
            }
            
            return _hostOn;
        }
    }

    public static void OverrideTo(HostOn runAs)
    {
        _hostOn = runAs;
    }
}