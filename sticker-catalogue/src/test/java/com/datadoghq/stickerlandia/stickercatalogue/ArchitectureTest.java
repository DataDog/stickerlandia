/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

package com.datadoghq.stickerlandia.stickercatalogue;

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
}
