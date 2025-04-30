# Stickerlandia

## High-Level Architecture
Stickerlandia is built in a microservice fashion, and can run in a bunch of different configurations across various types of modern infrastructure including cloud-native serverless, cloud-native container orchestrated, and self-hosted container orchestrated. In each case, appropriate components are composed together to lean into the strengths of a particular platform - the choice of database, queue technology, and load balancer on a serverless deployment will not be the same as on a self-hosted K8S environment!

Services communicate in two ways:

* Synchronous, RESTful APIs - for both read and write operations, where the operation is naturally imperative. These calls are secured by JWT. 
* Asynchronous, event-based APIs - for our implicit, observation based interactions, each service emits business events describing changes in its managed domain.

In both cases these will be modelled in OpenAPI. 

## Services

### sticker-award

The sticker award service tracks the assignment of particular stickers to particular users. It facilitates this via the following interfaces:

* REST API - synchronous creation and read-back of stickers. Stickers can be read and manipulated for the current active user as identified by the caller's credentials, or, in the case of a user with admin credentials, the assignments of other users
* Asynchronous API - the award service listens to `CertificationCompleted` events, in order to assign certificates to particular users based on meeting the criteria for particular stickers. Additionally the award service emits `StickerAssignedToUser` and `StickerRemovedFromUser` events when a user's sticker assignments are modified.

