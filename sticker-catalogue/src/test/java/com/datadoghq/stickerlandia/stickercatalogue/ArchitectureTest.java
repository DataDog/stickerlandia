package com.datadoghq.stickerlandia.stickercatalogue;

import com.datadoghq.stickerlandia.common.architecture.StickerlandiaDatabaseRepository;
import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.domain.JavaModifier;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.POST;
import jakarta.ws.rs.Path;
import org.junit.jupiter.api.Test;

import javax.annotation.Resource;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;
import static com.tngtech.archunit.library.dependencies.SlicesRuleDefinition.slices;

/**
 * ArchUnit tests to validate architectural rules based on the CodeQL arch tests. These tests ensure
 * domain boundaries are respected and REST API classes don't directly import entity types.
 */
public class ArchitectureTest {

    private static final JavaClasses classes =
            new ClassFileImporter().importPackages("com.datadoghq.stickerlandia");

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


    /**
     * Let's not allow top level classes that are annotated with Path, for some reason.
     */
    @Test
    public void test_something_else() {
        ArchRule rule =
                classes()
                        .that()
                        .areAnnotatedWith(Path.class.getName())
                        .should()
                        .notBeTopLevelClasses();

        rule.check(classes);
    }

    /**
     * We don't like GETs on our resources
     */
    @Test
    public void we_dont_like_GET_apis() {
        ArchRule rule =
                noMethods()
                        .that().areAnnotatedWith(GET.class)
                        .should()
                        .beDeclaredInClassesThat()
                        .areAnnotatedWith(Path.class);

        rule.check(classes);
    }

    /**
     * We don't like dependency loops! This one is a bit more sensible.
     */
    @Test
    public void we_dont_like_cycles_either() {
        ArchRule rule =
                slices()
                        .matching("com.datadoghq.stickerlandia.(*)..")
                        .namingSlices("$1")
                        .should().beFreeOfCycles();

        rule.check(classes);
    }

    /**
     * We can also be creative and use custom annotations to assign architectural
     * roles to different pieces of our application, and then assert on them.
     *
     * We could use this rule, for instance, if we wanted to introduce a layer of
     * indirection between REST API handlers _and_ Repositories. It isn't there now,
     *  so we get a bunch of violations.
     */
    @Test
    public void resources_cant_use_repositories() {
        ArchRule rule =
                noClasses()
                    .that()
                        .areAnnotatedWith(Path.class)
                        .should()
                        .dependOnClassesThat()
                        .areAnnotatedWith(StickerlandiaDatabaseRepository.class);

        rule.check(classes);

    }
}
