// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

public record UserAccountDTO
{
    public string AccountId { get; set; }
    
    public string EmailAddress { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
}