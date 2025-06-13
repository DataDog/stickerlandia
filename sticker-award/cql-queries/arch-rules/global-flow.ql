import java
import semmle.code.java.dataflow.DataFlow
import semmle.code.java.dataflow.FlowSources

module MyFlowConfiguration implements DataFlow::ConfigSig {

  predicate isSource(DataFlow::Node source) {
    source instanceof RemoteFlowSource // this is a preconfigured way to find stuff that might be controlled by the user
  }

  predicate isSink(DataFlow::Node sink) {
  exists(MethodCall ma |
    ma.getMethod().getDeclaringType().getPackage().getName().indexOf("io.quarkus.hibernate.orm.panache") >= 0 and
    sink.asExpr() = ma
  )
}
  
}

module MyFlow = DataFlow::Global<MyFlowConfiguration>;


from DataFlow::Node src, DataFlow::Node sink
where MyFlow::flow(src, sink)
select src, src.getEnclosingCallable().getDeclaringType(), src.getEnclosingCallable(), "This data flows from here to", sink, "here"
