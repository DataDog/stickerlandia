package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.syntax.elements.GivenClassesConjunction;
import org.junit.jupiter.api.Test;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;

/**
 * Tests to enforce consistent domain structure across all domains.
 * These rules ensure each domain follows the same architectural patterns.
 */
public class DomainStructureTest {

    private static final JavaClasses classes =
            new ClassFileImporter().importPath("target/classes");

    /**
     * Helper to select domain classes (everything under com.datadoghq.stickerlandia 
     * except common and unfortunate infrastructure packages)
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

    @Test
    public void domain_dtos_should_follow_naming_conventions() {
        domainClasses()
            .and().resideInAPackage("..dto..")
            .should().haveSimpleNameEndingWith("Request")
            .orShould().haveSimpleNameEndingWith("Response")
            .orShould().haveSimpleNameEndingWith("DTO")
            .orShould().haveSimpleNameEndingWith("Metadata")
            .check(classes);
    }

    @Test
    public void domain_events_should_end_with_event() {
        domainClasses()
            .and().resideInAPackage("..event..")
            .should().haveSimpleNameEndingWith("Event")
            .check(classes);
    }

    @Test
    public void domain_repositories_should_end_with_repository() {
        domainClasses()
            .and().areAnnotatedWith("com.datadoghq.stickerlandia.common.architecture.StickerlandiaDatabaseRepository")
            .should().haveSimpleNameEndingWith("Repository")
            .check(classes);
    }

    @Test
    public void path_annotated_classes_should_end_with_resource() {
        domainClasses()
            .and().areAnnotatedWith("jakarta.ws.rs.Path")
            .should().haveSimpleNameEndingWith("Resource")
            .check(classes);
    }

    @Test
    public void domain_resources_should_depend_on_dtos() {
        domainClasses()
            .and().haveSimpleNameEndingWith("Resource")
            .should().dependOnClassesThat().resideInAPackage("..dto..")
            .check(classes);
    }

    @Test
    public void domain_repositories_should_depend_on_entities() {
        domainClasses()
            .and().haveSimpleNameEndingWith("Repository")
            .and().areNotAnnotations()
            .should().dependOnClassesThat().resideInAPackage("..entity..")
            .check(classes);
    }

    @Test
    public void services_should_be_annotated_with_application_scoped() {
        domainClasses()
            .and().haveSimpleNameEndingWith("Service")
            .should().beAnnotatedWith("jakarta.enterprise.context.ApplicationScoped")
            .check(classes);
    }
}