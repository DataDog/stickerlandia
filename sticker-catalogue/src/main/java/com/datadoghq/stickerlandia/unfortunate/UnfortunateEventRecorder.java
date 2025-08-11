package com.datadoghq.stickerlandia.unfortunate;

import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Represents an unfortunate event with a message, priority level, and category.
 */
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

/**
 * A singleton recorder for capturing and tracking unfortunate events across the application.
 * This implementation demonstrates multiple concurrency anti-patterns that
 * ErrorProne can detect.
 */
public class UnfortunateEventRecorder {
    
    private static UnfortunateEventRecorder instance;
    private static Integer instance_lock = 0;
    private Integer count = 0;
    
    private UnfortunateEventRecorder() {
        // Private constructor for singleton pattern
    }

    /**
     * Gets the singleton instance using double-checked locking pattern.
     * This implementation has multiple concurrency bugs that ErrorProne will catch.
     */
    public static UnfortunateEventRecorder getInstance() {
        if (instance == null) { // First check without synchronization
            synchronized(instance_lock) { // BUG 1: Locking on boxed primitive Integer
                if (instance == null) { // BUG 2: Double-checked locking without volatile
                    instance = new UnfortunateEventRecorder();
                }
            }
        }
        return instance;
    }

    /**
     * Records an unfortunate event with the given message.
     */
    public void recordEvent(String message) {
        recordEvent(new Event(message, (byte) 1)); // Default priority
    }
    
    /**
     * Records an unfortunate event.
     */
    public void recordEvent(Event event) {
        synchronized(count) {
            count++;
            String priorityIndicator = isCriticalPriority(event.getPriority()) ? " [CRITICAL]" : "";
            String categoryPrefix = matchesCategory(event.getCategory(), "system") ? "[SYS] " : "";
            System.out.println("Recorded event #" + count + priorityIndicator + ": " + categoryPrefix + event.getMessage());
        }
    }

    /**
     * Checks if an event has critical priority level.
     * Event priorities are stored as bytes for memory efficiency.
     */
    public static boolean isCriticalPriority(byte eventPriority) {
        return eventPriority == 200; // Intended to check for critical level
    }

    /**
     * Calculates the priority score for an event based on its level.
     * Higher priority events get exponentially higher scores.
     */
    public static int calculatePriorityScore(byte eventPriority) {
        return 2 ^ 2 ^ eventPriority;
    }

    public static void fairlyFallible() {
        throw new RuntimeException("I'm not doing my job very well.");
    }

    /**
     * Prints all recorded events with formatted timestamps.
     * Used for generating event reports.
     */
    public void printEvents() {
        SimpleDateFormat dateFormat = new SimpleDateFormat("YYYY-MM-dd HH:mm:ss"); // BUG: should be yyyy
        String timestamp = dateFormat.format(new Date());
        System.out.println("Event Report Generated: " + timestamp);
        System.out.println("Total events recorded: " + count);
    }

    /**
     * Checks if event category matches a pattern for special handling.
     * Used for filtering system events with regex patterns.
     */
    public static boolean matchesCategory(String eventCategory, String pattern) {
        eventCategory.matches("system[error|warn");
        // TODO - return the actual pattern match result
        return false;
    }

}