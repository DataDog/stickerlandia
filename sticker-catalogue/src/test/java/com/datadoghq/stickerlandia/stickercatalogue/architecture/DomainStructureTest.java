package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.syntax.elements.GivenClassesConjunction;
import org.junit.jupiter.api.Test;

/**
 * Tests to enforce consistent domain structure across all domains. These rules ensure each domain
 * follows the same architectural patterns.
 */
public class DomainStructureTest {

    private static final JavaClasses classes = new ClassFileImporter().importPath("target/classes");

    /**
     * Helper to select domain classes (everything under com.datadoghq.stickerlandia except common
     * and unfortunate infrastructure packages)
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
    public void dtos_should_follow_naming_conventions() {
        domainClasses()
                .and()
                .resideInAPackage("..dto..")
                .should()
                .haveSimpleNameEndingWith("Request")
                .orShould()
                .haveSimpleNameEndingWith("Response")
                .orShould()
                .haveSimpleNameEndingWith("DTO")
                .check(classes);
    }

    @Test
    public void events_should_end_with_event() {
        domainClasses()
                .and()
                .resideInAPackage("..event..")
                .should()
                .haveSimpleNameEndingWith("Event")
                .check(classes);
    }

    @Test
    public void repositories_should_end_with_repository() {
        domainClasses()
                .and()
                // Here we use our 'architecture annotations' to identify components as an
                // alternative to the naming
                .areAnnotatedWith(
                        "com.datadoghq.stickerlandia.common.architecture.StickerlandiaDatabaseRepository")
                .should()
                .haveSimpleNameEndingWith("Repository")
                .check(classes);
    }

    @Test
    public void path_annotated_classes_should_end_with_resource() {
        domainClasses()
                .and()
                .areAnnotatedWith("jakarta.ws.rs.Path")
                .should()
                .haveSimpleNameEndingWith("Resource")
                .check(classes);
    }


    @Test
    public void services_should_be_annotated_with_application_scoped() {
        domainClasses()
                .and()
                .haveSimpleNameEndingWith("Service")
                .should()
                .beAnnotatedWith("jakarta.enterprise.context.ApplicationScoped")
                .check(classes);
    }
}
