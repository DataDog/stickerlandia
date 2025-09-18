package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;

import com.datadoghq.stickerlandia.common.architecture.StickerlandiaDatabaseRepository;
import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import jakarta.ws.rs.Path;
import org.junit.jupiter.api.Test;

/**
 * Tests to enforce proper layer separation and access control. These tests ensure that different
 * architectural layers interact correctly and respect boundaries.
 */
public class LayeringTest {

    private static final JavaClasses classes = new ClassFileImporter().importPath("target/classes");

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
     * We can also be creative and use custom annotations to assign architectural roles to different
     * pieces of our application, and then assert on them.
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
     * Services should not depend on other services directly. This enforces proper layering where
     * services depend on repositories, not other services. Technical stores (like
     * StickerImageStore) don't end with "Service" so they're allowed.
     */
    @Test
    public void services_cant_skip_layers() {
        ArchRule rule =
                noClasses()
                        .that()
                        .haveSimpleNameEndingWith("Service")
                        .should()
                        .dependOnClassesThat()
                        .haveSimpleNameEndingWith("Service")
                        .because(
                                "Services should not depend on other services - use repositories or technical stores instead");

        rule.check(classes);
    }

    /**
     * Only repositories and services should directly access entity classes. Resources should work
     * through services and use DTOs for serialization.
     */
    @Test
    public void only_services_and_repositories_can_access_entities() {
        ArchRule rule =
                noClasses()
                        .that()
                        .haveSimpleNameNotEndingWith("Repository")
                        .and()
                        .haveSimpleNameNotEndingWith("Service")
                        .and()
                        .haveSimpleNameNotEndingWith("Mapper")
                        .and()
                        .haveSimpleNameNotEndingWith("Seeder")
                        .and()
                        .resideOutsideOfPackage("..entity..")
                        .and()
                        .resideOutsideOfPackage("..result..")
                        .should()
                        .accessClassesThat()
                        .resideInAPackage("..entity..");

        rule.check(classes);
    }

    /**
     * Only Services should publish events directly. This enforces that business logic and event
     * publishing are centralized in the service layer, not scattered across repositories or
     * seeders.
     */
    @Test
    public void only_services_should_use_event_publishers() {
        ArchRule rule =
                noClasses()
                        .that()
                        .haveSimpleNameNotEndingWith("Service")
                        .and()
                        .resideOutsideOfPackage("..test..")
                        .should()
                        .dependOnClassesThat()
                        .haveSimpleNameEndingWith("EventPublisher")
                        .because(
                                "Only Service classes should publish events - repositories and other classes should delegate to services");

        rule.check(classes);
    }
}
