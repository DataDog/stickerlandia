// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package middleware

import (
	"net/http"

	"github.com/gin-gonic/gin"
	log "github.com/sirupsen/logrus"
)

// Recovery returns a Gin middleware that recovers from panics
func Recovery() gin.HandlerFunc {
	return gin.RecoveryWithWriter(gin.DefaultWriter, func(c *gin.Context, err interface{}) {
		log.WithContext(c.Request.Context()).WithFields(log.Fields{
			"error":     err,
			"method":    c.Request.Method,
			"path":      c.Request.URL.Path,
			"client_ip": c.ClientIP(),
		}).Error("Panic recovered")

		c.JSON(http.StatusInternalServerError, gin.H{
			"type":   "about:blank",
			"title":  "Internal Server Error",
			"status": http.StatusInternalServerError,
			"detail": "An unexpected error occurred",
		})
	})
}
