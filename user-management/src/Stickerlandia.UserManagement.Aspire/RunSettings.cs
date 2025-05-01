// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Aspire;

public enum RunAs
{
    ASPNET,
    AZURE_FUNCTIONS,
    AWS_LAMBDA
}

public static class RunSettings
{
    private static RunAs? _runAs;

    public static RunAs? RunAs
    {
        get
        {
            if (_runAs is not null)
            {
                return _runAs;
            }
            
            var runAsSetting = Environment.GetEnvironmentVariable("RUN_AS");
        
            switch (runAsSetting)
            {
                case "AZURE_FUNCTIONS":
                    _runAs = Aspire.RunAs.AZURE_FUNCTIONS;
                    break;
                case "AWS_LAMBDA":
                    _runAs = Aspire.RunAs.AWS_LAMBDA;
                    break;
                default:
                    _runAs = Aspire.RunAs.ASPNET;
                    break;
            }
            
            return _runAs;
        }
    }

    public static void OverrideTo(RunAs runAs)
    {
        _runAs = runAs;
    }
}