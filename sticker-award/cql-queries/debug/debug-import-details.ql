/**
 * @name Debug Import Details
 * @description Shows detailed info about imports in StickerRepository
 * @kind problem
 * @problem.severity info
 * @id debug/import-details
 */

import java

from CompilationUnit file, Import imp
where 
  file.getAbsolutePath().matches("%StickerRepository.java") and
  imp.getCompilationUnit() = file
select imp, "Import: " + imp.toString() + " | ImportedName: " + imp.getImportedName()