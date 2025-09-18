/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.Core;

public interface IUserEventPublisher
{
    Task PublishUserRegisteredEventV1(UserRegisteredEvent userRegisteredEvent);
}