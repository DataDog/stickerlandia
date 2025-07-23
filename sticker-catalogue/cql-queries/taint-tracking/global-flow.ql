import java
import semmle.code.java.dataflow.DataFlow
import semmle.code.java.dataflow.FlowSources

module MyFlowConfiguration implements DataFlow::ConfigSig {

  predicate isSource(DataFlow::Node source) {
    // HTTP parameters from JAX-RS annotations
    exists(Parameter p |
      source.asParameter() = p and
      (p.getAnAnnotation().getType().hasQualifiedName("jakarta.ws.rs", "PathParam") or
       p.getAnAnnotation().getType().hasQualifiedName("jakarta.ws.rs", "QueryParam"))
    )
  }

  predicate isSink(DataFlow::Node sink) {
    // Panache method calls that could be vulnerable to SQL injection
    exists(MethodCall call |
      sink.asExpr() = call.getAnArgument() and
      call.getMethod().getDeclaringType().getASupertype*().hasQualifiedName("io.quarkus.hibernate.orm.panache", "PanacheEntityBase") and
      call.getMethod().getName() in ["find", "list", "count", "delete", "update"]
    )
  }
}

module MyFlow = DataFlow::Global<MyFlowConfiguration>;


from DataFlow::Node src, DataFlow::Node sink
where MyFlow::flow(src, sink)
select src, src.getEnclosingCallable().getDeclaringType(), src.getEnclosingCallable(), "This data flows from here to", sink, "here"
