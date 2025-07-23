# Authentication Guide

This document provides comprehensive guidance on the OAuth 2.0 authentication flows supported by the Stickerlandia User Management Service.

## Overview

The Stickerlandia User Management Service implements a complete OAuth 2.0 authorization server using [OpenIddict](https://documentation.openiddict.com/), integrated with ASP.NET Core Identity for robust user management.

### Key Components
- **Authorization Server**: OpenIddict-based OAuth 2.0 implementation
- **Identity Provider**: ASP.NET Core Identity with PostgreSQL storage
- **User Model**: Custom `PostgresUserAccount` extending `IdentityUser`
- **Security**: PKCE (Proof Key for Code Exchange) for enhanced security

## OAuth 2.0 Endpoints

The service exposes standard OAuth 2.0 endpoints:

| Endpoint | URL | Purpose |
|----------|-----|---------|
| Authorization | `/api/users/v1/connect/authorize` | Initiate OAuth authorization flow |
| Token | `/api/users/v1/connect/token` | Exchange authorization codes for tokens |
| UserInfo | `/api/users/v1/connect/userinfo` | Retrieve user information using access token |
| Logout | `/api/users/v1/connect/logout` | End user session |

## Supported Authentication Flows

### ✅ Authorization Code Flow with PKCE

**Primary flow for client applications** - Recommended for SPAs, mobile apps, and web applications.

**Features:**
- Enhanced security with PKCE (Proof Key for Code Exchange)
- Suitable for public clients (SPAs, mobile apps)
- Supports refresh tokens for seamless user experience
- Implements OpenID Connect for user identity

**Scopes Supported:**
- `email` - Access to user email address
- `profile` - Access to user profile information (name, account details)
- `roles` - Access to user roles and permissions

### ✅ Refresh Token Flow

**Token refresh capability** - Extends user sessions without re-authentication.

**Features:**
- Seamless token refresh without user interaction
- Configurable token lifetimes
- Automatic token rotation for enhanced security

### ❌ Client Credentials Flow

**Not Currently Implemented** - Service-to-service authentication can be implemented at a later date once service to service communication is required.

### ❌ Other Flows

The following OAuth 2.0 flows are **not supported** for security reasons:
- **Implicit Flow** - Deprecated due to security vulnerabilities
- **Resource Owner Password Credentials Flow** - Not recommended for modern applications

## Authentication Flow Diagrams

### Authorization Code Flow with PKCE

```mermaid
sequenceDiagram
    participant U as User
    participant C as Client App
    participant B as Browser
    participant AS as Auth Server
    participant RS as Resource Server (API)

    Note over C,AS: 1. Client prepares PKCE parameters
    C->>C: Generate code_verifier (random string)
    C->>C: Generate code_challenge = SHA256(code_verifier)

    Note over U,AS: 2. Authorization Request
    U->>C: Click "Login"
    C->>B: Redirect to /api/users/v1/connect/authorize?<br/>response_type=code&<br/>client_id=spa-client&<br/>redirect_uri=https://app.com/callback&<br/>scope=email profile&<br/>code_challenge=xyz&<br/>code_challenge_method=S256&<br/>state=random-state
    B->>AS: GET /api/users/v1/connect/authorize

    Note over AS,B: 3. User Authentication & Consent
    AS->>B: Show login page
    U->>B: Enter credentials
    B->>AS: POST login credentials
    AS->>AS: Validate user credentials
    AS->>B: Show consent page (if required)
    U->>B: Grant permissions
    B->>AS: POST consent approval

    Note over AS,C: 4. Authorization Response
    AS->>B: Redirect to callback?<br/>code=auth-code-123&<br/>state=random-state
    B->>C: Follow redirect with authorization code

    Note over C,AS: 5. Token Exchange
    C->>AS: POST /api/users/v1/connect/token<br/>grant_type=authorization_code&<br/>code=auth-code-123&<br/>client_id=spa-client&<br/>code_verifier=original-verifier&<br/>redirect_uri=https://app.com/callback
    AS->>AS: Validate code_verifier against code_challenge
    AS->>C: Return tokens:<br/>{<br/>  "access_token": "...",<br/>  "refresh_token": "...",<br/>  "id_token": "...",<br/>  "token_type": "Bearer",<br/>  "expires_in": 3600<br/>}

    Note over C,RS: 6. Access Protected Resources
    C->>RS: GET /api/users/v1/details<br/>Authorization: Bearer access-token
    RS->>RS: Validate JWT token
    RS->>C: Return user data
```

### User Registration Flow

```mermaid
sequenceDiagram
    participant U as User
    participant C as Client App
    participant AS as Auth Server
    participant DB as Database
    participant ES as Event System

    Note over U,DB: User Registration Process
    U->>C: Fill registration form
    C->>AS: POST /api/users/v1/register<br/>{<br/>  "email": "user@example.com",<br/>  "password": "secure-password",<br/>  "firstName": "John",<br/>  "lastName": "Doe"<br/>}
    
    AS->>AS: Validate input data
    AS->>AS: Hash password
    AS->>DB: Create user record
    DB->>AS: Confirm user created
    
    AS->>ES: Publish userRegistered.v1 event<br/>(via outbox pattern)
    AS->>C: Return 201 Created<br/>{<br/>  "userId": "user-123",<br/>  "email": "user@example.com"<br/>}
    
    Note over ES: Background Processing
    ES->>ES: Process outbox events
    ES->>ES: Send welcome email
    ES->>ES: Initialize user preferences
    
    C->>U: Show registration success
    C->>U: Redirect to login
```

### Token Refresh Flow

```mermaid
sequenceDiagram
    participant C as Client App
    participant AS as Auth Server
    participant RS as Resource Server

    Note over C,AS: Token Refresh Process
    C->>RS: API Request with expired token
    RS->>C: 401 Unauthorized (token expired)
    
    C->>C: Check if refresh token available
    C->>AS: POST /api/users/v1/connect/token<br/>grant_type=refresh_token&<br/>refresh_token=refresh-token-123&<br/>client_id=spa-client
    
    AS->>AS: Validate refresh token
    AS->>AS: Generate new tokens
    AS->>C: Return new tokens:<br/>{<br/>  "access_token": "new-access-token",<br/>  "refresh_token": "new-refresh-token",<br/>  "token_type": "Bearer",<br/>  "expires_in": 3600<br/>}
    
    C->>C: Store new tokens
    C->>RS: Retry API request with new token<br/>Authorization: Bearer new-access-token
    RS->>C: 200 OK with data
```

### Logout Flow

```mermaid
sequenceDiagram
    participant U as User
    participant C as Client App
    participant AS as Auth Server
    participant DB as Database

    Note over U,DB: User Logout Process
    U->>C: Click "Logout"
    C->>AS: POST /api/users/v1/connect/logout<br/>id_token_hint=user-id-token&<br/>post_logout_redirect_uri=https://app.com/logged-out
    
    AS->>DB: Revoke user session
    AS->>DB: Invalidate refresh tokens
    DB->>AS: Confirm tokens revoked
    
    AS->>C: Redirect to post_logout_redirect_uri
    C->>C: Clear local tokens
    C->>U: Show "Logged out successfully"
```