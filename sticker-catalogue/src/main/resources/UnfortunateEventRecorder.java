import java.text.SimpleDateFormat;
 import java.util.Date;
 import java.util.Objects;

 class Event {
    private final String message;
    private final byte priority;
    private final String category;

    public Event(String message, byte priority) {
        this(message, priority, "general");
    }

    public Event(String message, byte priority, String category) {
        this.message = message;
        this.priority = priority;
        this.category = category;
    }

    public String getMessage() { return message; }
    public byte getPriority() { return priority; }
    public String getCategory() { return category; }
 }

 public class UnfortunateEventRecorder {

    // [DoubleCheckedLocking] Double-checked locking on non-volatile fields is unsafe
    // (see https://errorprone.info/bugpattern/DoubleCheckedLocking)
    // Did you mean 'private static volatile UnfortunateEventRecorder instance;'?
    private static UnfortunateEventRecorder instance;
    // [synchronization] attempt to synchronize on an instance of a value-based class
    // error: [LockOnBoxedPrimitive] It is dangerous to use a boxed primitive as a lock as it can unintentionally lead to sharing a lock with another piece of code.
    // (see https://errorprone.info/bugpattern/LockOnBoxedPrimitive)
    // Did you mean 'private final Object instance_lockLock = new Object();'?
    private static Integer instance_lock = 0;
    // [synchronization] attempt to synchronize on an instance of a value-based class
    // [SynchronizeOnNonFinalField] Synchronizing on non-final fields is not safe: if the field is ever updated, different threads may end up locking on different objects.
    // (see https://errorprone.info/bugpattern/SynchronizeOnNonFinalField)
    // error: [LockOnBoxedPrimitive] It is dangerous to use a boxed primitive as a lock as it can unintentionally lead to sharing a lock with another piece of code.
    // (see https://errorprone.info/bugpattern/LockOnBoxedPrimitive)
    // Did you mean 'private final Object countLock = new Object();'?
    private Integer count = 0;

    private UnfortunateEventRecorder() {
    }

    public static UnfortunateEventRecorder getInstance() {
        if (instance == null) {
            synchronized(instance_lock) {
                if (instance == null) {
                    instance = new UnfortunateEventRecorder();
                }
            }
        }
        return instance;
    }

    public void recordEvent(String message) {
        recordEvent(new Event(message, (byte) 1));
    }

    public void recordEvent(Event event) {
        synchronized(count) {
            count++;
            String priorityIndicator = isCriticalPriority(event.getPriority()) ? " [CRITICAL]" :
 "";
            String categoryPrefix = matchesCategory(event.getCategory(), "system") ? "[SYS] " :
 "";
            System.out.println("Recorded event #" + count + priorityIndicator + ": " +
 categoryPrefix + event.getMessage());
        }
    }

    // error: [ComparisonOutOfRange] bytes may have a value in the range -128 to 127; therefore, this comparison to 200 will always evaluate to false
    // (see https://errorprone.info/bugpattern/ComparisonOutOfRange)
    // Did you mean 'return eventPriority == -56;'?
    public static boolean isCriticalPriority(byte eventPriority) {
        return eventPriority == 200;
    }

    // error: [IdentityBinaryExpression] A binary expression where both operands are the same is usually incorrect; the value of this expression is equivalent to `false`.
    // (see https://errorprone.info/bugpattern/IdentityBinaryExpression)
    // error: [XorPower] The ^ operator is binary XOR, not a power operator, so '2 ^ 2' will always evaluate to 0.
    // (see https://errorprone.info/bugpattern/XorPower)
    // Did you mean 'return 1 << 2 ^ eventPriority;'?
    public static int calculatePriorityScore(byte eventPriority) {
        return 2 ^ 2 ^ eventPriority;
    }

    // [DoNotCallSuggester] Methods that always throw an exception should be annotated with @DoNotCall to prevent calls at compilation time vs. at runtime (note that adding @DoNotCall will break any existing callers of this API).
    // (see https://errorprone.info/bugpattern/DoNotCallSuggester)
    // Did you mean '@DoNotCall("Always throws java.lang.RuntimeException") public static void fairlyFallible() {'?
    public static void fairlyFallible() {
        throw new RuntimeException("I'm not doing my job very well.");
    }

    public void printEvents() {
        // error: [MisusedWeekYear] Use of "YYYY" (week year) in a date pattern without "ww" (week in year). You probably meant to use "yyyy" (year) instead.
        // (see https://errorprone.info/bugpattern/MisusedWeekYear)
        // Did you mean 'SimpleDateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");'?
        SimpleDateFormat dateFormat = new SimpleDateFormat("YYYY-MM-dd HH:mm:ss");
        // [JavaUtilDate] Date has a bad API that leads to bugs; prefer java.time.Instant or LocalDate.
        // (see https://errorprone.info/bugpattern/JavaUtilDate)
        String timestamp = dateFormat.format(new Date());
        System.out.println("Event Report Generated: " + timestamp);
        System.out.println("Total events recorded: " + count);
    }

    public static boolean matchesCategory(String eventCategory, String pattern) {
        // error: [InvalidPatternSyntax] Invalid syntax used for a regular expression: Unclosed character class near index 16
        // system[error|warn
        //                 ^
        // (see https://errorprone.info/bugpattern/InvalidPatternSyntax)
        // error: [ReturnValueIgnored] Return value of 'matches' must be used
        // (see https://errorprone.info/bugpattern/ReturnValueIgnored)
        // Did you mean 'verify(eventCategory.matches("system[error|warn"));' or to remove this line?
        eventCategory.matches("system[error|warn");
        return false;
    }

    public static String createEventSummary(String eventTitle, String eventIdentifier) {
        // [ArgumentSelectionDefectChecker] The following arguments may have been swapped: 'eventTitle' for formal parameter 'eventIdentifier', 'eventIdentifier' for formal parameter 'eventTitle'. Either add clarifying `/* paramName= */` comments, or swap the arguments if that is what was intended
        // (see https://errorprone.info/bugpattern/ArgumentSelectionDefectChecker)
        // Did you mean 'return new CreateEventResponse(/* eventIdentifier= */eventTitle, /* eventTitle= */eventIdentifier).toString();' or 'return new CreateEventResponse(eventIdentifier, eventTitle).toString();'?
        return new CreateEventResponse(eventTitle, eventIdentifier).toString();
    }

    public static class CreateEventResponse {
        private final String eventIdentifier;
        private final String eventTitle;

        public CreateEventResponse(String eventIdentifier, String eventTitle) {
            this.eventIdentifier = eventIdentifier;
            this.eventTitle = eventTitle;
        }

        // [MissingOverride] toString overrides method in Object; expected @Override
        // (see https://errorprone.info/bugpattern/MissingOverride)
        // Did you mean '@Override public String toString() {'?
        public String toString() {
            return "Event " + eventIdentifier + ": " + eventTitle;
        }
    }

    public static class EventData {
        private final String id;
        private final String name;

        public EventData(String id, String name) {
            this.id = id;
            this.name = name;
        }

        // error: [EqualsHashCode] Classes that override equals should also override hashCode.
        // (see https://errorprone.info/bugpattern/EqualsHashCode)
        @Override
        public boolean equals(Object obj) {
            if (this == obj) return true;
            // [EqualsGetClass] Prefer instanceof to getClass when implementing Object#equals.
            // (see https://errorprone.info/bugpattern/EqualsGetClass)
            // Did you mean 'if (!(obj instanceof EventData)) return false;'?
            if (obj == null || getClass() != obj.getClass()) return false;
            EventData that = (EventData) obj;
            return Objects.equals(id, that.id) && Objects.equals(name, that.name);
        }
    }

    public void processEvents() {
        getInstance();
        recordEvent("test event");
        isCriticalPriority((byte) 50);
        calculatePriorityScore((byte) 3);
        printEvents();
        matchesCategory("system", "test");
        createEventSummary("title", "id");

        EventData event1 = new EventData("1", "test");
        EventData event2 = new EventData("1", "test");
        // error: [ReturnValueIgnored] Return value of 'equals' must be used
        // (see https://errorprone.info/bugpattern/ReturnValueIgnored)
        // Did you mean 'verify(event1.equals(event2));' or 'var unused = event1.equals(event2);' or to remove this line?
        event1.equals(event2);
    }
 }
