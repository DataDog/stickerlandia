package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import com.datadoghq.stickerlandia.common.architecture.StickerlandiaDatabaseRepository;
import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import jakarta.ws.rs.HEAD;
import jakarta.ws.rs.Path;
import org.junit.jupiter.api.Test;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;
import static com.tngtech.archunit.library.dependencies.SlicesRuleDefinition.slices;

/**
 * ArchUnit tests to validate architectural rules based on the CodeQL arch tests. These tests ensure
 * domain boundaries are respected and REST API classes don't directly import entity types.
 */
public class ArchitectureTest {

    private static final JavaClasses classes =
            new ClassFileImporter().importPath("target/classes");

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
     * No reason to manually implement HEAD endpoints
     */
    @Test
    public void no_explicit_head_endpoints() {

        ArchRule rule =
                noMethods()
                        .that().areAnnotatedWith(HEAD.class)
                        .should()
                        .beDeclaredInClassesThat()
                        .areAnnotatedWith(Path.class)
                        .allowEmptyShould(true); // We will optimally find no methods annotated with HEAD!
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

    /**
     * Services should not skip layers by calling other services directly.
     * Services should only depend on repositories, not other services.
     */
    @Test
    public void services_cant_skip_layers() {
        ArchRule rule =
                noClasses()
                    .that()
                        .haveSimpleNameEndingWith("Service")
                        .should()
                        .dependOnClassesThat()
                        .haveSimpleNameEndingWith("Service");

        rule.check(classes);
    }

    /**
     * Only repositories should directly instantiate or modify entity classes.
     * Other layers should work through repositories for entity access.
     */
    @Test
    public void only_repositories_can_create_entities() {
        ArchRule rule =
                noClasses()
                    .that()
                        .haveSimpleNameNotEndingWith("Repository")
                        .and()
                        .resideOutsideOfPackage("..entity..")
                        .should()
                        .accessClassesThat()
                        .resideInAPackage("..entity..");

        rule.check(classes);
    }
}
