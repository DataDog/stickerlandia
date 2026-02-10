/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.Agnostic.Repositories;

public class PostgresPrinterKeyValidator(IPrinterRepository printerRepository) : IPrinterKeyValidator
{
    public async Task<Printer?> ValidateKeyAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        return await printerRepository.GetPrinterByKeyAsync(key).ConfigureAwait(false);
    }
}
