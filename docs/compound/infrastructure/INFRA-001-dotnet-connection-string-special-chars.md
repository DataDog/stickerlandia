---
type: problem-solution
category: infrastructure
tags: [dotnet, connection-string, postgres, npgsql, aws-cdk, secrets-manager, rds, password-escaping]
created: 2026-02-02
confidence: high
languages: [dotnet, javascript]
related: []
---

# .NET Connection String Fails with Auto-Generated Passwords Containing Special Characters

## Problem

Database migrations fail with the error:
```
System.ArgumentException: Format of the initialization string does not conform to specification starting at index NNN.
```

This occurs when:
1. Using AWS CDK `DatabaseSecret` to auto-generate database passwords
2. Building .NET connection strings by simple string interpolation
3. The generated password contains characters that are delimiters in .NET connection strings (`;`, `=`, `'`)

### Misleading Symptoms

The error manifests as "migration service fails to connect to the Postgres database" which leads to investigating:
- Security groups and networking
- VPC configuration
- DNS resolution
- SSL/TLS settings

**The actual issue is connection string parsing, not networking.**

### How to Identify

1. Check ECS task or application logs for the full stack trace
2. Look for `System.ArgumentException` with "initialization string" in the message
3. The stack trace will show `DbConnectionStringBuilder` or `NpgsqlConnectionStringBuilder`

## Solution

When building .NET connection strings, **always quote password values** and escape embedded quotes:

### Before (Broken)
```javascript
const connStr = `Host=${host};Database=${db};Username=${user};Password=${password}`;
```

If password is `abc;def=xyz`, the connection string becomes:
```
Host=xxx;Database=yyy;Username=postgres;Password=abc;def=xyz
```
The parser interprets `def=xyz` as a separate key-value pair.

### After (Fixed)
```javascript
// Escape single quotes by doubling them, then wrap in single quotes
const escapedPassword = password.replace(/'/g, "''");
const connStr = `Host=${host};Database=${db};Username=${user};Password='${escapedPassword}'`;
```

If password is `abc;def='xyz`, the connection string becomes:
```
Host=xxx;Database=yyy;Username=postgres;Password='abc;def=''xyz'
```
The parser correctly extracts the password as `abc;def='xyz`.

## .NET Connection String Escaping Rules

| Character | In Unquoted Value | In Single-Quoted Value |
|-----------|-------------------|------------------------|
| `;` | Terminates value | Allowed |
| `=` | Invalid in value | Allowed |
| `'` | Starts quoted value | Use `''` to escape |
| `"` | Starts quoted value | Allowed |
| whitespace | Trimmed | Preserved |

## Alternative: Exclude Characters from Password Generation

If you control the password generation, exclude problematic characters:

```typescript
new DatabaseSecret(this, "DBSecret", {
  username: "postgres",
  excludeCharacters: '"@/\\;=\'',  // Exclude connection string delimiters
});
```

**However**, quoting the password is more defensive as it handles any special character.

## Files Changed

- `shared/lib/shared-constructs/lib/lambda/database-init/index.js` - Quote password in dotnet format connection string

## Related

- [Npgsql Connection String Documentation](https://www.npgsql.org/doc/connection-string-parameters.html)
- [.NET Connection String Syntax](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/connection-string-syntax)

## Prevention

1. **Always quote values** in connection strings that may contain special characters
2. **Test with generated passwords** - don't just test with simple passwords
3. **Log connection errors clearly** - distinguish between parsing errors and network errors
