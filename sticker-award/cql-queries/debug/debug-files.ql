/**
 * @name Debug Files
 * @description Shows files that match StickerRepository pattern
 * @kind problem
 * @problem.severity info
 * @id debug/files
 */

import java

from CompilationUnit file
where file.getAbsolutePath().matches("%StickerRepository.java")
select file, "Found file: " + file.getAbsolutePath()