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

  // SINK: Panache operations that accept raw query strings (vulnerable to SQL injection)
  (panacheCall.getMethod().getDeclaringType().getASupertype*().hasQualifiedName("io.quarkus.hibernate.orm.panache", "PanacheEntityBase") and
   panacheCall.getMethod().getName() in ["find", "list", "count", "delete", "update"]) and

  // FLOW: HTTP parameter flows to Panache call
  simpleFlow(httpParam, panacheCall.getAnArgument()) and
  
  // Exclude test files
  not panacheCall.getFile().getAbsolutePath().matches("%/src/test/%")

select panacheCall,
       httpParam.getCallable().getDeclaringType().getName() + "::" + 
       httpParam.getCallable().getName() + " - HTTP parameter " + httpParam.getName() + " flows to " +
       panacheCall.getMethod().getName() + "()"


