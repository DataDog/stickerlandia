// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025-Present Datadog, Inc.

package middleware

import (
	"context"
	"net/http"
	"strings"
	"time"

	"github.com/MicahParks/keyfunc/v3"
	"github.com/gin-gonic/gin"
	"github.com/golang-jwt/jwt/v5"
	log "github.com/sirupsen/logrus"

	"github.com/datadog/stickerlandia/sticker-award/internal/config"
)

// Claims represents the JWT claims we expect
type Claims struct {
	jwt.RegisteredClaims
	Sub   string `json:"sub"`
	Email string `json:"email,omitempty"`
	Name  string `json:"name,omitempty"`
}

// JWTValidator handles JWT token validation using JWKS
type JWTValidator struct {
	jwks       keyfunc.Keyfunc
	issuer     string
	clockSkew  time.Duration
	cancelFunc context.CancelFunc
}

// NewJWTValidator creates a new JWT validator with the given configuration
func NewJWTValidator(cfg *config.AuthConfig) (*JWTValidator, error) {
	// Create context with cancel for cleanup
	ctx, cancel := context.WithCancel(context.Background())

	// Create JWKS keyfunc with background refresh
	jwks, err := keyfunc.NewDefaultCtx(ctx, []string{cfg.JWKSUrl})
	if err != nil {
		cancel()
		return nil, err
	}

	return &JWTValidator{
		jwks:       jwks,
		issuer:     cfg.Issuer,
		clockSkew:  time.Duration(cfg.ClockSkew) * time.Second,
		cancelFunc: cancel,
	}, nil
}

// JWT returns a Gin middleware that validates JWT tokens
func (v *JWTValidator) JWT() gin.HandlerFunc {
	return func(c *gin.Context) {
		// Extract token from Authorization header
		authHeader := c.GetHeader("Authorization")
		if authHeader == "" {
			log.WithContext(c.Request.Context()).Debug("Missing Authorization header")
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{
				"error": "missing authorization header",
			})
			return
		}

		// Check Bearer prefix
		if !strings.HasPrefix(authHeader, "Bearer ") {
			log.WithContext(c.Request.Context()).Debug("Invalid Authorization header format")
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{
				"error": "invalid authorization header format",
			})
			return
		}

		tokenString := strings.TrimPrefix(authHeader, "Bearer ")

		// Parse and validate the token
		token, err := jwt.ParseWithClaims(tokenString, &Claims{}, v.jwks.Keyfunc,
			jwt.WithIssuer(v.issuer),
			jwt.WithLeeway(v.clockSkew),
		)

		if err != nil {
			log.WithContext(c.Request.Context()).WithError(err).Debug("Token validation failed")
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{
				"error": "invalid token",
			})
			return
		}

		if !token.Valid {
			log.WithContext(c.Request.Context()).Debug("Token is not valid")
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{
				"error": "invalid token",
			})
			return
		}

		// Extract claims and store in context
		if claims, ok := token.Claims.(*Claims); ok {
			c.Set("user_id", claims.Sub)
			c.Set("user_email", claims.Email)
			c.Set("user_name", claims.Name)
			c.Set("claims", claims)
		}

		c.Next()
	}
}

// Close releases resources used by the validator
func (v *JWTValidator) Close() {
	if v.cancelFunc != nil {
		v.cancelFunc()
	}
}
