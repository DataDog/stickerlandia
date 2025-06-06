/**
 * @name Sticker Repository Domain Boundary Violations
 * @description Finds violations where StickerRepository imports classes from the award domain
 * @kind problem
 * @problem.severity warning
 * @precision high
 * @id java/sticker-repository-domain-violation
 * @tags maintainability
 *       architecture
 *       domain-driven-design
 */

import java

from CompilationUnit file, Import imp
where
  // Match StickerRepository files by path
  file.getAbsolutePath().matches("%StickerRepository.java") and
  
  // Find imports in these files
  imp.getCompilationUnit() = file and
  
  // Check if import is specifically the problematic award domain types
  (
    imp.toString() = "import UserAssignmentDTO" or
    imp.toString() = "import StickerAssignment"
  )

select imp, 
  "StickerRepository file '" + file.getBaseName() + 
  "' should not import '" + imp.toString() + 
  "' from the award domain. Use DTOs or events instead." 

