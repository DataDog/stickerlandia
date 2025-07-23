/**
 * @name Cross-Domain Boundary Violations
 * @description Finds violations where sticker and award domains import from each other
 * @kind problem
 * @problem.severity warning
 * @precision high
 * @id java/cross-domain-boundary-violation
 * @tags maintainability
 *       architecture
 *       domain-driven-design
 */

import java

from CompilationUnit file, ImportType imp
where
  // Find imports in these files
  imp.getCompilationUnit() = file and
  
  // Check for bidirectional violations
  (
    // Sticker domain importing from award domain
    (
      file.getAbsolutePath().matches("%/stickeraward/sticker/%.java") and
      imp.getImportedType().getPackage().getName().matches("%.stickeraward.award.%")
    ) or
    // Award domain importing from sticker domain
    (
      file.getAbsolutePath().matches("%/stickeraward/award/%.java") and
      imp.getImportedType().getPackage().getName().matches("%.stickeraward.sticker.%")
    )
  )

select imp, 
  "Domain boundary violation in '" + file.getBaseName() + 
  "': should not import '" + imp.toString() + 
  "' from another domain. Use DTOs or events instead."
