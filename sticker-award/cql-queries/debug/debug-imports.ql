/**
 * @name Debug Resource Imports
 * @description Shows all imports in Resource files for debugging
 * @kind table
 * @id debug/resource-imports
 */

import java

from Import imp
where imp.getCompilationUnit().getAbsolutePath().matches("%Resource.java")
select imp, imp.toString()