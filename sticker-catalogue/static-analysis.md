# Checkstyle

Formatting lints, using the Google style guide checkstyle template:
[checkstyle.xml](checkstyle.xml)
```bash
mvn checkstyle:check
``` 

And we can fix _some of that_, if we want, with spotless:

```bash
mvn spotless:check
```

```bash
mvn spotless:apply
```

We can also go checkout [checkstyle.xml](checkstyle.xml) and customize the things that irk us.
... ish. It can't write javadoc for us!

# Error Prone

That was pretty noisy. What if we just want easy, drop-in lints that find _serious problems_ for us?

```bash
# Ignore checkstyle so we can just get to Error Prone errors
mvn clean compile -Dcheckstyle.skip=true 
```

