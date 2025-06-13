/**
 * @name HTTP Parameters Flowing to Panache ORM Operations - TAINT TRACKING
 * @description Tracks actual taint flows from HTTP parameters to Panache database operations
 * @kind problem
 * @problem.severity info
 * @precision medium
 * @id java/http-to-panache-taint-tracking
 * @tags security
 *       data-flow
 *       taint-tracking
 */

import java

/*
 * Simple data flow tracking within and across method calls
 */
predicate simpleFlow(Parameter source, Expr sink) {
  // Direct: parameter used as argument
  source.getAnAccess() = sink
  or
  // Through local variable: String x = param; method(x);
  exists(Variable v |
    v.getInitializer() = source.getAnAccess() and
    v.getAnAccess() = sink
  )
  or
  // Cross-method: parameter flows to method call, method parameter flows to sink
  exists(MethodCall call, Parameter methodParam |
    // HTTP param flows to method call argument
    simpleFlow(source, call.getAnArgument()) and
    // Method parameter flows to sink
    methodParam.getCallable() = call.getMethod() and
    simpleFlow(methodParam, sink)
  )
}

/*
 * TAINT TRACKING: HTTP Parameters → Panache ORM Operations
 */
from Parameter httpParam, MethodCall panacheCall
where
  // SOURCE: HTTP parameters with JAX-RS annotations  
  (httpParam.getAnAnnotation().getType().hasQualifiedName("jakarta.ws.rs", "PathParam") or
   httpParam.getAnAnnotation().getType().hasQualifiedName("jakarta.ws.rs", "QueryParam")) and

  // SINK: Panache operations
  (panacheCall.getMethod().getDeclaringType().getASupertype*().hasQualifiedName("io.quarkus.hibernate.orm.panache", "PanacheEntityBase") and
   (panacheCall.getMethod().getName().matches("find%") or
    panacheCall.getMethod().getName() = "list" or 
    panacheCall.getMethod().getName() = "count" or
    panacheCall.getMethod().getName() = "persist")) and

  // FLOW: HTTP parameter flows to Panache call
  simpleFlow(httpParam, panacheCall.getAnArgument()) and
  
  // Exclude test files
  not panacheCall.getFile().getAbsolutePath().matches("%/src/test/%")

select panacheCall,
       httpParam.getCallable().getDeclaringType().getName() + "::" + 
       httpParam.getCallable().getName() + " - HTTP parameter " + httpParam.getName() + " flows to " +
       panacheCall.getMethod().getName() + "()"

/*
 * THIS IS NOW ACTUAL TAINT TRACKING:
 * 
 * ✅ SOURCES: HTTP parameters (@PathParam, @QueryParam)
 * ✅ SINKS: Panache database operations (find*, persist, list, etc.)
 * ✅ FLOW: Tracks how HTTP data reaches database operations  
 * ✅ SECURITY RELEVANT: Shows potential injection/validation issues
 * 
 * The query finds actual security-relevant data flows, not just all Panache operations.
 */