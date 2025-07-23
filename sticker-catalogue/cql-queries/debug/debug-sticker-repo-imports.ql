/**
 * @name Debug StickerRepository Imports
 * @description Shows all imports in StickerRepository files for debugging
 * @kind problem
 * @id debug/sticker-repo-imports
 */

import java

from Import imp
where imp.getCompilationUnit().getAbsolutePath().matches("%StickerRepository.java")
select imp, imp.toString()