package com.datadoghq.stickerlandia.stickeraward;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.noClasses;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import org.junit.jupiter.api.Test;

/**
 * ArchUnit tests to validate architectural rules based on the CodeQL arch tests. These tests ensure
 * domain boundaries are respected and REST API classes don't directly import entity types.
 */
public class ArchitectureTest {

    private static final JavaClasses classes =
            new ClassFileImporter().importPackages("com.datadoghq.stickerlandia.stickeraward");

    /**
     * Cross-Domain Boundary Violations. Based on cross-domain-issues.ql Ensures sticker and award
     * domains don't import from each other.
     */
    @Test
    void sticker_domain_should_not_import_award_domain() {
        ArchRule rule =
                noClasses()
                        .that()
                        .resideInAPackage("..sticker..")
                        .should()
                        .dependOnClassesThat()
                        .resideInAPackage("..award..")
                        .because("Sticker domain should not import from award domain.");

        rule.check(classes);
    }

    @Test
    void award_domain_should_not_import_sticker_domain() {
        ArchRule rule =
                noClasses()
                        .that()
                        .resideInAPackage("..award..")
                        .should()
                        .dependOnClassesThat()
                        .resideInAPackage("..sticker..")
                        .because("Award domain should not import from sticker domain.");

        rule.check(classes);
    }

    /**
     * REST API classes should not import entity types. Based on rest-api-no-entities.ql and
     * rest-api-imports-simple.ql Ensures REST resources don't directly use entity classes.
     */
    @Test
    void rest_api_should_not_import_entities() {
        ArchRule rule =
                noClasses()
                        .that()
                        .haveSimpleNameEndingWith("Resource")
                        .should()
                        .dependOnClassesThat()
                        .resideInAPackage("..entity..")
                        .because("REST API classes should not import entity types.");

        rule.check(classes);
    }
}
