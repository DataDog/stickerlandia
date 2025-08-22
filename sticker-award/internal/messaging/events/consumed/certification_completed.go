package consumed

import "time"

// CertificationCompletedEvent represents the event consumed when a certification is completed
type CertificationCompletedEvent struct {
	EventName       string    `json:"eventName"`
	EventVersion    string    `json:"eventVersion"`
	AccountID       string    `json:"accountId"`
	CertificationID string    `json:"certificationId"`
	CompletedAt     time.Time `json:"completedAt"`
}

// NewCertificationCompletedEvent creates a new CertificationCompletedEvent
func NewCertificationCompletedEvent(userID, certificationID string, completedAt time.Time) *CertificationCompletedEvent {
	return &CertificationCompletedEvent{
		EventName:       "CertificationCompleted",
		EventVersion:    "v1",
		AccountID:       userID,
		CertificationID: certificationID,
		CompletedAt:     completedAt,
	}
}
