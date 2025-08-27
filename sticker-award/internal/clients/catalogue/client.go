package catalogue

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	httptrace "github.com/DataDog/dd-trace-go/contrib/net/http/v2"
	"github.com/datadog/stickerlandia/sticker-award/pkg/errors"
)

// ErrStickerNotFound is returned when a sticker is not found in the catalogue
var ErrStickerNotFound = errors.ErrStickerNotFound

// Client represents a client for the sticker catalogue service
type Client struct {
	baseURL    string
	httpClient *http.Client
}

// StickerMetadata represents sticker metadata from the catalogue service
type StickerMetadata struct {
	StickerID                string    `json:"stickerId"`
	StickerName              string    `json:"stickerName"`
	StickerDescription       *string   `json:"stickerDescription"`
	StickerQuantityRemaining int       `json:"stickerQuantityRemaining"`
	ImageURL                 *string   `json:"imageUrl"`
	CreatedAt                time.Time `json:"createdAt"`
	UpdatedAt                time.Time `json:"updatedAt"`
}

// NewClient creates a new catalogue service client
func NewClient(baseURL string, timeout time.Duration) *Client {
	traced := httptrace.WrapClient(&http.Client{Timeout: timeout})
	return &Client{
		baseURL:    baseURL,
		httpClient: traced,
	}
}

// GetSticker retrieves sticker metadata from the catalogue service
func (c *Client) GetSticker(ctx context.Context, stickerID string) (*StickerMetadata, error) {
	url := fmt.Sprintf("%s/api/stickers/v1/%s", c.baseURL, stickerID)

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Accept", "application/json")

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("failed to make request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusNotFound {
		return nil, ErrStickerNotFound
	}

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	var sticker StickerMetadata
	if err := json.NewDecoder(resp.Body).Decode(&sticker); err != nil {
		return nil, fmt.Errorf("failed to decode response: %w", err)
	}

	return &sticker, nil
}

// StickerExists checks if a sticker exists in the catalogue
func (c *Client) StickerExists(ctx context.Context, stickerID string) (bool, error) {
	_, err := c.GetSticker(ctx, stickerID)
	if err != nil {
		if err == ErrStickerNotFound {
			return false, nil
		}
		return false, err
	}
	return true, nil
}
