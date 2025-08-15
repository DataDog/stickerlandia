/**
 * @name REST API classes should not import entity types
 * @description Finds REST API classes that import database entity types
 * @kind problem
 * @problem.severity error
 * @precision high
 * @id java/rest-api-entity-import
 * @tags maintainability
 *       architecture
 *       separation-of-concerns
 */

import java

from Class restClass, RefType entityType, Element reference
where
  // Match REST API classes by fully qualified class name
  (
    restClass.getQualifiedName() = "com.datadoghq.stickerlandia.stickercatalogue.StickerResource" or
    restClass.getQualifiedName() = "com.datadoghq.stickerlandia.stickercatalogue.award.StickerAwardResource"
  ) and
  
  // Check if the class references entity types
  entityType.getQualifiedName().matches("%.entity.%") and
  
  // Find references to entity types in fields, methods, or parameters
  // This isn't just matching on imports, it's finding concrete usages!
  (
    exists(Field f | f.getDeclaringType() = restClass and f.getType() = entityType and reference = f) or
    exists(Method m | m.getDeclaringType() = restClass and m.getReturnType() = entityType and reference = m) or
    exists(Parameter p | p.getCallable().getDeclaringType() = restClass and p.getType() = entityType and reference = p)
  )

select reference, 
  "REST API class '" + restClass.getName() + 
  "' should not reference entity type '" + entityType.getQualifiedName() + 
  "'. Use DTOs instead."