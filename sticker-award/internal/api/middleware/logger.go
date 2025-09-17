// Copyright 2025 Datadog, Inc.
// SPDX-License-Identifier: Apache-2.0

package middleware

import (
	"time"

	"github.com/gin-gonic/gin"
	log "github.com/sirupsen/logrus"
)

// Logger returns a Gin middleware that logs HTTP requests using logrus
func Logger() gin.HandlerFunc {
	return gin.LoggerWithFormatter(func(param gin.LogFormatterParams) string {
		log.WithContext(param.Request.Context()).WithFields(log.Fields{
			"timestamp":     param.TimeStamp.Format(time.RFC3339),
			"method":        param.Method,
			"path":          param.Path,
			"status":        param.StatusCode,
			"latency":       param.Latency,
			"client_ip":     param.ClientIP,
			"user_agent":    param.Request.UserAgent(),
			"response_size": param.BodySize,
			"error":         param.ErrorMessage,
		}).Info("HTTP Request")
		return ""
	})
}
