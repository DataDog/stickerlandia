# Stickerlandia

## High-Level Architecture
Stickerlandia is built in a microservice fashion, and can run in a bunch of different configurations across various types of modern infrastructure including cloud-native serverless, cloud-native container orchestrated, and self-hosted container orchestrated. In each case, appropriate components are composed together to lean into the strengths of a particular platform - the choice of database, queue technology, and load balancer on a serverless deployment will not be the same as on a self-hosted K8S environment!

Services communicate in two ways:

* Synchronous, RESTful APIs - for both read and write operations, where the operation is naturally imperative. These calls are secured by JWT. 
* Asynchronous, event-based APIs - for our implicit, observation based interactions, each service emits business events describing changes in its managed domain.

In both cases these will be modelled in OpenAPI. 

## Services

| Service | Description | Documentation |
|---------|-------------|---------------|
| [User Management](./user-management/) | Manages user accounts, authentication, and profile information. Handles user registration, login, and JWT token issuance. | [API Docs](./user-management/docs/api.json) |
| [Sticker Award](./sticker-award/) | Manages the assignment of stickers to users. Tracks which users have which stickers and handles assignment/removal based on criteria like certification completion. | [API Docs](./sticker-award/docs/api.json) |

## Messaging Topics

The following Kafka topics are used for asynchronous communication between services:

| Topic Name | Publishing Service | Description |
|------------|-------------------|-------------|
| users.userRegistered.v1 | User Management | Published when a new user successfully registers. Contains user profile information for other services to initialize user-related data. |
| users.userDetailsUpdated.v1 | User Management | Published when a user updates their profile information. Contains the updated user profile data. |
| users.stickerClaimed.v1 | TODO | TODO |

