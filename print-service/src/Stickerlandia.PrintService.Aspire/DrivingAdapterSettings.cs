/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Aspire;

internal enum DrivingAdapter
{
    AWS,
    AGNOSTIC,
    AZURE,
    GCP
}

internal static class DrivingAdapterSettings
{
    private static DrivingAdapter? _drivingAdapter;

    public static DrivingAdapter? DrivingAdapter
    {
        get
        {
            if (_drivingAdapter is not null) return _drivingAdapter;

            var drivingSetting = Environment.GetEnvironmentVariable("DRIVING");

            switch (drivingSetting)
            {
                case "AZURE_FUNCTIONS":
                    _drivingAdapter = Aspire.DrivingAdapter.AZURE;
                    break;
                case "AWS_LAMBDA":
                    _drivingAdapter = Aspire.DrivingAdapter.AWS;
                    break;
                default:
                    _drivingAdapter = Aspire.DrivingAdapter.AGNOSTIC;
                    break;
            }

            return _drivingAdapter;
        }
    }

    public static void OverrideTo(DrivingAdapter drivingAdapter)
    {
        _drivingAdapter = drivingAdapter;
    }
}