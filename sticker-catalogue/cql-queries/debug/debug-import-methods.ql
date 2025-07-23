/**
 * @name Debug Import Methods
 * @description Shows what methods are available on Import
 * @kind problem
 * @problem.severity info
 * @id debug/import-methods
 */

import java

from CompilationUnit file, Import imp
where 
  file.getAbsolutePath().matches("%StickerRepository.java") and
  imp.getCompilationUnit() = file
select imp, "Import: " + imp.toString() + " | Type: " + imp.getImportedType().toString()