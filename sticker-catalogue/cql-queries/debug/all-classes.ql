import java

from Class c
where 
  // Only include classes from the stickerlandia project
  c.getPackage().getName().matches("com.datadoghq.stickerlandia.%")
select c.getName() as name,
  c.getDoc().getJavadoc() as doc,
  c.getNumberOfLinesOfCode() as loc

