# Stickerlandia Event Flows

This document outlines the event flows between the different services in the Stickerlandia application.

## User Registration Flow

A user can either register directly with Stickerlandia, or login via a federated identity provider. We foresee this to be Datadog's own corporate directory and potentially one associated with our certification provider, so that we can tie external folks' identity up to their awards.

Note: We still need to implement a login interface to facilitate the federated identity integration. This is currently a TODO item for future development.

When a new user registers in the system, the User Management service publishes a user registration event. This flow ensures that other services are aware of new users in the system.

```mermaid
sequenceDiagram
    participant Client
    participant UserManagement
    participant MessageBroker
    participant OutboxProcessor
    
    Client->>UserManagement: Register new user
    UserManagement->>UserManagement: Create user account
    UserManagement->>OutboxProcessor: Store UserRegisteredEvent
    UserManagement-->>Client: Registration confirmation
    OutboxProcessor->>MessageBroker: Publish users.userRegistered.v1
    Note over MessageBroker: Event available for other services
```

## Sticker Assignment

Stickers can be assigned through two primary mechanisms:
1. Via an admin UI, where administrators can manually award stickers to users
2. Via integration with certification systems, where the completion of certifications automatically triggers sticker awards (future state)

For the automatic assignment, another service will adapt external certification events to our internal event model and generate events that the sticker-award service listens to.
Note: This automatic assignment process corresponds to the [Sticker Claimed Flow](#sticker-claimed-flow) described below.

In all cases, the mapping from users to assigned stickers is managed by the sticker-award service.

When a sticker is assigned to a user, the Sticker Award service creates the assignment and notifies other services through events.

```mermaid
sequenceDiagram
    participant Client
    participant StickerAward
    participant Database
    participant MessageBroker
    participant UserManagement
    
    Client->>StickerAward: POST /api/award/v1/users/{userId}/stickers
    StickerAward->>Database: Check if sticker exists
    StickerAward->>Database: Check if already assigned
    StickerAward->>Database: Create assignment
    StickerAward-->>Client: Assignment confirmation
    StickerAward->>MessageBroker: Publish stickers.stickerAssignedToUser.v1
    MessageBroker->>UserManagement: Consume event
    UserManagement->>UserManagement: Update user record
```

## Sticker Removal Flow

Sticker removal is primarily performed through the admin UI, allowing administrators to revoke previously assigned stickers when necessary.

When a sticker is removed from a user, the Sticker Award service updates the assignment status and notifies other services.

```mermaid
sequenceDiagram
    participant Client
    participant StickerAward
    participant Database
    participant MessageBroker
    participant UserManagement
    
    Client->>StickerAward: DELETE /api/award/v1/users/{userId}/stickers/{stickerId}
    StickerAward->>Database: Find active assignment
    StickerAward->>Database: Mark as removed
    StickerAward-->>Client: Removal confirmation
    StickerAward->>MessageBroker: Publish stickers.stickerRemovedFromUser.v1
    MessageBroker->>UserManagement: Consume event
    UserManagement->>UserManagement: Update user record
```

## Sticker Claimed Flow

This flow represents the scenario where users complete specific challenges or achievements in external systems. A service monitors these achievements and publishes events that inform our system about users qualifying for stickers.

When a user claims a sticker by completing a task or achievement, the event is processed to update the user's account.

```mermaid
sequenceDiagram
    participant ExternalSystem
    participant MessageBroker
    participant UserManagement
    participant Database
    
    ExternalSystem->>MessageBroker: Publish users.stickerClaimed.v1
    MessageBroker->>UserManagement: Consume via dedicated event worker
    UserManagement->>UserManagement: Process StickerClaimedEventV1
    UserManagement->>Database: Update user's sticker count
    Note over UserManagement: StickerOrdered() method called
```

## Certification Completion Flow

When a user completes a certification in an external system, the Sticker Award service can automatically assign appropriate stickers.

```mermaid
sequenceDiagram
    participant CertificationSystem
    participant MessageBroker
    participant StickerAward
    participant Database
    
    CertificationSystem->>MessageBroker: Publish certifications.certificationCompleted.v1
    MessageBroker->>StickerAward: Consume event
    StickerAward->>Database: Determine appropriate sticker(s)
    StickerAward->>Database: Create sticker assignment(s)
    StickerAward->>MessageBroker: Publish stickers.stickerAssignedToUser.v1
    Note over StickerAward: Automatic award process completed
```
