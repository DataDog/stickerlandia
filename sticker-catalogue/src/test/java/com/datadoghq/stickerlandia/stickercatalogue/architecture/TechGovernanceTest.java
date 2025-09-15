package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import org.junit.jupiter.api.Test;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.noClasses;
import static com.tngtech.archunit.library.dependencies.SlicesRuleDefinition.slices;


public class TechGovernanceTest {

    private static final JavaClasses classes =
            new ClassFileImporter().importPackages("com.datadoghq.stickerlandia");

    /**
     * All logging should use JBoss Logging, not other logging frameworks.
     * This ensures consistency with Quarkus logging infrastructure.
     */
    @Test
    public void should_only_use_jboss_logging() {
        ArchRule noSlf4j = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.slf4j..")
            .because("Use JBoss Logging instead of SLF4J for consistency with Quarkus");

        ArchRule noLog4j = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.apache.log4j..")
            .because("Use JBoss Logging instead of Log4j for consistency with Quarkus");

        ArchRule noJavaUtilLogging = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("java.util.logging..")
            .because("Use JBoss Logging instead of java.util.logging for consistency with Quarkus");

        ArchRule noCommonsLogging = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.apache.commons.logging..")
            .because("Use JBoss Logging instead of Commons Logging for consistency with Quarkus");

        // Check all the rules
        noSlf4j.check(classes);
        noLog4j.check(classes);
        noJavaUtilLogging.check(classes);
        noCommonsLogging.check(classes);
    }

    /**
     * Use Panache abstractions instead of raw Hibernate APIs.
     * Panache provides simpler, more maintainable database access patterns.
     */
    @Test
    public void should_use_panache_not_raw_hibernate() {
        ArchRule noHibernateSession = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.hibernate.Session..")
            .because("Use Panache abstractions not raw Hibernate Session");

        ArchRule noEntityManager = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("jakarta.persistence.EntityManager..")
            .because("Use Panache abstractions not raw JPA EntityManager");

        ArchRule noHibernateCriteria = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.hibernate.criterion..")
            .because("Use Panache query methods not Hibernate Criteria API");

        noHibernateSession.check(classes);
        noEntityManager.check(classes);
        noHibernateCriteria.check(classes);
    }

    /**
     * Use modern java.time API instead of legacy java.util.Date.
     * java.time provides better immutability, thread-safety, and API design.
     */
    @Test
    public void should_use_java_time_not_util_date() {
        ArchRule noUtilDate = noClasses()
            .should().dependOnClassesThat()
            .haveFullyQualifiedName("java.util.Date")
            .because("Use java.time.Instant/LocalDateTime instead of java.util.Date");

        ArchRule noUtilCalendar = noClasses()
            .should().dependOnClassesThat()
            .haveFullyQualifiedName("java.util.Calendar")
            .because("Use java.time.LocalDateTime instead of java.util.Calendar");

        ArchRule noSqlDate = noClasses()
            .should().dependOnClassesThat()
            .haveFullyQualifiedName("java.sql.Date")
            .because("Use java.time.LocalDate instead of java.sql.Date");

        noUtilDate.check(classes);
        noUtilCalendar.check(classes);
        noSqlDate.check(classes);
    }

    /**
     * Use Jakarta EE APIs (jakarta.*) not legacy Java EE (javax.*).
     * Jakarta EE is the modern standard after the Oracle → Eclipse transition.
     */
    @Test
    public void should_use_jakarta_not_javax() {
        ArchRule noJavaxWsRs = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("javax.ws.rs..")
            .because("Use jakarta.ws.rs not javax.ws.rs (Jakarta EE migration)");

        ArchRule noJavaxPersistence = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("javax.persistence..")
            .because("Use jakarta.persistence not javax.persistence (Jakarta EE migration)");

        ArchRule noJavaxInject = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("javax.inject..")
            .because("Use jakarta.inject not javax.inject (Jakarta EE migration)");

        noJavaxWsRs.check(classes);
        noJavaxPersistence.check(classes);
        noJavaxInject.check(classes);
    }

    /**
     * Use Jackson for JSON processing, not alternative JSON libraries.
     * Jackson is the standard in Quarkus and provides the best integration.
     */
    @Test
    public void should_use_jackson_for_json() {
        ArchRule noGson = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("com.google.gson..")
            .because("Use Jackson (com.fasterxml.jackson) not Gson for JSON processing");

        ArchRule noOrgJson = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.json..")
            .because("Use Jackson (com.fasterxml.jackson) not org.json for JSON processing");

        ArchRule noJsonSimple = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.json.simple..")
            .because("Use Jackson (com.fasterxml.jackson) not json-simple for JSON processing");

        noGson.check(classes);
        noOrgJson.check(classes);
        noJsonSimple.check(classes);
    }

    /**
     * Use JUnit 5 (Jupiter) not legacy JUnit 4 for testing.
     * JUnit 5 provides better assertions, lifecycle management, and extension model.
     */
    @Test
    public void should_use_junit5_not_junit4() {
        ArchRule noJunit4 = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("org.junit..")  // JUnit 4 package
            .because("Use JUnit 5 (org.junit.jupiter) not JUnit 4 for testing");

        ArchRule noJunit4Assert = noClasses()
            .should().dependOnClassesThat()
            .resideInAPackage("junit.framework..")
            .because("Use JUnit 5 assertions not JUnit 4 Assert class");

        noJunit4.check(classes);
        noJunit4Assert.check(classes);
    }

    /**
     * Use Quarkus-provided testing utilities instead of generic alternatives.
     * This ensures better integration with Quarkus lifecycle and configuration.
     */
    @Test
    public void should_use_quarkus_testing_utilities() {
        // This test would check for Quarkus-specific testing patterns
        // For now, we mainly ensure REST Assured is used consistently
        ArchRule noApacheHttpClient = noClasses()
            .that().resideInAPackage("..test..")
            .should().dependOnClassesThat()
            .resideInAPackage("org.apache.http..")
            .allowEmptyShould(true)  // Allow empty result when no violations found
            .because("Use REST Assured not Apache HttpClient for API testing in Quarkus");

        noApacheHttpClient.check(classes);
    }

}