package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import jakarta.ws.rs.HEAD;
import jakarta.ws.rs.Path;
import org.junit.jupiter.api.Test;

/**
 * Tests specific to REST API design and HTTP endpoint patterns. These tests ensure consistent and
 * proper HTTP API implementation.
 */
public class HttpApiTest {

    private static final JavaClasses classes = new ClassFileImporter().importPath("target/classes");

    /**
     * No reason to manually implement HEAD endpoints. JAX-RS automatically provides HEAD for GET
     * endpoints.
     */
    @Test
    public void no_explicit_head_endpoints() {
        ArchRule rule =
                noMethods()
                        .that()
                        .areAnnotatedWith(HEAD.class)
                        .should()
                        .beDeclaredInClassesThat()
                        .areAnnotatedWith(Path.class)
                        .allowEmptyShould(
                                true); // We optimally find no methods annotated with HEAD!

        rule.check(classes);
    }

    /**
     * All REST endpoints should use proper OpenAPI annotations for documentation. This ensures
     * comprehensive API documentation.
     */
    @Test
    public void endpoints_should_have_openapi_annotations() {
        ArchRule rule =
                methods()
                        .that()
                        .arePublic()
                        .and()
                        .areDeclaredInClassesThat()
                        .areAnnotatedWith(Path.class)
                        .and()
                        .areAnnotatedWith("jakarta.ws.rs.GET")
                        .or()
                        .areAnnotatedWith("jakarta.ws.rs.POST")
                        .or()
                        .areAnnotatedWith("jakarta.ws.rs.PUT")
                        .or()
                        .areAnnotatedWith("jakarta.ws.rs.DELETE")
                        .should()
                        .beAnnotatedWith("org.eclipse.microprofile.openapi.annotations.Operation")
                        .because("All REST endpoints should have OpenAPI documentation");

        rule.check(classes);
    }

    /**
     * REST resources should use proper HTTP status codes through Response objects. This ensures
     * consistent status code usage.
     */
    @Test
    public void rest_methods_should_return_response_objects() {
        ArchRule rule =
                methods()
                        .that()
                        .arePublic()
                        .and()
                        .areDeclaredInClassesThat()
                        .areAnnotatedWith(Path.class)
                        .and()
                        .areNotAnnotatedWith(
                                "jakarta.ws.rs.GET") // GET methods may return DTOs directly
                        .should()
                        .haveRawReturnType("jakarta.ws.rs.core.Response")
                        .because(
                                "Non-GET REST methods should return Response objects for proper status codes");

        rule.check(classes);
    }

    /**
     * Domain REST resources should use ProblemDetailsResponseBuilder for consistent error
     * responses. This ensures standardized error response format across the API (excluding health
     * endpoints).
     */
    @Test
    public void http_resources_should_use_problem_details_for_errors() {
        ArchRule rule =
                classes()
                        .that()
                        .areAnnotatedWith(Path.class)
                        .and()
                        .resideOutsideOfPackage("..health..")
                        .should()
                        .dependOnClassesThat()
                        .haveSimpleName("ProblemDetailsResponseBuilder")
                        .because(
                                "Domain REST resources should use ProblemDetailsResponseBuilder for error responses");

        rule.check(classes);
    }

    /**
     * REST endpoint methods should return appropriate types for HTTP serialization. This ensures
     * proper API contracts by allowing DTOs (for data), Response objects (for status code control),
     * or InputStreams (for binary content).
     */
    @Test
    public void rest_methods_should_return_serializable_types() {
        ArchRule rule =
                methods()
                        .that()
                        .arePublic()
                        .and()
                        .areDeclaredInClassesThat()
                        .areAnnotatedWith(Path.class)
                        .and()
                        .areAnnotatedWith("jakarta.ws.rs.GET")
                        .or()
                        .areAnnotatedWith("jakarta.ws.rs.POST")
                        .or()
                        .areAnnotatedWith("jakarta.ws.rs.PUT")
                        .or()
                        .areAnnotatedWith("jakarta.ws.rs.DELETE")
                        .should()
                        .haveRawReturnType("jakarta.ws.rs.core.Response")
                        .orShould()
                        .haveRawReturnType(
                                com.tngtech.archunit.base.DescribedPredicate.describe(
                                        "DTO type",
                                        clazz -> clazz.getPackageName().contains(".dto")))
                        .orShould()
                        .haveRawReturnType(java.io.InputStream.class) // For binary content
                        .because(
                                "REST methods should return Response (status control), DTOs (data), or InputStreams (binary)");

        rule.check(classes);
    }
}
