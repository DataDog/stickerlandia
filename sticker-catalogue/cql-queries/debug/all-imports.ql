import java

from CompilationUnit file, Import imp
where 
  imp.getCompilationUnit() = file and
  file.getAbsolutePath().matches("%stickerlandia%")
select file.getBaseName(), imp.toString()