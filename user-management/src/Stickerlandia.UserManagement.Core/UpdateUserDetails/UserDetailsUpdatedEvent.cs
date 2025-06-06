// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.Core.UpdateUserDetails;

public record UserDetailsUpdatedEvent : DomainEvent
{
    public UserDetailsUpdatedEvent(){}
    
    public UserDetailsUpdatedEvent(UserAccount account)
    {
        AccountId = account.Id!.Value;
    }
    
    [JsonPropertyName("eventName")]
    public override string EventName => "users.userDetailsUpdated.v1";
    
    [JsonPropertyName("eventVersion")]
    public override string EventVersion => "1.0";
    public override string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }

    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";
}