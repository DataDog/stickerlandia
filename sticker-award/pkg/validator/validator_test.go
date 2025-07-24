package validator

import (
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"github.com/datadog/stickerlandia/sticker-award/internal/api/dto"
)

func TestNew(t *testing.T) {
	v := New()
	assert.NotNil(t, v)
	assert.NotNil(t, v.validator)
}

func TestValidator_Validate_AssignStickerRequest(t *testing.T) {
	v := New()

	tests := []struct {
		name    string
		request dto.AssignStickerRequest
		wantErr bool
		errMsg  string
	}{
		{
			name: "valid request with reason",
			request: dto.AssignStickerRequest{
				StickerID: "test-sticker-1",
				Reason:    stringPtr("Test reason"),
			},
			wantErr: false,
		},
		{
			name: "valid request without reason",
			request: dto.AssignStickerRequest{
				StickerID: "test-sticker-1",
				Reason:    nil,
			},
			wantErr: false,
		},
		{
			name: "missing sticker ID",
			request: dto.AssignStickerRequest{
				StickerID: "",
				Reason:    stringPtr("Test reason"),
			},
			wantErr: true,
			errMsg:  "StickerID",
		},
		{
			name: "reason too long",
			request: dto.AssignStickerRequest{
				StickerID: "test-sticker-1",
				Reason:    stringPtr(generateLongString(501)), // Max is 500
			},
			wantErr: true,
			errMsg:  "Reason",
		},
		{
			name: "reason at max length",
			request: dto.AssignStickerRequest{
				StickerID: "test-sticker-1",
				Reason:    stringPtr(generateLongString(500)), // Exactly max
			},
			wantErr: false,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			err := v.Validate(tt.request)

			if tt.wantErr {
				require.Error(t, err)
				assert.Contains(t, err.Error(), tt.errMsg)
			} else {
				assert.NoError(t, err)
			}
		})
	}
}

func TestValidator_ValidateVar(t *testing.T) {
	v := New()

	tests := []struct {
		name    string
		field   interface{}
		tag     string
		wantErr bool
	}{
		{
			name:    "required field with value",
			field:   "test-value",
			tag:     "required",
			wantErr: false,
		},
		{
			name:    "required field empty",
			field:   "",
			tag:     "required",
			wantErr: true,
		},
		{
			name:    "max length valid",
			field:   "short",
			tag:     "max=10",
			wantErr: false,
		},
		{
			name:    "max length exceeded",
			field:   "this is too long",
			tag:     "max=10",
			wantErr: true,
		},
		{
			name:    "email valid",
			field:   "test@example.com",
			tag:     "email",
			wantErr: false,
		},
		{
			name:    "email invalid",
			field:   "not-an-email",
			tag:     "email",
			wantErr: true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			err := v.ValidateVar(tt.field, tt.tag)

			if tt.wantErr {
				assert.Error(t, err)
			} else {
				assert.NoError(t, err)
			}
		})
	}
}

func TestValidator_Validate_InvalidStruct(t *testing.T) {
	v := New()

	// Test with non-struct type
	err := v.Validate("not a struct")
	assert.Error(t, err)
}

// Helper functions
func stringPtr(s string) *string {
	return &s
}

func generateLongString(length int) string {
	result := make([]byte, length)
	for i := range result {
		result[i] = 'a'
	}
	return string(result)
}
