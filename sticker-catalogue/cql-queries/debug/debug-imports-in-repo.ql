/**
 * @name Debug Imports in StickerRepository
 * @description Shows all imports in StickerRepository files
 * @kind problem
 * @problem.severity info
 * @id debug/repo-imports
 */

import java

from CompilationUnit file, Import imp
where 
  file.getAbsolutePath().matches("%StickerRepository.java") and
  imp.getCompilationUnit() = file
select imp, "Import found: " + imp.toString()