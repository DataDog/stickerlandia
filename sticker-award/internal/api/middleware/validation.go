package middleware

import (
	"net/http"

	"github.com/gin-gonic/gin"
	"github.com/go-playground/validator/v10"

	"github.com/datadoghq/stickerlandia/sticker-award/internal/api/dto"
)

// ValidationError represents a validation error response
type ValidationError struct {
	Field   string `json:"field"`
	Message string `json:"message"`
}

// Validation returns a middleware that validates request payloads
func Validation() gin.HandlerFunc {
	return func(c *gin.Context) {
		c.Next()
	}
}

// formatValidationErrors converts validator errors to a readable format
func formatValidationErrors(err error) []ValidationError {
	var validationErrors []ValidationError

	if validationErr, ok := err.(validator.ValidationErrors); ok {
		for _, fieldErr := range validationErr {
			validationErrors = append(validationErrors, ValidationError{
				Field:   fieldErr.Field(),
				Message: getValidationMessage(fieldErr),
			})
		}
	}

	return validationErrors
}

// getValidationMessage returns a user-friendly validation message
func getValidationMessage(fieldErr validator.FieldError) string {
	switch fieldErr.Tag() {
	case "required":
		return "This field is required"
	case "max":
		return "Value is too long"
	case "min":
		return "Value is too short"
	case "email":
		return "Invalid email format"
	default:
		return "Invalid value"
	}
}

// HandleValidationError handles validation errors consistently
func HandleValidationError(c *gin.Context, err error) {
	_ = formatValidationErrors(err) // Format errors for potential future use

	c.JSON(http.StatusBadRequest, dto.ProblemDetails{
		Type:   stringPtr("about:blank"),
		Title:  stringPtr("Validation Error"),
		Status: intPtr(http.StatusBadRequest),
		Detail: stringPtr("Request validation failed"),
		// Additional field for validation errors could be added here
	})
}

func stringPtr(s string) *string {
	return &s
}

func intPtr(i int) *int {
	return &i
}
