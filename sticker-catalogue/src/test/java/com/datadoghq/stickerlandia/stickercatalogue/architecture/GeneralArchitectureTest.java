package com.datadoghq.stickerlandia.stickercatalogue.architecture;

import com.tngtech.archunit.core.domain.JavaClasses;
import com.tngtech.archunit.core.importer.ClassFileImporter;
import com.tngtech.archunit.lang.ArchRule;
import org.junit.jupiter.api.Test;

import static com.tngtech.archunit.lang.syntax.ArchRuleDefinition.*;
import static com.tngtech.archunit.library.dependencies.SlicesRuleDefinition.slices;

/**
 * General architecture tests that apply across the entire application,
 * not specific to domain structure but enforcing technology and framework choices.
 */
public class GeneralArchitectureTest {

    private static final JavaClasses classes =
            new ClassFileImporter().importPath("target/classes");

    /**
     * We don't like dependency loops! This one is a bit more sensible.
     */
    @Test
    public void no_cycles() {
        ArchRule rule =
                slices()
                        .matching("com.datadoghq.stickerlandia.(*)..")
                        .namingSlices("$1")
                        .should().beFreeOfCycles();

        rule.check(classes);
    }
}