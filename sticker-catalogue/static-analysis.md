# Code Style

We use `spotless` for code style. Check out the minimal configuration in [pom.xml](pom.xml) - we simply describe
the transforms we want to use, and we have both automatic formatting _and_ checking:

```xml
<plugin>
    <groupId>com.diffplug.spotless</groupId>
    <artifactId>spotless-maven-plugin</artifactId>
    <version>${spotless-plugin.version}</version>
    <configuration>
        <!-- These are transformation steps applied to your codebase -->
        <java>
            <!-- 1. Apply Google Java Format with 4-space indentation -->
            <googleJavaFormat>
                <version>1.19.2</version>
                <style>AOSP</style> <!-- AOSP style uses 4-space indentation -->
            </googleJavaFormat>
            <!-- Remove unused imports -->
            <removeUnusedImports/>
            <includes>
                <include>src/main/java/**/*.java</include>
                <include>src/test/java/**/*.java</include>
            </includes>
        </java>
    </configuration>
</plugin>
```

Check for any violations - this is what we run in your CI:
```bash
mvn spotless:check
```

Format code - this is what we run locally, optimally in our git hooks:
```bash
mvn spotless:apply
```

# Linting

We use [Error Prone](https://errorprone.info/) for Linting. It is actively maintained, 
popular, and seems to do a good job finding issues in the code. Again, the [pom.xml](pom.xml) 
contains the entire configuration; it is slightly more involved due to its interaction with Quarkus,
but well documented.

```bash
# Ignore checkstyle so we can just get to Error Prone errors
mvn clean compile 
```

