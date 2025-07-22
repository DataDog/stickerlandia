// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.RegularExpressions;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal static class HtmlFormParser
{
    public static Dictionary<string, string> ExtractFormFields(string html, string? formId = null)
    {
        var formFields = new Dictionary<string, string>();
        
        // Find the form
        var formPattern = formId != null 
            ? $@"<form[^>]*id=""{formId}""[^>]*>(.*?)</form>"
            : @"<form[^>]*>(.*?)</form>";
            
        var formMatch = Regex.Match(html, formPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        if (!formMatch.Success)
        {
            return formFields;
        }
        
        var formContent = formMatch.Groups[1].Value;
        
        // Extract input fields
        var inputPattern = @"<input[^>]*name=""([^""]+)""[^>]*value=""([^""]*)""[^>]*>";
        var inputMatches = Regex.Matches(formContent, inputPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in inputMatches)
        {
            var name = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            formFields[name] = value;
        }
        
        // Extract hidden fields specifically (they might not have explicit values)
        var hiddenPattern = @"<input[^>]*type=""hidden""[^>]*name=""([^""]+)""[^>]*value=""([^""]*)""[^>]*>";
        var hiddenMatches = Regex.Matches(formContent, hiddenPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in hiddenMatches)
        {
            var name = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            formFields[name] = value;
        }
        
        return formFields;
    }
    
    public static string? ExtractFormAction(string html, string? formId = null)
    {
        var formPattern = formId != null 
            ? $@"<form[^>]*id=""{formId}""[^>]*action=""([^""]+)""[^>]*>"
            : @"<form[^>]*action=""([^""]+)""[^>]*>";
            
        var match = Regex.Match(html, formPattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
    
    public static string? ExtractAntiForgeryToken(string html)
    {
        var tokenPattern = @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""[^>]*>";
        var match = Regex.Match(html, tokenPattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}