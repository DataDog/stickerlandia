/**
 * @name REST API classes should not import entity types (simple)
 * @description Finds REST API classes that import database entity types
 * @kind problem
 * @problem.severity error
 * @precision high
 * @id java/rest-api-entity-import-simple
 */

import java

from CompilationUnit file, Import imp
where
  // Match REST API files by path (anything ending in Resource.java)
  file.getAbsolutePath().matches("%Resource.java") and
  
  // Find imports in these files
  imp.getCompilationUnit() = file and
  
  // Check if import text contains 'entity'
  imp.toString().matches("%entity%")

select imp, 
  "REST API file '" + file.getBaseName() + 
  "' should not import entity type '" + imp.toString() + 
  "'. Use DTOs instead."