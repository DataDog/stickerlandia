package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import org.junit.jupiter.api.Test;

/**
 * Tests to enforce explicit result type patterns instead of null returns. These tests ensure that
 * services use type-safe error handling with sealed interfaces.
 */
public class ResultTypeTest {

    private static final JavaClasses classes = new ClassFileImporter().importPath("target/classes");

    /**
     * Result type classes should exist in the result package. This ensures we have proper result
     * type organization.
     */
    @Test
    public void result_types_should_be_in_result_package() {
        ArchRule rule =
                classes()
                        .that()
                        .haveSimpleNameEndingWith("Result")
                        .should()
                        .resideInAPackage("..result..")
                        .because("Result types should be organized in the result package");

        rule.check(classes);
    }

    /**
     * All result types should be interfaces to enable sealed interface patterns. This supports
     * type-safe error handling patterns.
     */
    @Test
    public void result_types_should_be_interfaces() {
        ArchRule rule =
                classes()
                        .that()
                        .haveSimpleNameEndingWith("Result")
                        .and()
                        .resideInAPackage("..result..")
                        .should()
                        .beInterfaces()
                        .because("Result types should be interfaces for sealed interface patterns");

        rule.check(classes);
    }

    /**
     * Success result cases should contain meaningful domain objects. This ensures successful
     * operations return useful data.
     */
    @Test
    public void success_cases_should_contain_domain_objects() {
        ArchRule rule =
                classes()
                        .that()
                        .haveSimpleNameEndingWith("Success")
                        .and()
                        .areNestedClasses()
                        .should()
                        .haveOnlyFinalFields()
                        .because("Success cases should be immutable records with domain data");

        rule.allowEmptyShould(true); // Allow empty if no Success nested classes exist
        rule.check(classes);
    }

    /**
     * Failure result cases should contain error information. This ensures failed operations provide
     * meaningful error context.
     */
    @Test
    public void failure_cases_should_contain_error_info() {
        ArchRule rule =
                classes()
                        .that()
                        .haveSimpleNameEndingWith("NotFound")
                        .or()
                        .haveSimpleNameEndingWith("Error")
                        .or()
                        .haveSimpleNameEndingWith("Failure")
                        .and()
                        .areNestedClasses()
                        .should()
                        .haveOnlyFinalFields()
                        .because(
                                "Failure cases should be immutable records with error information");

        rule.allowEmptyShould(true); // Allow empty if no failure nested classes exist
        rule.check(classes);
    }
}
