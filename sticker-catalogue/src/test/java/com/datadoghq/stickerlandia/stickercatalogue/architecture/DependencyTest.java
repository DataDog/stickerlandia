package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;
import static com.tngtech.archunit.library.dependencies.SlicesRuleDefinition.slices;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import com.tngtech.archunit.lang.syntax.elements.GivenClassesConjunction;
import org.junit.jupiter.api.Test;

/**
 * Tests to validate dependency relationships and cycles across the application.
 * These tests ensure proper dependency direction and prevent problematic circular dependencies.
 */
public class DependencyTest {

    private static final JavaClasses classes = new ClassFileImporter().importPath("target/classes");

    /**
     * Helper to select domain classes (everything under com.datadoghq.stickerlandia except common
     * and unfortunate packages)
     */
    private static GivenClassesConjunction domainClasses() {
        return classes()
                .that()
                .resideInAPackage("com.datadoghq.stickerlandia..")
                .and()
                .resideOutsideOfPackage("..common..")
                .and()
                .resideOutsideOfPackage("..unfortunate..");
    }

    /** We don't like dependency loops! This prevents circular dependencies between domains. */
    @Test
    public void no_cycles() {
        ArchRule rule =
                slices().matching("com.datadoghq.stickerlandia.(*)..")
                        .namingSlices("$1")
                        .should()
                        .beFreeOfCycles();

        rule.check(classes);
    }

    /**
     * HTTP resources should depend on DTOs for proper serialization contracts.
     * This ensures the HTTP layer uses appropriate data transfer objects.
     */
    @Test
    public void http_resources_should_depend_on_dtos() {
        domainClasses()
                .and()
                .haveSimpleNameEndingWith("Resource")
                .should()
                .dependOnClassesThat()
                .resideInAPackage("..dto..")
                .check(classes);
    }

    /**
     * Repositories should depend on entities for data persistence.
     * This ensures the data layer works with proper domain entities.
     */
    @Test
    public void domain_repositories_should_depend_on_entities() {
        domainClasses()
                .and()
                .haveSimpleNameEndingWith("Repository")
                .and()
                .areNotAnnotations()
                .should()
                .dependOnClassesThat()
                .resideInAPackage("..entity..")
                .check(classes);
    }
}
