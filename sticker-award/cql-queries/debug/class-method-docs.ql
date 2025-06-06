import java

from Class c, Method m
where 
  // Only include classes from the stickerlandia project
  c.getPackage().getName().matches("com.datadoghq.stickerlandia.%") and
  m.getDeclaringType() = c
  
select c.getName(), m.getName(), m.getDoc().getJavadoc()
