/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue.security;

import jakarta.annotation.Priority;
import jakarta.enterprise.context.ApplicationScoped;
import jakarta.inject.Inject;
import jakarta.ws.rs.Priorities;
import jakarta.ws.rs.container.ContainerRequestContext;
import jakarta.ws.rs.container.ContainerRequestFilter;
import jakarta.ws.rs.core.Response;
import jakarta.ws.rs.core.SecurityContext;
import jakarta.ws.rs.ext.Provider;
import java.io.IOException;
import java.util.Optional;
import org.eclipse.microprofile.config.inject.ConfigProperty;
import org.eclipse.microprofile.jwt.JsonWebToken;
import org.jboss.logging.Logger;

/**
 * JAX-RS filter that validates JWT issuer with trailing slash normalization. This is needed because
 * OpenIddict adds a trailing slash to the issuer claim, but we don't want double slashes in the
 * JWKS URL derivation.
 */
@Provider
@Priority(Priorities.AUTHENTICATION + 1) // Run after JWT authentication
@ApplicationScoped
public class IssuerValidationFilter implements ContainerRequestFilter {

    private static final Logger LOG = Logger.getLogger(IssuerValidationFilter.class);

    @Inject JsonWebToken jwt;

    @ConfigProperty(name = "sticker.jwt.expected-issuer")
    Optional<String> expectedIssuer;

    @ConfigProperty(name = "sticker.jwt.issuer-validation.enabled", defaultValue = "true")
    boolean issuerValidationEnabled;

    @Override
    public void filter(ContainerRequestContext requestContext) throws IOException {
        SecurityContext securityContext = requestContext.getSecurityContext();

        // Skip if no authentication or no expected issuer configured
        if (securityContext == null || securityContext.getUserPrincipal() == null) {
            return;
        }

        if (expectedIssuer.isEmpty()) {
            LOG.debug("No expected issuer configured, skipping issuer validation");
            return;
        }

        // Skip if issuer validation is explicitly disabled (test profile only)
        if (!issuerValidationEnabled) {
            LOG.debug("Issuer validation disabled by configuration");
            return;
        }

        // If we have authentication but it's not a JWT, fail - unexpected state in production
        if (!(securityContext.getUserPrincipal() instanceof JsonWebToken)) {
            LOG.error("Principal is authenticated but not a JsonWebToken - unexpected state");
            requestContext.abortWith(
                    Response.status(Response.Status.INTERNAL_SERVER_ERROR)
                            .entity(
                                    "{\"error\": \"internal error: unexpected authentication state\"}")
                            .build());
            return;
        }

        // Get the issuer from the JWT
        String tokenIssuer = jwt.getIssuer();
        if (tokenIssuer == null) {
            LOG.warn("JWT has no issuer claim");
            requestContext.abortWith(
                    Response.status(Response.Status.UNAUTHORIZED)
                            .entity("{\"error\": \"invalid token: missing issuer\"}")
                            .build());
            return;
        }

        // Normalize both issuers (strip trailing slash) for comparison
        String normalizedTokenIssuer = normalizeIssuer(tokenIssuer);
        String normalizedExpectedIssuer = normalizeIssuer(expectedIssuer.get());

        if (!normalizedTokenIssuer.equals(normalizedExpectedIssuer)) {
            LOG.warnf(
                    "JWT issuer mismatch: token=%s, expected=%s",
                    tokenIssuer, expectedIssuer.get());
            requestContext.abortWith(
                    Response.status(Response.Status.UNAUTHORIZED)
                            .entity("{\"error\": \"invalid token: issuer mismatch\"}")
                            .build());
            return;
        }

        LOG.debugf("JWT issuer validated: %s", tokenIssuer);
    }

    private String normalizeIssuer(String issuer) {
        if (issuer == null) {
            return "";
        }
        // Remove trailing slash for consistent comparison
        return issuer.endsWith("/") ? issuer.substring(0, issuer.length() - 1) : issuer;
    }
}
