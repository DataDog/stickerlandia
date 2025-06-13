import java

predicate isStickerDomain(Package p) {
  p.getName().matches("%.sticker") or p.getName().matches("%.sticker.%")
}

predicate isAwardDomain(Package p) {
  p.getName().matches("%.award") or p.getName().matches("%.award.%")
}

from Class c, Package p
where c.getName().matches("%Repository%") and p = c.getPackage()
select c.getName(), p.getName()