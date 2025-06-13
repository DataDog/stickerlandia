import java

from Class c, RefType t, Element reference
where 
  c.getName() = "StickerRepository" and
  c.getPackage().getName().matches("%.sticker") and
  (
    exists(Field f | f.getDeclaringType() = c and f.getType() = t and reference = f) or
    exists(Method m | m.getDeclaringType() = c and m.getReturnType() = t and reference = m) or
    exists(Parameter p | p.getCallable().getDeclaringType() = c and p.getType() = t and reference = p)
  )
select reference, t.getName(), t.getPackage().getName()